// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Threading;
using static System.Math;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>
[PseudoClasses(":isPanning")]
public partial class ZoomBorder : Border
{
    [Conditional("DEBUG")]
    private static void Log(string message) => Debug.WriteLine(message);

    private static double ClampValue(double value, double minimum, double maximum)
    {
        if (minimum > maximum)
            throw new ArgumentException($"Parameter {nameof(minimum)} is greater than {nameof(maximum)}.");

        if (maximum < minimum)
            throw new ArgumentException($"Parameter {nameof(maximum)} is lower than {nameof(minimum)}.");

        return Min(Max(value, minimum), maximum);
    }

    /// <summary>
    /// Calculate pan and zoom matrix based on provided stretch mode.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="mode">The stretch mode.</param>
    public static Matrix CalculateMatrix(double panelWidth, double panelHeight, double elementWidth, double elementHeight, StretchMode mode)
    {
        var zx = panelWidth / elementWidth;
        var zy = panelHeight / elementHeight;
        var cx = elementWidth / 2.0;
        var cy = elementHeight / 2.0;

        switch (mode)
        {
            default:
            case StretchMode.None:
                return Matrix.Identity;
            case StretchMode.Fill:
                return MatrixHelper.ScaleAt(zx, zy, cx, cy);
            case StretchMode.Uniform:
            {
                var zoom = Min(zx, zy);
                return MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
            }
            case StretchMode.UniformToFill:
            {
                var zoom = Max(zx, zy);
                return MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
            }
        }
    }

    private PinchGestureRecognizer? _pinchGestureRecognizer;
    private ScrollGestureRecognizer? _scrollGestureRecognizer;
    private bool _gestureRecognizersAdded;
    
    // Multi-touch gesture tracking
    private DateTime _gestureStartTime;
    private bool _gestureRecognized;
    private bool _simultaneousGestureActive;

    // Pinch tracking
    private bool _pinchActive;
    private double _lastPinchScale = 1.0;
    private bool _autoFitPending = true;
    private Animation.TransformOperationsTransition? _animationTransition;
    private Matrix _lastValidMatrix = Matrix.Identity;

    // Zoom indicator
    private DispatcherTimer? _zoomIndicatorTimer;
    private bool _zoomIndicatorVisible;

    /// <summary>
    /// Gets the layout offset where Avalonia positioned the child element within this ZoomBorder.
    /// When a child element is smaller or larger than the ZoomBorder, Avalonia centers it by default, resulting in a
    /// non-zero Bounds.Position. This offset is in viewport coordinates and should NOT be scaled
    /// by the transform matrix when converting between coordinate systems.
    /// </summary>
    private Point LayoutOffset => _element?.Bounds.Position ?? default;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoomBorder"/> class.
    /// </summary>
    public ZoomBorder()
    {
        _isPanning = false;
        _matrix = Matrix.Identity;
        _captured = false;

        Focusable = true;
        Background = Brushes.Transparent;

        AttachedToVisualTree += PanAndZoom_AttachedToVisualTree;
        DetachedFromVisualTree += PanAndZoom_DetachedFromVisualTree;

        this.GetObservable(ChildProperty).Subscribe(new AnonymousObserver<Control?>(ChildChanged));
        this.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver<Rect>(BoundsChangedHandler));
        
        // Initialize gesture recognizers
        _pinchGestureRecognizer = new PinchGestureRecognizer();
        _scrollGestureRecognizer = new ScrollGestureRecognizer
        {
            CanHorizontallyScroll = true,
            CanVerticallyScroll = true
        };
        
        // Add gesture recognizers based on EnableGestures flag
        UpdateGestureRecognizers();
        
        // Subscribe to EnableGestures property changes
        this.GetObservable(EnableGesturesProperty).Subscribe(new AnonymousObserver<bool>(_ => UpdateGestureRecognizers()));
        this.GetObservable(StretchProperty).Subscribe(new AnonymousObserver<StretchMode>(_ => _autoFitPending = true));
        this.GetObservable(EnableAnimationsProperty).Subscribe(new AnonymousObserver<bool>(_ => UpdateAnimationTransition()));
        this.GetObservable(AnimationDurationProperty).Subscribe(new AnonymousObserver<TimeSpan>(_ => UpdateAnimationTransition()));
        this.GetObservable(BoundsModeProperty).Subscribe(new AnonymousObserver<ContentBoundsMode>(_ => Invalidate(skipTransitions: true)));
        this.GetObservable(BoundsPaddingProperty).Subscribe(new AnonymousObserver<Thickness>(_ => Invalidate(skipTransitions: true)));
        this.GetObservable(MinimumVisibleContentPercentageProperty).Subscribe(new AnonymousObserver<double>(_ => Invalidate(skipTransitions: true)));
    }

    /// <summary>
    /// Updates gesture recognizers based on EnableGestures flag.
    /// </summary>
    private void UpdateGestureRecognizers()
    {
        if (EnableGestures && !_gestureRecognizersAdded)
        {
            // Add pinch gesture recognizer
            if (_pinchGestureRecognizer != null)
            {
                GestureRecognizers.Add(_pinchGestureRecognizer);
            }
            
            // Add scroll gesture recognizer only if not disabled by ScrollViewer parent
            if (_scrollGestureRecognizer != null)
            {
                GestureRecognizers.Add(_scrollGestureRecognizer);
            }
            
            _gestureRecognizersAdded = true;
        }
        else if (!EnableGestures && _gestureRecognizersAdded)
        {
            ResetPinchGestureState();

            // Since GestureRecognizerCollection doesn't support Remove/Clear,
            // we need to recreate the recognizers to effectively "remove" them
            _pinchGestureRecognizer = new PinchGestureRecognizer();
            _scrollGestureRecognizer = new ScrollGestureRecognizer
            {
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true
            };
            
            _gestureRecognizersAdded = false;
        }
    }

    private void ResetPinchGestureState()
    {
        _gestureRecognized = false;
        _gestureStartTime = default;
        _simultaneousGestureActive = false;
        _pinchActive = false;
        _lastPinchScale = 1.0;
    }

    /// <summary>
    /// Checks if panning is allowed on pointer-wheel event.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    protected virtual bool CanPanOnPointerWheel(PointerWheelEventArgs e)
    {
        return EnablePan;
    }

    /// <summary>
    /// Checks if zooming is allowed on pointer-wheel event.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    protected virtual bool CanZoomOnPointerWheel(PointerWheelEventArgs e)
    {
        return EnableZoom;
    }

    /// <summary>
    /// Arranges the control's child.
    /// </summary>
    /// <param name="finalSize">The size allocated to the control.</param>
    /// <returns>The space taken.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);

        if (_element == null || !_element.IsMeasureValid)
        {
            return size;
        }

        if (_autoFitPending)
        {
            AutoFit(size.Width, size.Height, _element.Bounds.Width, _element.Bounds.Height);
            _autoFitPending = false;
        }

        return size;
    }

    private void PanAndZoom_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Log($"[AttachedToVisualTree] {Name}");
        ChildChanged(Child);

        // Add pointer event handlers
        PointerWheelChanged += Border_PointerWheelChanged;
        PointerPressed += Border_PointerPressed;
        PointerReleased += Border_PointerReleased;
        PointerMoved += Border_PointerMoved;
        PointerCaptureLost += Border_PointerCaptureLost;
        DoubleTapped += Border_DoubleTapped;
        KeyDown += Border_KeyDown;

        // Add gesture event handlers
        AddHandler(InputElement.PinchEvent, Border_PinchGesture);
        AddHandler(InputElement.PinchEndedEvent, Border_PinchGestureEnded);
        AddHandler(InputElement.ScrollGestureEvent, Border_ScrollGesture);
        AddHandler(InputElement.ScrollGestureEndedEvent, Border_ScrollGestureEnded);
        
        // Add touch pad gesture handler
        AddHandler(InputElement.PointerTouchPadGestureMagnifyEvent, Border_Magnified);

        // Update gesture recognizers based on the new state
        UpdateGestureRecognizers();

        _updating = true;
        Invalidate(skipTransitions: false);
        _updating = false;

        // Add initial state to history
        AddToViewHistory();
    }

    private void PanAndZoom_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Log($"[DetachedFromVisualTree] {Name}");
        ResetPinchGestureState();
        DetachElement();
        
        // Remove pointer event handlers
        PointerWheelChanged -= Border_PointerWheelChanged;
        PointerPressed -= Border_PointerPressed;
        PointerReleased -= Border_PointerReleased;
        PointerMoved -= Border_PointerMoved;
        PointerCaptureLost -= Border_PointerCaptureLost;
        DoubleTapped -= Border_DoubleTapped;
        KeyDown -= Border_KeyDown;

        // Remove gesture event handlers
        RemoveHandler(InputElement.PinchEvent, Border_PinchGesture);
        RemoveHandler(InputElement.PinchEndedEvent, Border_PinchGestureEnded);
        RemoveHandler(InputElement.ScrollGestureEvent, Border_ScrollGesture);
        RemoveHandler(InputElement.ScrollGestureEndedEvent, Border_ScrollGestureEnded);
        
        // Remove touch pad gesture handler
        RemoveHandler(InputElement.PointerTouchPadGestureMagnifyEvent, Border_Magnified);
    }

    private void Border_Magnified(object? sender, PointerDeltaEventArgs e)
    {
        Log($"[Magnified] {Name} {e.Delta}");
        var point = e.GetPosition(_element);
        ZoomDeltaTo(e.Delta.X, point.X, point.Y);
    }

    private void Border_PinchGesture(object? sender, PinchEventArgs e)
    {
        if (!EnableGestures || _element == null)
            return;

        // Need either zoom or rotation enabled for pinch gesture to do anything
        if (!EnableGestureZoom && !EnableGestureRotation)
            return;

        // Check if we're within touch point limits (pinch requires 2 points)
        if (MinimumTouchPoints > 2 || MaximumTouchPoints < 2)
        {
            return;
        }

        // Check gesture recognition delay
        if (!_gestureRecognized)
        {
            if (_gestureStartTime == default)
            {
                _gestureStartTime = DateTime.Now;
            }

            if ((DateTime.Now - _gestureStartTime) < GestureRecognitionDelay)
            {
                return;
            }

            _gestureRecognized = true;
        }

        // Check if simultaneous pan/zoom is allowed
        if (!EnableSimultaneousPanZoom && _isPanning)
        {
            return;
        }

        Log($"[PinchGesture] {Name} Scale: {e.Scale}, AngleDelta: {e.AngleDelta}");

        // Start from the reported pinch origin
        var visualPoint = e.ScaleOrigin;

        // Convert the pinch origin from ZoomBorder coordinates into child coordinates.
        var translatedPoint = this.TranslatePoint(visualPoint, _element);
        var zoomCenter = translatedPoint ?? visualPoint;

        // Mark simultaneous gesture as active
        _simultaneousGestureActive = EnableSimultaneousPanZoom && _isPanning;

        // Raise GestureStarted event
        var previousMatrix = _matrix;
        var gestureArgs = new GestureEventArgs(
            "Pinch",
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            zoomCenter.X,
            zoomCenter.Y,
            e.Scale - 1.0,
            _matrix,
            previousMatrix
        );
        RaiseGestureStarted(gestureArgs);

        // Apply zoom if enabled
        if (EnableGestureZoom)
        {
            if (!_pinchActive)
            {
                _pinchActive = true;
                _lastPinchScale = 1.0;
            }

            // Avalonia pinch scale is cumulative since gesture start,
            // so convert it to an incremental factor.
            var deltaScale = e.Scale / _lastPinchScale;
            _lastPinchScale = e.Scale;

            if (!double.IsNaN(deltaScale) && !double.IsInfinity(deltaScale) && deltaScale > 0.0)
            {
                Log($"[ZoomTo] factor: {deltaScale}, center: {zoomCenter.X}, {zoomCenter.Y}");
                ZoomTo(deltaScale, zoomCenter.X, zoomCenter.Y);
            }
        }

        // Apply rotation if enabled
        if (EnableGestureRotation && Math.Abs(e.AngleDelta) > 0.001)
        {
            // AngleDelta is in degrees (positive = clockwise, negative = counterclockwise)
            // Use RotateAt to rotate around the pinch center point
            RotateAt(e.AngleDelta, zoomCenter, animate: false);
        }

        e.Handled = true;
    }

    private void Border_PinchGestureEnded(object? sender, PinchEndedEventArgs e)
    {
        ResetPinchGestureState();

        if (!EnableGestures)
            return;

        Log($"[PinchGestureEnded] {Name}");

        var gestureArgs = new GestureEventArgs(
            "Pinch",
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            0,
            0,
            0,
            _matrix,
            _matrix
        );
        RaiseGestureEnded(gestureArgs);
        
        // Add to view history after pinch gesture completes
        AddToViewHistory();

        e.Handled = true;
    }

    private void Border_ScrollGesture(object? sender, ScrollGestureEventArgs e)
    {
        if (!EnableGestureTranslation || _element == null)
            return;

        // Check touch point limits (scroll gesture typically uses 2 fingers)
        if (MinimumTouchPoints > 2 || MaximumTouchPoints < 2)
        {
            return;
        }

        // Check if simultaneous pan/zoom is allowed when another gesture is active
        if (!EnableSimultaneousPanZoom && _simultaneousGestureActive)
        {
            return;
        }

        Log($"[ScrollGesture] {Name} Delta: {e.Delta}");

        // Raise GestureStarted event
        var previousMatrix = _matrix;
        var gestureArgs = new GestureEventArgs(
            "Scroll",
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            0,
            0,
            Math.Sqrt(e.Delta.X * e.Delta.X + e.Delta.Y * e.Delta.Y),
            _matrix,
            previousMatrix
        );
        RaiseGestureStarted(gestureArgs);

        // Use the scroll delta for panning. Scroll gesture delta follows
        // scroll direction semantics (positive = scroll down/right), which is
        // opposite to direct manipulation (content following finger). Invert
        // it so the content moves with the finger on touch/gesture devices.
        PanDelta(-e.Delta.X, -e.Delta.Y);

        e.Handled = true;
    }

    private void Border_ScrollGestureEnded(object? sender, ScrollGestureEndedEventArgs e)
    {
        Log($"[ScrollGestureEnded] {Name}");

        // Raise GestureEnded event
        var gestureArgs = new GestureEventArgs(
            "Scroll",
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            0,
            0,
            0,
            _matrix,
            _matrix
        );
        RaiseGestureEnded(gestureArgs);
        
        // Add to view history after scroll gesture completes
        AddToViewHistory();

        e.Handled = true;
    }

    private void Border_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var behavior = GetWheelBehavior(e.KeyModifiers);

        switch (behavior)
        {
            case WheelBehaviorMode.Zoom:
                if (EnableZoom)
                {
                    Wheel(e, WheelZoomSensitivity);
                    e.Handled = true;
                }
                else if (EnablePan && e.KeyModifiers == KeyModifiers.None)
                {
                    // Backward compatibility: If zoom is disabled but pan is enabled,
                    // fall back to panning with the wheel (old behavior)
                    PanDelta(10 * e.Delta.X * WheelPanSensitivity, 10 * e.Delta.Y * WheelPanSensitivity);
                    e.Handled = true;
                }
                break;

            case WheelBehaviorMode.PanVertical:
                if (EnablePan)
                {
                    PanDelta(10 * e.Delta.X * WheelPanSensitivity, 10 * e.Delta.Y * WheelPanSensitivity);
                    e.Handled = true;
                }
                break;

            case WheelBehaviorMode.PanHorizontal:
                if (EnablePan)
                {
                    PanDelta(10 * e.Delta.Y * WheelPanSensitivity, 10 * e.Delta.X * WheelPanSensitivity);
                    e.Handled = true;
                }
                break;

            case WheelBehaviorMode.None:
                break;
        }
    }

    private WheelBehaviorMode GetWheelBehavior(KeyModifiers modifiers)
    {
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            return WheelWithCtrl;
        }

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            return WheelWithShift;
        }

        return WheelBehavior;
    }

    private void Border_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (!EnableDoubleClickZoom || _element == null)
            return;

        var point = e.GetPosition(_element);

        switch (DoubleClickZoomMode)
        {
            case DoubleClickZoomMode.ZoomIn:
                ZoomTo(DoubleClickZoomFactor, point.X, point.Y, ShouldSkipTransitions());
                break;

            case DoubleClickZoomMode.ZoomOut:
                ZoomTo(1.0 / DoubleClickZoomFactor, point.X, point.Y, ShouldSkipTransitions());
                break;

            case DoubleClickZoomMode.ZoomInOut:
                // Toggle between zoom in and zoom out based on current zoom level
                if (_zoomX >= _doubleClickZoomThreshold)
                {
                    // Zoom out or reset
                    ResetMatrix(ShouldSkipTransitions());
                }
                else
                {
                    // Zoom in
                    ZoomTo(DoubleClickZoomFactor, point.X, point.Y, ShouldSkipTransitions());
                }
                break;

            case DoubleClickZoomMode.ZoomToFit:
                AutoFit(ShouldSkipTransitions());
                break;

            case DoubleClickZoomMode.None:
                return;
        }

        e.Handled = true;
    }

    private void Border_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!EnableKeyboardNavigation || _element == null)
            return;

        var handled = true;

        switch (e.Key)
        {
            case Key.Left:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    // Ctrl+Left: Navigate back
                    if (EnableViewHistory && CanNavigateBack)
                    {
                        NavigateBack(ShouldAnimate());
                    }
                }
                else
                {
                    // Left arrow: Pan left
                    PanDelta(-KeyboardPanStep, 0, ShouldSkipTransitions());
                }
                break;

            case Key.Right:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    // Ctrl+Right: Navigate forward
                    if (EnableViewHistory && CanNavigateForward)
                    {
                        NavigateForward(ShouldAnimate());
                    }
                }
                else
                {
                    // Right arrow: Pan right
                    PanDelta(KeyboardPanStep, 0, ShouldSkipTransitions());
                }
                break;

            case Key.Up:
                // Up arrow: Pan up
                PanDelta(0, -KeyboardPanStep, ShouldSkipTransitions());
                break;

            case Key.Down:
                // Down arrow: Pan down
                PanDelta(0, KeyboardPanStep, ShouldSkipTransitions());
                break;

            case Key.Add:
            case Key.OemPlus:
                // +/=: Zoom in
                if (_element != null)
                {
                    var centerX = _element.Bounds.Width / 2.0;
                    var centerY = _element.Bounds.Height / 2.0;
                    ZoomTo(KeyboardZoomStep, centerX, centerY, ShouldSkipTransitions());
                }
                break;

            case Key.Subtract:
            case Key.OemMinus:
                // -: Zoom out
                if (_element != null)
                {
                    var centerX = _element.Bounds.Width / 2.0;
                    var centerY = _element.Bounds.Height / 2.0;
                    ZoomTo(1.0 / KeyboardZoomStep, centerX, centerY, ShouldSkipTransitions());
                }
                break;

            case Key.D0:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    // Ctrl+0: Reset to 100% zoom (1:1)
                    ResetMatrix(ShouldSkipTransitions());
                }
                else
                {
                    handled = false;
                }
                break;

            case Key.Home:
                // Home: Fit to viewport
                AutoFit(ShouldSkipTransitions());
                break;

            default:
                handled = false;
                break;
        }

        if (handled)
        {
            e.Handled = true;
        }
    }

    private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Pressed(e);
    }

    private void Border_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        Released(e);
    }

    private void Border_PointerMoved(object? sender, PointerEventArgs e)
    {
        Moved(e);
    }

    private void Border_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        CaptureLost();
        e.Handled = true;
    }

    private void Element_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
        {
            InvalidateScrollable();
        }
    }

    private void BoundsChangedHandler(Rect bounds)
    {
        // Log($"[BoundsChanged] {bounds}");
        InvalidateScrollable();
        HandleResizeBehavior(bounds);
    }

    private void HandleResizeBehavior(Rect newBounds)
    {
        if (_element == null || _sizeBeforeResize == default(Size) || _sizeBeforeResize.Width == 0 || _sizeBeforeResize.Height == 0)
        {
            _sizeBeforeResize = newBounds.Size;
            return;
        }

        var oldSize = _sizeBeforeResize;
        var newSize = newBounds.Size;

        if (oldSize == newSize)
        {
            return;
        }

        switch (ResizeBehavior)
        {
            case ResizeBehaviorMode.None:
                break;

            case ResizeBehaviorMode.MaintainCenter:
                MaintainCenterOnResize(oldSize, newSize);
                break;

            case ResizeBehaviorMode.MaintainTopLeft:
                // Default behavior - do nothing
                break;

            case ResizeBehaviorMode.MaintainZoom:
                MaintainZoomOnResize(oldSize, newSize);
                break;

            case ResizeBehaviorMode.ReapplyStretch:
                AutoFit(skipTransitions: true);
                break;

            case ResizeBehaviorMode.Custom:
                OnResized(oldSize, newSize);
                break;
        }

        _sizeBeforeResize = newSize;
    }

    /// <summary>
    /// Virtual method called when control is resized in Custom resize behavior mode.
    /// </summary>
    /// <param name="oldSize">The previous size.</param>
    /// <param name="newSize">The new size.</param>
    protected virtual void OnResized(Size oldSize, Size newSize)
    {
    }

    private void MaintainCenterOnResize(Size oldSize, Size newSize)
    {
        if (_element == null)
            return;

        var oldCenterX = (oldSize.Width / 2.0 - _offsetX) / _zoomX;
        var oldCenterY = (oldSize.Height / 2.0 - _offsetY) / _zoomY;

        var newOffsetX = newSize.Width / 2.0 - oldCenterX * _zoomX;
        var newOffsetY = newSize.Height / 2.0 - oldCenterY * _zoomY;

        _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, newOffsetX, newOffsetY);
        Invalidate(skipTransitions: true);
    }

    private void MaintainZoomOnResize(Size oldSize, Size newSize)
    {
        if (_element == null)
            return;

        var scaleX = newSize.Width / oldSize.Width;
        var scaleY = newSize.Height / oldSize.Height;

        var newOffsetX = _offsetX * scaleX;
        var newOffsetY = _offsetY * scaleY;

        _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, newOffsetX, newOffsetY);
        Invalidate(skipTransitions: true);
    }

    private void ChildChanged(Control? element)
    {
        Log($"[ChildChanged] {element}");

        if (element != _element && _element != null)
        {
            DetachElement();
        }

        if (element != null && element != _element)
        {
            AttachElement(element);
        }
    }

    private void AttachElement(Control? element)
    {
        if (element == null)
        {
            return;
        }

        _element = element;
        _element.PropertyChanged += Element_PropertyChanged;
        _autoFitPending = true;
        UpdateAnimationTransition();
        
        // Notify commands that element is now available
        RaiseCommandsCanExecuteChanged();
    }

    private void DetachElement()
    {
        if (_element == null)
        {
            return;
        }

        RemoveAnimationTransition();
        _element.PropertyChanged -= Element_PropertyChanged;
        _element.RenderTransform = null;
        _element = null;
        _autoFitPending = true;
        
        // Notify commands that element is no longer available
        RaiseCommandsCanExecuteChanged();
    }

    private void Wheel(PointerWheelEventArgs e, double sensitivity = 1.0)
    {
        if (_element == null || _captured)
        {
            return;
        }
        var point = e.GetPosition(_element);
        ZoomDeltaTo(e.Delta.Y * sensitivity, point.X, point.Y);
    }

    private bool ShouldAnimate()
    {
        return EnableAnimations && AnimationDuration > TimeSpan.Zero;
    }

    private bool ShouldSkipTransitions()
    {
        return !ShouldAnimate();
    }

    private void UpdateAnimationTransition()
    {
        if (_element == null)
        {
            return;
        }

        RemoveAnimationTransition();

        if (!ShouldAnimate())
        {
            return;
        }

        var transitions = _element.Transitions;
        if (transitions?.OfType<Animation.TransformOperationsTransition>()
                .Any(transition => transition.Property == Visual.RenderTransformProperty) == true)
        {
            return;
        }

        transitions ??= new Animation.Transitions();
        _element.Transitions = transitions;

        _animationTransition = new Animation.TransformOperationsTransition
        {
            Property = Visual.RenderTransformProperty,
            Duration = AnimationDuration
        };
        transitions.Add(_animationTransition);
    }

    private void RemoveAnimationTransition()
    {
        if (_element?.Transitions != null && _animationTransition != null)
        {
            _element.Transitions.Remove(_animationTransition);
        }

        _animationTransition = null;
    }

    private void Pressed(PointerPressedEventArgs e)
    {
        if (!EnablePan)
        {
            return;
        }

        if (_element != null && !_captured && !_isPanning && IsPanButtonPressed(e))
        {
            var point = e.GetPosition(_element);
            BeginPanTo(point.X, point.Y);
            _captured = true;
            _isPanning = true;
            ((IPseudoClasses)Classes).Set(":isPanning", _isPanning);
        }
    }

    // ReSharper disable once UnusedParameter.Local
    private void Released(PointerReleasedEventArgs e) => PanningFinished();

    private void CaptureLost() => PanningFinished();

    private void PanningFinished()
    {
        if (!EnablePan)
        {
            return;
        }
        if (_element == null || _captured != true || _isPanning != true)
        {
            return;
        }
        
        // Raise PanEnded event
        var args = new PanEventArgs(
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            _offsetX,
            _offsetY,
            0,
            0,
            _matrix,
            _matrix
        );
        RaisePanEnded(args);
        
        // Add to view history after pan gesture completes
        AddToViewHistory();
        
        _captured = false;
        _isPanning = false;
        ((IPseudoClasses)Classes).Set(":isPanning", _isPanning);
    }

    private void Moved(PointerEventArgs e)
    {
        if (!EnablePan)
        {
            return;
        }

        if (_element == null || _captured != true || _isPanning != true || !IsPanButtonPressed(e))
        {
            return;
        }

        var point = e.GetPosition(_element);
        ContinuePanTo(point.X, point.Y, true);
    }

    private bool IsPanButtonPressed(PointerEventArgs e)
    {
        var button = PanButton;
        var properties = e.GetCurrentPoint(this).Properties;
        return (properties.IsLeftButtonPressed && button == ButtonName.Left)
            || (properties.IsRightButtonPressed && button == ButtonName.Right)
            || (properties.IsMiddleButtonPressed && button == ButtonName.Middle);
    }

    /// <summary>
    /// Adds the current view state to history.
    /// </summary>
    private void AddToViewHistory()
    {
        if (!EnableViewHistory || _isNavigating)
            return;

        var viewState = new ViewState
        {
            Matrix = _matrix,
            Stretch = Stretch,
            Timestamp = DateTime.UtcNow
        };

        // Remove any forward history when adding a new state
        if (_viewHistoryIndex < _viewHistory.Count - 1)
        {
            _viewHistory.RemoveRange(_viewHistoryIndex + 1, _viewHistory.Count - _viewHistoryIndex - 1);
        }

        // Add new state
        _viewHistory.Add(viewState);
        _viewHistoryIndex = _viewHistory.Count - 1;

        // Maintain size limit
        if (_viewHistory.Count > ViewHistorySize)
        {
            _viewHistory.RemoveAt(0);
            _viewHistoryIndex--;
        }

        ViewHistoryChanged?.Invoke(this, EventArgs.Empty);
        RaiseNavigationCommandsCanExecuteChanged();
    }

    /// <summary>
    /// Navigate back to the previous view state in history.
    /// </summary>
    /// <param name="animate">Whether to animate the transition.</param>
    public void NavigateBack(bool animate = true)
    {
        if (!CanNavigateBack)
            return;

        _isNavigating = true;
        _viewHistoryIndex--;
        var state = _viewHistory[_viewHistoryIndex];
        Stretch = state.Stretch;
        SetMatrix(state.Matrix, !animate);
        _isNavigating = false;

        ViewHistoryChanged?.Invoke(this, EventArgs.Empty);
        RaiseNavigationCommandsCanExecuteChanged();
    }

    /// <summary>
    /// Navigate forward to the next view state in history.
    /// </summary>
    /// <param name="animate">Whether to animate the transition.</param>
    public void NavigateForward(bool animate = true)
    {
        if (!CanNavigateForward)
            return;

        _isNavigating = true;
        _viewHistoryIndex++;
        var state = _viewHistory[_viewHistoryIndex];
        Stretch = state.Stretch;
        SetMatrix(state.Matrix, !animate);
        _isNavigating = false;

        ViewHistoryChanged?.Invoke(this, EventArgs.Empty);
        RaiseNavigationCommandsCanExecuteChanged();
    }

    /// <summary>
    /// Clears the view history.
    /// </summary>
    public void ClearViewHistory()
    {
        _viewHistory.Clear();
        _viewHistoryIndex = -1;
        ViewHistoryChanged?.Invoke(this, EventArgs.Empty);
        RaiseNavigationCommandsCanExecuteChanged();
    }

    /// <summary>
    /// Centers the viewport on a specific point in content coordinates.
    /// </summary>
    /// <param name="point">The point to center on.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void CenterOn(Point point, bool animate = true)
    {
        if (_element == null)
            return;

        var layoutOffset = LayoutOffset;

        var viewportCenterX = (Bounds.Width - CenterPadding.Left - CenterPadding.Right) / 2.0 + CenterPadding.Left;
        var viewportCenterY = (Bounds.Height - CenterPadding.Top - CenterPadding.Bottom) / 2.0 + CenterPadding.Top;

        // The actual visual position is: layoutOffset + matrix offset + point * zoom
        // So: matrix offset = viewportCenter - layoutOffset - point * zoom
        var offsetX = viewportCenterX - layoutOffset.X - point.X * _zoomX;
        var offsetY = viewportCenterY - layoutOffset.Y - point.Y * _zoomY;

        Pan(offsetX, offsetY, !animate);
    }

    /// <summary>
    /// Centers the viewport on a specific point in content coordinates with a specific zoom level.
    /// </summary>
    /// <param name="point">The point to center on.</param>
    /// <param name="zoom">The target zoom level.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void CenterOn(Point point, double zoom, bool animate = true)
    {
        if (_element == null)
            return;

        var layoutOffset = LayoutOffset;

        var viewportCenterX = (Bounds.Width - CenterPadding.Left - CenterPadding.Right) / 2.0 + CenterPadding.Left;
        var viewportCenterY = (Bounds.Height - CenterPadding.Top - CenterPadding.Bottom) / 2.0 + CenterPadding.Top;

        // The actual visual position is: layoutOffset + matrix offset + point * zoom
        // So: matrix offset = viewportCenter - layoutOffset - point * zoom
        var offsetX = viewportCenterX - layoutOffset.X - point.X * zoom;
        var offsetY = viewportCenterY - layoutOffset.Y - point.Y * zoom;

        _matrix = MatrixHelper.ScaleAndTranslate(zoom, zoom, offsetX, offsetY);
        Invalidate(!animate);
    }

    /// <summary>
    /// Centers the viewport on a rectangle in content coordinates.
    /// </summary>
    /// <param name="rect">The rectangle to center on.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void CenterOn(Rect rect, bool animate = true)
    {
        if (_element == null)
            return;

        var viewportWidth = Bounds.Width - CenterPadding.Left - CenterPadding.Right;
        var viewportHeight = Bounds.Height - CenterPadding.Top - CenterPadding.Bottom;

        var zoomX = viewportWidth / rect.Width;
        var zoomY = viewportHeight / rect.Height;
        var zoom = Math.Min(zoomX, zoomY);

        var centerX = rect.X + rect.Width / 2.0;
        var centerY = rect.Y + rect.Height / 2.0;

        CenterOn(new Point(centerX, centerY), zoom, animate);
    }

    /// <summary>
    /// Centers the viewport on a control element.
    /// </summary>
    /// <param name="element">The element to center on.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void CenterOn(Control element, bool animate = true)
    {
        if (element == null)
            return;

        var bounds = element.Bounds;
        CenterOn(bounds, animate);
    }

    /// <summary>
    /// Converts a viewport point to content coordinates.
    /// </summary>
    /// <param name="viewportPoint">The point in viewport coordinates.</param>
    /// <returns>The point in content coordinates.</returns>
    public Point ViewportToContent(Point viewportPoint)
    {
        if (!_matrix.TryInvert(out var inverted))
            return viewportPoint;

        var layoutOffset = LayoutOffset;
        var adjustedPoint = new Point(viewportPoint.X - layoutOffset.X, viewportPoint.Y - layoutOffset.Y);

        return inverted.Transform(adjustedPoint);
    }

    /// <summary>
    /// Converts a content point to viewport coordinates.
    /// </summary>
    /// <param name="contentPoint">The point in content coordinates.</param>
    /// <returns>The point in viewport coordinates.</returns>
    public Point ContentToViewport(Point contentPoint)
    {
        var transformed = _matrix.Transform(contentPoint);

        var layoutOffset = LayoutOffset;
        return new Point(transformed.X + layoutOffset.X, transformed.Y + layoutOffset.Y);
    }

    /// <summary>
    /// Converts a viewport rectangle to content coordinates.
    /// </summary>
    /// <param name="viewportRect">The rectangle in viewport coordinates.</param>
    /// <returns>The rectangle in content coordinates.</returns>
    public Rect ViewportToContent(Rect viewportRect)
    {
        if (!_matrix.TryInvert(out var inverted))
            return viewportRect;

        var layoutOffset = LayoutOffset;
        var adjustedRect = new Rect(
            viewportRect.X - layoutOffset.X,
            viewportRect.Y - layoutOffset.Y,
            viewportRect.Width,
            viewportRect.Height);

        var topLeft = inverted.Transform(adjustedRect.TopLeft);
        var bottomRight = inverted.Transform(adjustedRect.BottomRight);

        return new Rect(topLeft, bottomRight);
    }

    /// <summary>
    /// Converts a content rectangle to viewport coordinates.
    /// </summary>
    /// <param name="contentRect">The rectangle in content coordinates.</param>
    /// <returns>The rectangle in viewport coordinates.</returns>
    public Rect ContentToViewport(Rect contentRect)
    {
        var topLeft = _matrix.Transform(contentRect.TopLeft);
        var bottomRight = _matrix.Transform(contentRect.BottomRight);

        var layoutOffset = LayoutOffset;
        return new Rect(
            topLeft.X + layoutOffset.X,
            topLeft.Y + layoutOffset.Y,
            bottomRight.X - topLeft.X,
            bottomRight.Y - topLeft.Y);
    }

    /// <summary>
    /// Converts a screen vector to content vector.
    /// </summary>
    /// <param name="screenVector">The vector in screen coordinates.</param>
    /// <returns>The vector in content coordinates.</returns>
    public Vector ScreenToContent(Vector screenVector)
    {
        if (!_matrix.TryInvert(out var inverted))
            return screenVector;

        var origin = inverted.Transform(new Point(0, 0));
        var transformed = inverted.Transform(new Point(screenVector.X, screenVector.Y));

        return new Vector(transformed.X - origin.X, transformed.Y - origin.Y);
    }

    /// <summary>
    /// Converts a content vector to screen vector.
    /// </summary>
    /// <param name="contentVector">The vector in content coordinates.</param>
    /// <returns>The vector in screen coordinates.</returns>
    public Vector ContentToScreen(Vector contentVector)
    {
        var origin = _matrix.Transform(new Point(0, 0));
        var transformed = _matrix.Transform(new Point(contentVector.X, contentVector.Y));

        return new Vector(transformed.X - origin.X, transformed.Y - origin.Y);
    }

    /// <summary>
    /// Converts a screen size to content size.
    /// </summary>
    /// <param name="screenSize">The size in screen coordinates.</param>
    /// <returns>The size in content coordinates.</returns>
    public Size ScreenToContent(Size screenSize)
    {
        return new Size(screenSize.Width / _zoomX, screenSize.Height / _zoomY);
    }

    /// <summary>
    /// Converts a content size to screen size.
    /// </summary>
    /// <param name="contentSize">The size in content coordinates.</param>
    /// <returns>The size in screen coordinates.</returns>
    public Size ContentToScreen(Size contentSize)
    {
        return new Size(contentSize.Width * _zoomX, contentSize.Height * _zoomY);
    }

    /// <summary>
    /// Gets the transformation matrix from content to screen coordinates.
    /// </summary>
    /// <returns>The transformation matrix.</returns>
    public Matrix GetContentToScreenMatrix()
    {
        return _matrix;
    }

    /// <summary>
    /// Gets the transformation matrix from screen to content coordinates.
    /// </summary>
    /// <returns>The transformation matrix.</returns>
    public Matrix GetScreenToContentMatrix()
    {
        _matrix.TryInvert(out var inverted);
        return inverted;
    }

    /// <summary>
    /// Gets the visible content bounds in content coordinates.
    /// </summary>
    /// <returns>The visible content bounds.</returns>
    public Rect GetVisibleContentBounds()
    {
        return ViewportToContent(new Rect(0, 0, Bounds.Width, Bounds.Height));
    }

    /// <summary>
    /// Gets the viewport bounds in viewport coordinates.
    /// </summary>
    /// <returns>The viewport bounds.</returns>
    public Rect GetViewportBounds()
    {
        return new Rect(0, 0, Bounds.Width, Bounds.Height);
    }

    /// <summary>
    /// Zooms to fit a specific rectangle in content coordinates.
    /// </summary>
    /// <param name="rect">The rectangle to zoom to.</param>
    /// <param name="padding">Optional padding around the rectangle.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void ZoomToRectangle(Rect rect, Thickness? padding = null, bool animate = true)
    {
        if (_element == null || rect.Width == 0 || rect.Height == 0)
            return;
            
        if (_updating)
            return;
        _updating = true;

        var layoutOffset = LayoutOffset;

        var pad = padding ?? new Thickness(0);
        var viewportWidth = Bounds.Width - pad.Left - pad.Right;
        var viewportHeight = Bounds.Height - pad.Top - pad.Bottom;

        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            _updating = false;
            return;
        }

        var zoomX = viewportWidth / rect.Width;
        var zoomY = viewportHeight / rect.Height;
        var zoom = Math.Min(zoomX, zoomY);

        // Apply discrete zoom levels if enabled
        if (EnableDiscreteZoomLevels && DiscreteZoomLevels != null && DiscreteZoomLevels.Length > 0)
        {
            zoom = GetNearestDiscreteZoomLevel(zoom);
        }

        // Calculate center of the target rectangle in content coordinates
        var rectCenterX = rect.X + rect.Width / 2.0;
        var rectCenterY = rect.Y + rect.Height / 2.0;

        // Calculate the viewport center (accounting for padding)
        var viewportCenterX = pad.Left + viewportWidth / 2.0;
        var viewportCenterY = pad.Top + viewportHeight / 2.0;

        // The actual visual position is: layoutOffset + matrix offset + point * zoom
        // So: matrix offset = viewportCenter - layoutOffset - point * zoom
        var offsetX = viewportCenterX - layoutOffset.X - zoom * rectCenterX;
        var offsetY = viewportCenterY - layoutOffset.Y - zoom * rectCenterY;

        _matrix = new Matrix(zoom, 0, 0, zoom, offsetX, offsetY);
        Invalidate(!animate);
        
        _updating = false;
        AddToViewHistory();
    }

    /// <summary>
    /// Zooms to fit a specific rectangle with exact pixel dimensions.
    /// </summary>
    /// <param name="rect">The rectangle in content coordinates.</param>
    /// <param name="viewportRect">The target rectangle in viewport coordinates.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void ZoomToRectangleExact(Rect rect, Rect viewportRect, bool animate = true)
    {
        if (_element == null || rect.Width == 0 || rect.Height == 0)
            return;
            
        if (_updating)
            return;
        _updating = true;

        var layoutOffset = LayoutOffset;

        var zoomX = viewportRect.Width / rect.Width;
        var zoomY = viewportRect.Height / rect.Height;
        var zoom = Math.Min(zoomX, zoomY);

        if (EnableDiscreteZoomLevels && DiscreteZoomLevels != null && DiscreteZoomLevels.Length > 0)
        {
            zoom = GetNearestDiscreteZoomLevel(zoom);
        }

        // Calculate center of the target rectangle in content coordinates
        var rectCenterX = rect.X + rect.Width / 2.0;
        var rectCenterY = rect.Y + rect.Height / 2.0;

        // Calculate the center of the target viewport rect
        var viewportCenterX = viewportRect.X + viewportRect.Width / 2.0;
        var viewportCenterY = viewportRect.Y + viewportRect.Height / 2.0;

        // The actual visual position is: layoutOffset + matrix offset + point * zoom
        // So: matrix offset = viewportCenter - layoutOffset - point * zoom
        var offsetX = viewportCenterX - layoutOffset.X - zoom * rectCenterX;
        var offsetY = viewportCenterY - layoutOffset.Y - zoom * rectCenterY;

        // Temporarily disable constraints
        var previousEnableConstrains = EnableConstrains;
        EnableConstrains = false;
        
        _matrix = new Matrix(zoom, 0, 0, zoom, offsetX, offsetY);
        Invalidate(!animate);
        
        EnableConstrains = previousEnableConstrains;
        _updating = false;
        AddToViewHistory();
    }

    /// <summary>
    /// Saves the current view with a name.
    /// </summary>
    /// <param name="name">The name for this view.</param>
    /// <param name="description">Optional description.</param>
    public void SaveView(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("View name cannot be empty", nameof(name));

        var savedView = new SavedView
        {
            Name = name,
            Matrix = _matrix,
            Stretch = Stretch,
            Description = description,
            Timestamp = DateTime.UtcNow
        };

        _savedViews[name] = savedView;
    }

    /// <summary>
    /// Restores a previously saved view.
    /// </summary>
    /// <param name="name">The name of the view to restore.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    /// <returns>True if the view was found and restored, false otherwise.</returns>
    public bool RestoreView(string name, bool animate = true)
    {
        if (!_savedViews.TryGetValue(name, out var savedView))
            return false;

        Stretch = savedView.Stretch;
        SetMatrix(savedView.Matrix, !animate);
        return true;
    }

    /// <summary>
    /// Gets a saved view by name.
    /// </summary>
    /// <param name="name">The name of the view.</param>
    /// <returns>The saved view, or null if not found.</returns>
    public SavedView? GetSavedView(string name)
    {
        return _savedViews.TryGetValue(name, out var view) ? view : null;
    }

    /// <summary>
    /// Gets all saved view names.
    /// </summary>
    /// <returns>An array of saved view names.</returns>
    public string[] GetSavedViewNames()
    {
        return _savedViews.Keys.ToArray();
    }

    /// <summary>
    /// Gets all saved views.
    /// </summary>
    /// <returns>A collection of saved views.</returns>
    public IReadOnlyCollection<SavedView> GetSavedViews()
    {
        return _savedViews.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Deletes a saved view.
    /// </summary>
    /// <param name="name">The name of the view to delete.</param>
    /// <returns>True if the view was found and deleted, false otherwise.</returns>
    public bool DeleteSavedView(string name)
    {
        return _savedViews.Remove(name);
    }

    /// <summary>
    /// Clears all saved views.
    /// </summary>
    public void ClearSavedViews()
    {
        _savedViews.Clear();
    }

    /// <summary>
    /// Zooms to the nearest discrete zoom level.
    /// </summary>
    /// <param name="targetZoom">The target zoom level.</param>
    /// <returns>The nearest discrete zoom level.</returns>
    private double GetNearestDiscreteZoomLevel(double targetZoom)
    {
        if (DiscreteZoomLevels == null || DiscreteZoomLevels.Length == 0)
            return targetZoom;

        var nearest = DiscreteZoomLevels[0];
        var minDiff = Math.Abs(targetZoom - nearest);

        foreach (var level in DiscreteZoomLevels)
        {
            var diff = Math.Abs(targetZoom - level);
            if (diff < minDiff)
            {
                minDiff = diff;
                nearest = level;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Gets the next discrete zoom level up from current zoom.
    /// </summary>
    /// <returns>The next zoom level, or current zoom if at maximum.</returns>
    public double GetNextDiscreteZoomLevel()
    {
        if (!EnableDiscreteZoomLevels || DiscreteZoomLevels == null || DiscreteZoomLevels.Length == 0)
            return _zoomX * ZoomSpeed;

        var sorted = DiscreteZoomLevels.OrderBy(z => z).ToArray();
        var current = _zoomX;

        foreach (var level in sorted)
        {
            if (level > current)
                return level;
        }

        return sorted[sorted.Length - 1];
    }

    /// <summary>
    /// Gets the previous discrete zoom level down from current zoom.
    /// </summary>
    /// <returns>The previous zoom level, or current zoom if at minimum.</returns>
    public double GetPreviousDiscreteZoomLevel()
    {
        if (!EnableDiscreteZoomLevels || DiscreteZoomLevels == null || DiscreteZoomLevels.Length == 0)
            return _zoomX / ZoomSpeed;

        var sorted = DiscreteZoomLevels.OrderByDescending(z => z).ToArray();
        var current = _zoomX;

        foreach (var level in sorted)
        {
            if (level < current)
                return level;
        }

        return sorted[sorted.Length - 1];
    }

    /// <summary>
    /// Zooms to a specific discrete level.
    /// </summary>
    /// <param name="level">The zoom level to zoom to.</param>
    /// <param name="centerX">The x-coordinate of the zoom center point.</param>
    /// <param name="centerY">The y-coordinate of the zoom center point.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void ZoomToLevel(double level, double centerX, double centerY, bool animate = true)
    {
        if (!EnableZoom || _element == null)
            return;

        var zoom = level;
        if (EnableDiscreteZoomLevels && DiscreteZoomLevels != null && DiscreteZoomLevels.Length > 0)
        {
            zoom = GetNearestDiscreteZoomLevel(level);
        }

        ZoomTo(zoom / _zoomX, centerX, centerY, !animate);
    }

    /// <summary>
    /// Determines if a rectangle in content coordinates is visible in the viewport.
    /// </summary>
    /// <param name="rect">The rectangle in content coordinates.</param>
    /// <returns>True if any part of the rectangle is visible.</returns>
    public bool IsRectangleVisible(Rect rect)
    {
        var visibleContent = GetVisibleContentBounds();
        return rect.Intersects(visibleContent);
    }

    /// <summary>
    /// Determines if a point in content coordinates is visible in the viewport.
    /// </summary>
    /// <param name="point">The point in content coordinates.</param>
    /// <returns>True if the point is visible.</returns>
    public bool IsPointVisible(Point point)
    {
        var visibleContent = GetVisibleContentBounds();
        return visibleContent.Contains(point);
    }

    /// <summary>
    /// Gets the intersection of a rectangle with the visible content bounds.
    /// </summary>
    /// <param name="rect">The rectangle in content coordinates.</param>
    /// <returns>The visible portion of the rectangle.</returns>
    public Rect GetVisiblePortion(Rect rect)
    {
        var visibleContent = GetVisibleContentBounds();
        return rect.Intersect(visibleContent);
    }

    /// <summary>
    /// Calculates automatic zoom limits based on content and viewport size.
    /// </summary>
    /// <returns>A tuple containing the minimum and maximum zoom values.</returns>
    protected virtual (double minZoom, double maxZoom) CalculateAutoZoomLimits()
    {
        if (_element == null)
            return (double.NegativeInfinity, double.PositiveInfinity);

        var minZoom = double.NegativeInfinity;
        var maxZoom = double.PositiveInfinity;

        var contentWidth = _element.Bounds.Width;
        var contentHeight = _element.Bounds.Height;
        var viewportWidth = Bounds.Width;
        var viewportHeight = Bounds.Height;

        if (AutoCalculateMinZoom && contentWidth > 0 && contentHeight > 0 && viewportWidth > 0 && viewportHeight > 0)
        {
            // Min zoom is when the entire content fits in the viewport
            var zoomX = viewportWidth / contentWidth;
            var zoomY = viewportHeight / contentHeight;
            minZoom = Math.Min(zoomX, zoomY);
        }

        if (AutoCalculateMaxZoom && contentWidth > 0 && contentHeight > 0)
        {
            // Max zoom is based on pixel size (1 content pixel = MaxZoomPixelSize screen pixels)
            maxZoom = MaxZoomPixelSize;
        }

        return (minZoom, maxZoom);
    }

    /// <summary>
    /// Gets the effective zoom limits considering both manual settings and auto-calculated values.
    /// </summary>
    /// <param name="minZoomX">The effective minimum zoom for X axis.</param>
    /// <param name="maxZoomX">The effective maximum zoom for X axis.</param>
    /// <param name="minZoomY">The effective minimum zoom for Y axis.</param>
    /// <param name="maxZoomY">The effective maximum zoom for Y axis.</param>
    protected void GetEffectiveZoomLimits(out double minZoomX, out double maxZoomX, out double minZoomY, out double maxZoomY)
    {
        minZoomX = MinZoomX;
        maxZoomX = MaxZoomX;
        minZoomY = MinZoomY;
        maxZoomY = MaxZoomY;

        if (AutoCalculateMinZoom || AutoCalculateMaxZoom)
        {
            var (autoMinZoom, autoMaxZoom) = CalculateAutoZoomLimits();

            if (AutoCalculateMinZoom && !double.IsNegativeInfinity(autoMinZoom))
            {
                minZoomX = Math.Max(minZoomX, autoMinZoom);
                minZoomY = Math.Max(minZoomY, autoMinZoom);
            }

            if (AutoCalculateMaxZoom && !double.IsPositiveInfinity(autoMaxZoom))
            {
                maxZoomX = Math.Min(maxZoomX, autoMaxZoom);
                maxZoomY = Math.Min(maxZoomY, autoMaxZoom);
            }
        }
    }

    /// <summary>
    /// Gets the zoom indicator text based on the current zoom level.
    /// </summary>
    /// <returns>The formatted zoom indicator text.</returns>
    public string GetZoomIndicatorText()
    {
        var zoomValue = _zoomX; // Assuming uniform zoom for display
        return string.Format(ZoomIndicatorFormat, zoomValue);
    }

    /// <summary>
    /// Gets the position for the zoom indicator based on the configured position.
    /// </summary>
    /// <returns>A point representing the indicator position.</returns>
    protected virtual Point GetZoomIndicatorPosition()
    {
        const double margin = 10.0;
        const double indicatorWidth = 80.0;
        const double indicatorHeight = 30.0;

        return ZoomIndicatorPosition switch
        {
            ZoomIndicatorPosition.TopLeft => new Point(margin, margin),
            ZoomIndicatorPosition.TopRight => new Point(Bounds.Width - indicatorWidth - margin, margin),
            ZoomIndicatorPosition.BottomLeft => new Point(margin, Bounds.Height - indicatorHeight - margin),
            ZoomIndicatorPosition.BottomRight => new Point(Bounds.Width - indicatorWidth - margin, Bounds.Height - indicatorHeight - margin),
            _ => new Point(Bounds.Width - indicatorWidth - margin, Bounds.Height - indicatorHeight - margin)
        };
    }

    /// <summary>
    /// Snaps a value to the nearest grid point.
    /// </summary>
    /// <param name="value">The value to snap.</param>
    /// <returns>The snapped value.</returns>
    public double SnapToGrid(double value)
    {
        if (!EnableSnapToGrid || GridSize <= 0)
            return value;

        return Math.Round(value / GridSize) * GridSize;
    }

    /// <summary>
    /// Snaps a point to the nearest grid point.
    /// </summary>
    /// <param name="point">The point to snap.</param>
    /// <returns>The snapped point.</returns>
    public Point SnapToGrid(Point point)
    {
        return new Point(SnapToGrid(point.X), SnapToGrid(point.Y));
    }

    /// <summary>
    /// Snaps a rectangle to the nearest grid points.
    /// </summary>
    /// <param name="rect">The rectangle to snap.</param>
    /// <returns>The snapped rectangle.</returns>
    public Rect SnapToGrid(Rect rect)
    {
        var topLeft = SnapToGrid(new Point(rect.X, rect.Y));
        var bottomRight = SnapToGrid(new Point(rect.Right, rect.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    /// <summary>
    /// Rotates the content by the specified angle in degrees.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    /// <param name="animate">Whether to animate the rotation.</param>
    public void Rotate(double degrees, bool animate = true)
    {
        if (!EnableGestureRotation)
            return;

        var newRotation = Rotation + degrees;

        // Apply rotation snapping
        if (EnableRotationSnapping && RotationSnapAngle > 0)
        {
            newRotation = Math.Round(newRotation / RotationSnapAngle) * RotationSnapAngle;
        }

        // Apply rotation constraints
        newRotation = ClampValue(newRotation, MinRotation, MaxRotation);

        Rotation = newRotation;

        // Invalidate to apply the rotation transformation
        Invalidate(!animate);
    }

    /// <summary>
    /// Rotates the content by the specified angle around a center point.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    /// <param name="center">The center point for rotation in content coordinates.</param>
    /// <param name="animate">Whether to animate the rotation.</param>
    public void RotateAt(double degrees, Point center, bool animate = true)
    {
        if (!EnableGestureRotation)
            return;

        // Apply rotation (the center is handled in InvalidateElement based on content bounds)
        Rotate(degrees, animate);
    }

    /// <summary>
    /// Resets the rotation to zero.
    /// </summary>
    /// <param name="animate">Whether to animate the reset.</param>
    public void ResetRotation(bool animate = true)
    {
        Rotation = 0.0;
        
        // Invalidate to apply the rotation change
        Invalidate(!animate);
    }

    /// <summary>
    /// Snaps the current rotation to the nearest snap angle.
    /// </summary>
    public void SnapRotation()
    {
        if (!EnableRotationSnapping || RotationSnapAngle <= 0)
            return;

        Rotation = Math.Round(Rotation / RotationSnapAngle) * RotationSnapAngle;
        
        // Invalidate to apply the rotation change
        Invalidate(skipTransitions: false);
    }

    /// <summary>
    /// Exports the current state of the ZoomBorder control.
    /// </summary>
    /// <returns>A ZoomBorderState object containing the current state.</returns>
    public ZoomBorderState ExportState()
    {
        return new ZoomBorderState
        {
            Matrix = _matrix,
            Stretch = Stretch,
            ZoomSpeed = ZoomSpeed,
            EnablePan = EnablePan,
            EnableZoom = EnableZoom,
            Rotation = Rotation,
            MinZoomX = MinZoomX,
            MaxZoomX = MaxZoomX,
            MinZoomY = MinZoomY,
            MaxZoomY = MaxZoomY,
            EnableConstrains = EnableConstrains,
            EnableAnimations = EnableAnimations,
            AnimationDuration = AnimationDuration,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Imports state into the ZoomBorder control.
    /// </summary>
    /// <param name="state">The state to import.</param>
    /// <param name="animate">Whether to animate the state change.</param>
    public void ImportState(ZoomBorderState state, bool animate = true)
    {
        if (state == null)
            return;

        Stretch = state.Stretch;
        ZoomSpeed = state.ZoomSpeed;
        EnablePan = state.EnablePan;
        EnableZoom = state.EnableZoom;
        Rotation = state.Rotation;
        MinZoomX = state.MinZoomX;
        MaxZoomX = state.MaxZoomX;
        MinZoomY = state.MinZoomY;
        MaxZoomY = state.MaxZoomY;
        EnableConstrains = state.EnableConstrains;
        EnableAnimations = state.EnableAnimations;
        AnimationDuration = state.AnimationDuration;

        SetMatrix(state.Matrix, !animate);
    }

    /// <summary>
    /// Updates accessibility descriptions based on current state.
    /// </summary>
    public void UpdateAccessibilityDescriptions()
    {
        // Update zoom level description
        var zoomPercent = ZoomX * 100;
        ZoomLevelDescription = $"Zoom level: {zoomPercent:F0}%";

        // Update pan position description
        PanPositionDescription = $"Pan position: X={OffsetX:F0}, Y={OffsetY:F0}";
    }

    /// <summary>
    /// Gets the current accessibility description.
    /// </summary>
    /// <returns>A combined accessibility description.</returns>
    public string GetAccessibilityDescription()
    {
        UpdateAccessibilityDescriptions();
        return $"{ZoomLevelDescription}. {PanPositionDescription}";
    }

    /// <summary>
    /// Raises <see cref="ZoomChanged"/> event.
    /// </summary>
    /// <param name="e">Zoom changed event arguments.</param>
    protected virtual void OnZoomChanged(ZoomChangedEventArgs e)
    {
        ZoomChanged?.Invoke(this, e);
    }

    private void RaiseZoomChanged()
    {
        var args = new ZoomChangedEventArgs(_zoomX, _zoomY, _offsetX, _offsetY);
        OnZoomChanged(args);
    }

    private void Constrain()
    {
        // Get effective zoom limits (considering auto-calculation)
        GetEffectiveZoomLimits(out var effectiveMinZoomX, out var effectiveMaxZoomX, out var effectiveMinZoomY, out var effectiveMaxZoomY);

        var zoomX = ClampValue(_matrix.M11, effectiveMinZoomX, effectiveMaxZoomX);
        var zoomY = ClampValue(_matrix.M22, effectiveMinZoomY, effectiveMaxZoomY);
        var offsetX = ClampValue(_matrix.M31, MinOffsetX, MaxOffsetX);
        var offsetY = ClampValue(_matrix.M32, MinOffsetY, MaxOffsetY);
        _matrix = new Matrix(zoomX, 0.0, 0.0, zoomY, offsetX, offsetY);

        // Apply content bounds restriction
        ApplyContentBoundsRestriction();

        if (BoundsMode == ContentBoundsMode.Custom && !ValidateTransform(_matrix))
        {
            _matrix = _lastValidMatrix;
            return;
        }

        _lastValidMatrix = _matrix;
    }

    private void ApplyContentBoundsRestriction()
    {
        if (BoundsMode == ContentBoundsMode.Unrestricted || _element == null)
            return;

        switch (BoundsMode)
        {
            case ContentBoundsMode.KeepContentVisible:
                ApplyKeepContentVisible();
                break;

            case ContentBoundsMode.FillViewport:
                ApplyFillViewport();
                break;

            case ContentBoundsMode.KeepCentered:
                ApplyKeepCentered();
                break;

            case ContentBoundsMode.Custom:
                var customBounds = GetContentBounds();
                ApplyCustomBounds(customBounds);
                break;
        }
    }

    /// <summary>
    /// Virtual method to get custom content bounds.
    /// </summary>
    /// <returns>The content bounds rectangle.</returns>
    protected virtual Rect GetContentBounds()
    {
        return _element == null
            ? default
            : new Rect(0, 0, _element.Bounds.Width, _element.Bounds.Height);
    }

    /// <summary>
    /// Reapplies custom bounds restrictions and refreshes the logical scroll state.
    /// Call this from a subclass when the value returned by <see cref="GetContentBounds"/> changes.
    /// </summary>
    protected void InvalidateContentBounds()
    {
        Invalidate(skipTransitions: true);
    }

    /// <summary>
    /// Virtual method to validate transform matrix.
    /// </summary>
    /// <param name="newMatrix">The proposed new matrix.</param>
    /// <returns>True if the matrix is valid, false otherwise.</returns>
    protected virtual bool ValidateTransform(Matrix newMatrix)
    {
        return true;
    }

    private void ApplyKeepContentVisible()
    {
        if (_element == null)
            return;

        var layoutOffset = LayoutOffset;

        var contentWidth = _element.Bounds.Width * _matrix.M11;
        var contentHeight = _element.Bounds.Height * _matrix.M22;
        var viewportWidth = Bounds.Width;
        var viewportHeight = Bounds.Height;

        var minVisibleWidth = contentWidth * MinimumVisibleContentPercentage;
        var minVisibleHeight = contentHeight * MinimumVisibleContentPercentage;

        var offsetX = _matrix.M31;
        var offsetY = _matrix.M32;

        // Apply padding
        var padding = BoundsPadding;

        // The actual visual position is: layoutOffset + matrix offset
        // So: matrix offset = desired visual position - layoutOffset
        
        // Constrain X
        // Visual position range: [minVisibleWidth - contentWidth - padding.Left, viewportWidth - minVisibleWidth + padding.Right]
        // Matrix offset range: visual range shifted by -layoutOffset.X
        var maxOffsetX = viewportWidth - minVisibleWidth + padding.Right - layoutOffset.X;
        var minOffsetX = -contentWidth + minVisibleWidth - padding.Left - layoutOffset.X;
        offsetX = ClampValue(offsetX, minOffsetX, maxOffsetX);

        // Constrain Y
        // Visual position range: [minVisibleHeight - contentHeight - padding.Top, viewportHeight - minVisibleHeight + padding.Bottom]
        // Matrix offset range: visual range shifted by -layoutOffset.Y
        var maxOffsetY = viewportHeight - minVisibleHeight + padding.Bottom - layoutOffset.Y;
        var minOffsetY = -contentHeight + minVisibleHeight - padding.Top - layoutOffset.Y;
        offsetY = ClampValue(offsetY, minOffsetY, maxOffsetY);

        _matrix = new Matrix(_matrix.M11, 0.0, 0.0, _matrix.M22, offsetX, offsetY);
    }

    private void ApplyFillViewport()
    {
        if (_element == null)
            return;

        var layoutOffset = LayoutOffset;

        var contentWidth = _element.Bounds.Width * _matrix.M11;
        var contentHeight = _element.Bounds.Height * _matrix.M22;
        var viewportWidth = Bounds.Width;
        var viewportHeight = Bounds.Height;

        var offsetX = _matrix.M31;
        var offsetY = _matrix.M32;

        
        var padding = BoundsPadding;

        // The actual visual position is: layoutOffset + matrix offset
        // So: matrix offset = desired position - layoutOffset

        // If content is smaller than viewport, center it
        if (contentWidth <= viewportWidth)
        {
            var centeredX = (viewportWidth - contentWidth) / 2.0;
            offsetX = centeredX - layoutOffset.X;
        }
        else
        {
            // Constrain so no empty space is visible (with padding)
            // Visual position range should be: [viewportWidth - contentWidth - padding.Left, padding.Right]
            // So matrix offset range is: visual range shifted by -layoutOffset.X
            offsetX = ClampValue(offsetX, viewportWidth - contentWidth - padding.Left - layoutOffset.X, padding.Right - layoutOffset.X);
        }

        if (contentHeight <= viewportHeight)
        {
            var centeredY = (viewportHeight - contentHeight) / 2.0;
            offsetY = centeredY - layoutOffset.Y;
        }
        else
        {
            // Constrain so no empty space is visible (with padding)
            // Visual position range should be: [viewportHeight - contentHeight - padding.Top, padding.Bottom]
            // So matrix offset range is: visual range shifted by -layoutOffset.Y
            offsetY = ClampValue(offsetY, viewportHeight - contentHeight - padding.Top - layoutOffset.Y, padding.Bottom - layoutOffset.Y);
        }

        _matrix = new Matrix(_matrix.M11, 0.0, 0.0, _matrix.M22, offsetX, offsetY);
    }

    private void ApplyKeepCentered()
    {
        if (_element == null)
            return;

        var layoutOffset = LayoutOffset;
        
        var contentWidth = _element.Bounds.Width * _matrix.M11;
        var contentHeight = _element.Bounds.Height * _matrix.M22;
        var viewportWidth = Bounds.Width;
        var viewportHeight = Bounds.Height;

        // Calculate where the content should be centered in viewport coordinates
        var centeredX = (viewportWidth - contentWidth) / 2.0;
        var centeredY = (viewportHeight - contentHeight) / 2.0;
        
        // The actual visual position is: layoutOffset + matrix offset
        // So: matrix offset = desired position - layoutOffset
        var offsetX = centeredX - layoutOffset.X;
        var offsetY = centeredY - layoutOffset.Y;

        _matrix = new Matrix(_matrix.M11, 0.0, 0.0, _matrix.M22, offsetX, offsetY);
    }

    private void ApplyCustomBounds(Rect customBounds)
    {
        if (_element == null || customBounds.Width <= 0 || customBounds.Height <= 0)
        {
            return;
        }

        var transformedBounds = TransformContentToViewport(customBounds, LayoutOffset, _matrix);
        var viewportWidth = Bounds.Width;
        var viewportHeight = Bounds.Height;
        var padding = BoundsPadding;

        var targetX = transformedBounds.X;
        if (transformedBounds.Width <= viewportWidth)
        {
            targetX = (viewportWidth - transformedBounds.Width) / 2.0;
        }
        else
        {
            targetX = ClampValue(
                transformedBounds.X,
                viewportWidth - transformedBounds.Width - padding.Left,
                padding.Right);
        }

        var targetY = transformedBounds.Y;
        if (transformedBounds.Height <= viewportHeight)
        {
            targetY = (viewportHeight - transformedBounds.Height) / 2.0;
        }
        else
        {
            targetY = ClampValue(
                transformedBounds.Y,
                viewportHeight - transformedBounds.Height - padding.Top,
                padding.Bottom);
        }

        _matrix = new Matrix(
            _matrix.M11,
            _matrix.M12,
            _matrix.M21,
            _matrix.M22,
            _matrix.M31 + targetX - transformedBounds.X,
            _matrix.M32 + targetY - transformedBounds.Y);
    }

    /// <summary>
    /// Invalidate pan and zoom control.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    private void Invalidate(bool skipTransitions = false)
    {
        Log("[Invalidate] Begin");

        if (_element == null)
        {
            Log("[Invalidate] End");
            return;
        }

        if (EnableConstrains)
        {
            Constrain();
        }
        else
        {
            _lastValidMatrix = _matrix;
        }

        InvalidateProperties();
        InvalidateScrollable();
        InvalidateElement(skipTransitions);
        RaiseZoomChanged();
        
        // Show zoom indicator if enabled
        ShowZoomIndicatorTemporarily();

        Log("[Invalidate] End");
    }
    
    /// <summary>
    /// Shows the zoom indicator temporarily and starts the auto-hide timer.
    /// </summary>
    private void ShowZoomIndicatorTemporarily()
    {
        if (!ShowZoomIndicator)
            return;
            
        _zoomIndicatorVisible = true;
        RaisePropertyChanged(IsZoomIndicatorVisibleProperty, false, true);
        
        // Reset or start the auto-hide timer
        if (_zoomIndicatorTimer == null)
        {
            _zoomIndicatorTimer = new DispatcherTimer
            {
                Interval = ZoomIndicatorAutoHideDuration
            };
            _zoomIndicatorTimer.Tick += (s, e) =>
            {
                _zoomIndicatorTimer?.Stop();
                _zoomIndicatorVisible = false;
                RaisePropertyChanged(IsZoomIndicatorVisibleProperty, true, false);
            };
        }
        
        _zoomIndicatorTimer.Stop();
        _zoomIndicatorTimer.Interval = ZoomIndicatorAutoHideDuration;
        _zoomIndicatorTimer.Start();
    }

    /// <summary>
    /// Invalidate properties.
    /// </summary>
    private void InvalidateProperties()
    {
        SetAndRaise(ZoomXProperty, ref _zoomX, _matrix.M11);
        SetAndRaise(ZoomYProperty, ref _zoomY, _matrix.M22);
        SetAndRaise(OffsetXProperty, ref _offsetX, _matrix.M31);
        SetAndRaise(OffsetYProperty, ref _offsetY, _matrix.M32);
    }

    /// <summary>
    /// Invalidate child element.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    private void InvalidateElement(bool skipTransitions)
    {
        if (_element == null)
        {
            return;
        }

        Animation.Transitions? backupTransitions = null;

        if (skipTransitions)
        {
            Animation.Animatable? anim = _element;

            if (anim != null)
            {
                backupTransitions = anim.Transitions;
                anim.Transitions = null;
            }
        }

        _element.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
        
        // Apply rotation if enabled and non-zero
        var rotation = Rotation;
        if (EnableGestureRotation && Math.Abs(rotation) > 0.0001)
        {
            // Build transform with rotation: first apply scale/translate matrix, then rotate around content center
            var elementBounds = _element.Bounds;
            var centerX = elementBounds.Width / 2.0;
            var centerY = elementBounds.Height / 2.0;
            
            // Convert degrees to radians
            var radians = rotation * Math.PI / 180.0;
            
            // Create rotation matrix around the center of the content
            var rotationMatrix = MatrixHelper.Rotation(radians, centerX, centerY);
            
            // Combine: first apply pan/zoom matrix, then rotation
            var combinedMatrix = _matrix * rotationMatrix;
            
            _transformBuilder = new TransformOperations.Builder(1);
            _transformBuilder.AppendMatrix(combinedMatrix);
        }
        else
        {
            _transformBuilder = new TransformOperations.Builder(1);
            _transformBuilder.AppendMatrix(_matrix);
        }
        
        _element.RenderTransform = _transformBuilder.Build();

        if (skipTransitions && backupTransitions != null)
        {
            Animation.Animatable? anim = _element;

            if (anim != null)
            {
                anim.Transitions = backupTransitions;
            }
        }

        _element.InvalidateVisual();
    }

    /// <summary>
    /// Set pan and zoom matrix.
    /// </summary>
    /// <param name="matrix">The matrix to set as current.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void SetMatrix(Matrix matrix, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log("[SetMatrix]");
        var previousMatrix = _matrix;
        _matrix = matrix;
        Invalidate(skipTransitions);

        // Raise MatrixChanged event
        var args = new MatrixChangedEventArgs(
            _matrix,
            previousMatrix,
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            previousMatrix.M11,
            previousMatrix.M22,
            previousMatrix.M31,
            previousMatrix.M32,
            "SetMatrix"
        );
        RaiseMatrixChanged(args);

        _updating = false;
    }

    /// <summary>
    /// Reset pan and zoom matrix.
    /// </summary>
    public void ResetMatrix()
    {
        ResetMatrix(false);
    }

    /// <summary>
    /// Reset pan and zoom matrix.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ResetMatrix(bool skipTransitions)
    {
        var previousMatrix = _matrix;
        SetMatrix(Matrix.Identity, skipTransitions);
        
        // Raise MatrixReset event
        var args = new MatrixChangedEventArgs(
            Matrix.Identity,
            previousMatrix,
            1.0,
            1.0,
            0.0,
            0.0,
            previousMatrix.M11,
            previousMatrix.M22,
            previousMatrix.M31,
            previousMatrix.M32,
            "ResetMatrix"
        );
        RaiseMatrixReset(args);
        
        // Add to view history after reset completes
        AddToViewHistory();
    }

    /// <summary>
    /// Zoom to provided zoom value and provided center point.
    /// </summary>
    /// <param name="zoom">The zoom value.</param>
    /// <param name="x">The center point x axis coordinate.</param>
    /// <param name="y">The center point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Zoom(double zoom, double x, double y, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }

        // Clamp zoom to effective limits before applying to prevent translation jump
        var clampedZoom = zoom;
        if (EnableConstrains)
        {
            GetEffectiveZoomLimits(out var effectiveMinZoomX, out var effectiveMaxZoomX, out var effectiveMinZoomY, out var effectiveMaxZoomY);
            var effectiveMinZoom = Math.Max(effectiveMinZoomX, effectiveMinZoomY);
            var effectiveMaxZoom = Math.Min(effectiveMaxZoomX, effectiveMaxZoomY);
            clampedZoom = Math.Max(effectiveMinZoom, Math.Min(zoom, effectiveMaxZoom));
        }

        _updating = true;

        Log("[Zoom]");
        var previousMatrix = _matrix;
        var previousZoomX = _zoomX;
        var previousZoomY = _zoomY;
        
        _matrix = MatrixHelper.ScaleAt(clampedZoom, clampedZoom, x, y);
        Invalidate(skipTransitions);

        // Raise ZoomStarted event
        var args = new ZoomEventArgs(
            _zoomX,
            _zoomY,
            previousZoomX,
            previousZoomY,
            zoom / previousZoomX,
            x,
            y,
            _offsetX,
            _offsetY,
            _matrix,
            previousMatrix
        );
        RaiseZoomStarted(args);

        _updating = false;
    }

    /// <summary>
    /// Zoom to provided zoom ratio and provided center point.
    /// </summary>
    /// <param name="ratio">The zoom ratio.</param>
    /// <param name="x">The center point x axis coordinate.</param>
    /// <param name="y">The center point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomTo(double ratio, double x, double y, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }

        // Use effective zoom limits that consider auto-calculated bounds
        var effectiveRatio = ratio;

        if (EnableConstrains)
        {
            GetEffectiveZoomLimits(out var effectiveMinZoomX, out var effectiveMaxZoomX, out var effectiveMinZoomY, out var effectiveMaxZoomY);

            if ((ZoomX >= effectiveMaxZoomX && ZoomY >= effectiveMaxZoomY && ratio > 1) ||
                (ZoomX <= effectiveMinZoomX && ZoomY <= effectiveMinZoomY && ratio < 1))
            {
                return;
            }

            // Calculate clamped ratio to prevent exceeding limits and causing translation jump
            if (ratio > 1) // Zooming in
            {
                var maxRatioX = effectiveMaxZoomX / ZoomX;
                var maxRatioY = effectiveMaxZoomY / ZoomY;
                effectiveRatio = Math.Min(ratio, Math.Min(maxRatioX, maxRatioY));
            }
            else if (ratio < 1) // Zooming out
            {
                var minRatioX = effectiveMinZoomX / ZoomX;
                var minRatioY = effectiveMinZoomY / ZoomY;
                effectiveRatio = Math.Max(ratio, Math.Max(minRatioX, minRatioY));
            }

            // Don't proceed if effective ratio is essentially 1 (no zoom change)
            if (Math.Abs(effectiveRatio - 1.0) < 1e-10)
            {
                return;
            }
        }

        _updating = true;

        Log($"[ZoomTo] factor: {ratio}, center: {x}, {y}");
        var previousMatrix = _matrix;
        var previousZoomX = _zoomX;
        var previousZoomY = _zoomY;
        
        _matrix = MatrixHelper.ScaleAtPrepend(_matrix, effectiveRatio, effectiveRatio, x, y);
        Invalidate(skipTransitions);

        // Raise ZoomDeltaChanged event
        var args = new ZoomEventArgs(
            _zoomX,
            _zoomY,
            previousZoomX,
            previousZoomY,
            ratio,
            x,
            y,
            _offsetX,
            _offsetY,
            _matrix,
            previousMatrix
        );
        RaiseZoomDeltaChanged(args);
        
        // Add to view history after zoom operation completes
        AddToViewHistory();

        _updating = false;
    }

    /// <summary>
    /// Zoom in one step positive delta ratio and panel center point.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomIn(bool skipTransitions = false)
    {
        if (_element == null)
        {
            return;
        }

        var previousZoomX = _zoomX;
        var previousZoomY = _zoomY;
        var previousMatrix = _matrix;
        
        var x = _element.Bounds.Width / 2.0;
        var y = _element.Bounds.Height / 2.0;
        ZoomTo(ZoomSpeed, x, y, skipTransitions);
        
        // Raise ZoomEnded event
        var args = new ZoomEventArgs(
            _zoomX,
            _zoomY,
            previousZoomX,
            previousZoomY,
            ZoomSpeed,
            x,
            y,
            _offsetX,
            _offsetY,
            _matrix,
            previousMatrix
        );
        RaiseZoomEnded(args);
    }

    /// <summary>
    /// Zoom out one step positive delta ratio and panel center point.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomOut(bool skipTransitions = false)
    {
        if (_element == null)
        {
            return;
        }

        var previousZoomX = _zoomX;
        var previousZoomY = _zoomY;
        var previousMatrix = _matrix;
        
        var x = _element.Bounds.Width / 2.0;
        var y = _element.Bounds.Height / 2.0;
        ZoomTo(1 / ZoomSpeed, x, y, skipTransitions);
        
        // Raise ZoomEnded event
        var args = new ZoomEventArgs(
            _zoomX,
            _zoomY,
            previousZoomX,
            previousZoomY,
            1 / ZoomSpeed,
            x,
            y,
            _offsetX,
            _offsetY,
            _matrix,
            previousMatrix
        );
        RaiseZoomEnded(args);
    }

    /// <summary>
    /// Zoom to provided zoom delta ratio and provided center point.
    /// </summary>
    /// <param name="delta">The zoom delta ratio.</param>
    /// <param name="x">The center point x axis coordinate.</param>
    /// <param name="y">The center point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomDeltaTo(double delta, double x, double y, bool skipTransitions = false)
    {
        double realDelta = Sign(delta) * Pow(Abs(delta), PowerFactor);
        ZoomTo(Pow(ZoomSpeed, realDelta), x, y, skipTransitions || Abs(realDelta) <= TransitionThreshold);
    }

    /// <summary>
    /// Pan control to provided delta.
    /// </summary>
    /// <param name="dx">The target x axis delta.</param>
    /// <param name="dy">The target y axis delta.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void PanDelta(double dx, double dy, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log("[PanDelta]");
        var previousMatrix = _matrix;
        var previousOffsetX = _offsetX;
        var previousOffsetY = _offsetY;
        
        _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, _matrix.M31 + dx, _matrix.M32 + dy);
        Invalidate(skipTransitions);

        // Raise PanContinued event
        var args = new PanEventArgs(
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            previousOffsetX,
            previousOffsetY,
            dx,
            dy,
            _matrix,
            previousMatrix
        );
        RaisePanContinued(args);

        _updating = false;
    }

    /// <summary>
    /// Pan control to set the viewport offset to the specified values.
    /// </summary>
    /// <param name="x">The target offset x value.</param>
    /// <param name="y">The target offset y value.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Pan(double x, double y, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log("[Pan]");
        var previousMatrix = _matrix;
        var previousOffsetX = _offsetX;
        var previousOffsetY = _offsetY;
        
        _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, x, y);
        Invalidate(skipTransitions);

        // Raise PanContinued event
        var args = new PanEventArgs(
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            previousOffsetX,
            previousOffsetY,
            x - previousOffsetX,
            y - previousOffsetY,
            _matrix,
            previousMatrix
        );
        RaisePanContinued(args);
        AddToViewHistory();

        _updating = false;
    }

    /// <summary>
    /// Set pan origin.
    /// </summary>
    /// <param name="x">The origin point x axis coordinate.</param>
    /// <param name="y">The origin point y axis coordinate.</param>
    public void BeginPanTo(double x, double y)
    {
        _pan = new Point();
        _previous = new Point(x, y);
        
        // Raise PanStarted event
        var args = new PanEventArgs(
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            _offsetX,
            _offsetY,
            0.0,
            0.0,
            _matrix,
            _matrix
        );
        RaisePanStarted(args);
    }

    /// <summary>
    /// Continue pan to provided target point.
    /// </summary>
    /// <param name="x">The target point x axis coordinate.</param>
    /// <param name="y">The target point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ContinuePanTo(double x, double y, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log("[ContinuePanTo]");
        var previousMatrix = _matrix;
        var previousOffsetX = _offsetX;
        var previousOffsetY = _offsetY;
        
        var dx = x - _previous.X;
        var dy = y - _previous.Y;
        var delta = new Point(dx, dy);
        _previous = new Point(x, y);
        _pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
        _matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);
        Invalidate(skipTransitions);

        // Raise PanContinued event
        var args = new PanEventArgs(
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            previousOffsetX,
            previousOffsetY,
            dx,
            dy,
            _matrix,
            previousMatrix
        );
        RaisePanContinued(args);

        _updating = false;
    }

    /// <summary>
    /// Zoom and pan.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void None(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log($"[None] {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
        if (_element == null)
        {
            _updating = false;
            return;
        }

        _matrix = CalculateMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.None);
        Invalidate(skipTransitions);

        _updating = false;
    }

    /// <summary>
    /// Zoom and pan to fill panel.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Fill(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log($"[Fill] {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
        if (_element == null)
        {
            _updating = false;
            return;
        }

        _matrix = CalculateMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.Fill);
        Invalidate(skipTransitions);

        _updating = false;
    }

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Uniform(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log($"[Uniform] {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
        if (_element == null)
        {
            _updating = false;
            return;
        }

        _matrix = CalculateMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.Uniform);
        Invalidate(skipTransitions);

        _updating = false;
    }

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio. If aspect of panel is different panel is filled.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void UniformToFill(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log($"[UniformToFill] {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
        if (_element == null)
        {
            _updating = false;
            return;
        }

        _matrix = CalculateMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.UniformToFill);
        Invalidate(skipTransitions);

        _updating = false;
    }

    /// <summary>
    /// Zoom and pan child element inside panel using stretch mode.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void AutoFit(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
    {
        if (_element == null)
        {
            return;
        }
        switch (Stretch)
        {
            case StretchMode.Fill:
                Fill(panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);
                break;
            case StretchMode.Uniform:
                Uniform(panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);
                break;
            case StretchMode.UniformToFill:
                UniformToFill(panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);
                break;
            case StretchMode.None:
                break;
        }
    }

    /// <summary>
    /// Set next stretch mode.
    /// </summary>
    public void ToggleStretchMode()
    {
        switch (Stretch)
        {
            case StretchMode.None:
                Stretch = StretchMode.Fill;
                break;
            case StretchMode.Fill:
                Stretch = StretchMode.Uniform;
                break;
            case StretchMode.Uniform:
                Stretch = StretchMode.UniformToFill;
                break;
            case StretchMode.UniformToFill:
                Stretch = StretchMode.None;
                break;
        }
    }

    /// <summary>
    /// Zoom and pan.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void None(bool skipTransitions = false)
    {
        if (_element == null)
        {
            return;
        }
        None(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
    }

    /// <summary>
    /// Zoom and pan to fill panel.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Fill(bool skipTransitions = false)
    {
        if (_element == null)
        {
            return;
        }
        var previousStretch = Stretch;
        var previousMatrix = _matrix;
        
        Fill(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
        
        // Raise StretchModeChanged event
        var args = new StretchModeChangedEventArgs(
            StretchMode.Fill,
            previousStretch,
            _matrix,
            previousMatrix,
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            Bounds.Width,
            Bounds.Height,
            _element.Bounds.Width,
            _element.Bounds.Height
        );
        RaiseStretchModeChanged(args);
        
        // Add to view history after fill completes
        AddToViewHistory();
    }

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Uniform(bool skipTransitions = false)
    {
        if (_element == null)
        {
            return;
        }
        var previousStretch = Stretch;
        var previousMatrix = _matrix;
        
        Uniform(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
        
        // Raise StretchModeChanged event
        var args = new StretchModeChangedEventArgs(
            StretchMode.Uniform,
            previousStretch,
            _matrix,
            previousMatrix,
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            Bounds.Width,
            Bounds.Height,
            _element.Bounds.Width,
            _element.Bounds.Height
        );
        RaiseStretchModeChanged(args);
        
        // Add to view history after uniform completes
        AddToViewHistory();
    }

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio. If aspect of panel is different panel is filled.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void UniformToFill(bool skipTransitions = false)
    {
        if (_element == null)
        {
            return;
        }
        var previousStretch = Stretch;
        var previousMatrix = _matrix;
        
        UniformToFill(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
        
        // Raise StretchModeChanged event
        var args = new StretchModeChangedEventArgs(
            StretchMode.UniformToFill,
            previousStretch,
            _matrix,
            previousMatrix,
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            Bounds.Width,
            Bounds.Height,
            _element.Bounds.Width,
            _element.Bounds.Height
        );
        RaiseStretchModeChanged(args);
        
        // Add to view history after uniform to fill completes
        AddToViewHistory();
    }

    /// <summary>
    /// Zoom and pan child element inside panel using stretch mode.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void AutoFit(bool skipTransitions = false)
    {
        if (_element == null)
        {
            return;
        }
        var previousMatrix = _matrix;
        
        AutoFit(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
        
        // Raise AutoFitApplied event
        var args = new StretchModeChangedEventArgs(
            Stretch,
            Stretch,
            _matrix,
            previousMatrix,
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            Bounds.Width,
            Bounds.Height,
            _element.Bounds.Width,
            _element.Bounds.Height
        );
        RaiseAutoFitApplied(args);
        
        // Add to view history after auto fit completes
        AddToViewHistory();
    }

    /// <summary>
    /// Raises the PanStarted event.
    /// </summary>
    /// <param name="args">The pan event arguments.</param>
    protected virtual void RaisePanStarted(PanEventArgs args)
    {
        PanStarted?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the PanContinued event.
    /// </summary>
    /// <param name="args">The pan event arguments.</param>
    protected virtual void RaisePanContinued(PanEventArgs args)
    {
        PanContinued?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the PanEnded event.
    /// </summary>
    /// <param name="args">The pan event arguments.</param>
    protected virtual void RaisePanEnded(PanEventArgs args)
    {
        PanEnded?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the ZoomStarted event.
    /// </summary>
    /// <param name="args">The zoom event arguments.</param>
    protected virtual void RaiseZoomStarted(ZoomEventArgs args)
    {
        ZoomStarted?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the ZoomEnded event.
    /// </summary>
    /// <param name="args">The zoom event arguments.</param>
    protected virtual void RaiseZoomEnded(ZoomEventArgs args)
    {
        ZoomEnded?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the ZoomDeltaChanged event.
    /// </summary>
    /// <param name="args">The zoom event arguments.</param>
    protected virtual void RaiseZoomDeltaChanged(ZoomEventArgs args)
    {
        ZoomDeltaChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the MatrixChanged event.
    /// </summary>
    /// <param name="args">The matrix changed event arguments.</param>
    protected virtual void RaiseMatrixChanged(MatrixChangedEventArgs args)
    {
        MatrixChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the MatrixReset event.
    /// </summary>
    /// <param name="args">The matrix changed event arguments.</param>
    protected virtual void RaiseMatrixReset(MatrixChangedEventArgs args)
    {
        MatrixReset?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the StretchModeChanged event.
    /// </summary>
    /// <param name="args">The stretch mode changed event arguments.</param>
    protected virtual void RaiseStretchModeChanged(StretchModeChangedEventArgs args)
    {
        StretchModeChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the AutoFitApplied event.
    /// </summary>
    /// <param name="args">The stretch mode changed event arguments.</param>
    protected virtual void RaiseAutoFitApplied(StretchModeChangedEventArgs args)
    {
        AutoFitApplied?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the GestureStarted event.
    /// </summary>
    /// <param name="args">The gesture event arguments.</param>
    protected virtual void RaiseGestureStarted(GestureEventArgs args)
    {
        GestureStarted?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the GestureEnded event.
    /// </summary>
    /// <param name="args">The gesture event arguments.</param>
    protected virtual void RaiseGestureEnded(GestureEventArgs args)
    {
        GestureEnded?.Invoke(this, args);
    }
}
