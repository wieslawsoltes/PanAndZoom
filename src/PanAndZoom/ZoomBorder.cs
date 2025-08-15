using System;
using System.Diagnostics;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Reactive;
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
    private bool _gestureRecognizersAdded = false;

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
        this.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver<Rect>(BoundsChanged));
        Gestures.AddPointerTouchPadGestureMagnifyHandler(this, Border_Magnified);
        
        // Initialize gesture recognizers
        _pinchGestureRecognizer = new PinchGestureRecognizer();
        _scrollGestureRecognizer = new ScrollGestureRecognizer
        {
            CanHorizontallyScroll = true,
            CanVerticallyScroll = true
        };
        
        // Add gesture event handlers
        AddHandler(Gestures.PinchEvent, Border_PinchGesture);
        AddHandler(Gestures.PinchEndedEvent, Border_PinchGestureEnded);
        AddHandler(Gestures.ScrollGestureEvent, Border_ScrollGesture);
        AddHandler(Gestures.ScrollGestureEndedEvent, Border_ScrollGestureEnded);
        
        // Add gesture recognizers based on EnableGestures flag
        UpdateGestureRecognizers();
        
        // Subscribe to EnableGestures property changes
        this.GetObservable(EnableGesturesProperty).Subscribe(new AnonymousObserver<bool>(_ => UpdateGestureRecognizers()));
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
            // Since GestureRecognizerCollection doesn't support Remove/Clear,
            // we need to recreate the recognizers to effectively "remove" them
            _pinchGestureRecognizer = new PinchGestureRecognizer();
            _scrollGestureRecognizer = new ScrollGestureRecognizer
            {
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true
            };
            
            // Re-add event handlers to new recognizers
            AddHandler(Gestures.PinchEvent, Border_PinchGesture);
            AddHandler(Gestures.PinchEndedEvent, Border_PinchGestureEnded);
            AddHandler(Gestures.ScrollGestureEvent, Border_ScrollGesture);
            AddHandler(Gestures.ScrollGestureEndedEvent, Border_ScrollGestureEnded);
            
            _gestureRecognizersAdded = false;
        }
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

        AutoFit(size.Width, size.Height, _element.Bounds.Width, _element.Bounds.Height);

        return size;
    }

    private void PanAndZoom_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Log($"[AttachedToVisualTree] {Name}");
        ChildChanged(Child);

        // Update gesture recognizers based on the new state
        UpdateGestureRecognizers();

        _updating = true;
        Invalidate(skipTransitions: false);
        _updating = false;
    }

    private void PanAndZoom_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Log($"[DetachedFromVisualTree] {Name}");
        DetachElement();
    }

    private void Border_Magnified(object? sender, PointerDeltaEventArgs e)
    {
        Log($"[Magnified] {Name} {e.Delta}");
        var point = e.GetPosition(_element);
        ZoomDeltaTo(e.Delta.X, point.X, point.Y);
    }

    private void Border_PinchGesture(object? sender, PinchEventArgs e)
    {
        if (!EnableGestureZoom || _element == null)
            return;

        Log($"[PinchGesture] {Name} Scale: {e.Scale}");
        
        var point = e.ScaleOrigin;
        var elementPoint = new Point(point.X * _element.Bounds.Width, point.Y * _element.Bounds.Height);
        
        // Calculate zoom delta based on scale change
        var zoomDelta = e.Scale - 1.0;
        ZoomDeltaTo(zoomDelta, elementPoint.X, elementPoint.Y);
        
        e.Handled = true;
    }

    private void Border_PinchGestureEnded(object? sender, PinchEndedEventArgs e)
    {
        Log($"[PinchGestureEnded] {Name}");
        e.Handled = true;
    }

    private void Border_ScrollGesture(object? sender, ScrollGestureEventArgs e)
    {
        if (!EnableGestureTranslation)
            return;

        Log($"[ScrollGesture] {Name} Delta: {e.Delta}");
        
        // Use the scroll delta for panning
        PanDelta(e.Delta.X, e.Delta.Y);
        
        e.Handled = true;
    }

    private void Border_ScrollGestureEnded(object? sender, ScrollGestureEndedEventArgs e)
    {
        Log($"[ScrollGestureEnded] {Name}");
        e.Handled = true;
    }

    private void Border_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (CanZoomOnPointerWheel(e))
        {
            Wheel(e);
            e.Handled = true;
        }
        else if (CanPanOnPointerWheel(e))
        {
            PanDelta(10 * e.Delta.X, 10 * e.Delta.Y);
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

    private void Border_PointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
    {
        CaptureLost(sender, e);
    }

    private void Element_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
        {
            InvalidateScrollable();
        }
    }

    private void BoundsChanged(Rect bounds)
    {
        // Log($"[BoundsChanged] {bounds}");
        InvalidateScrollable();
    }

    private void ChildChanged(Control? element)
    {
        Log($"[ChildChanged] {element}");

        if (element != null && element != _element && _element != null)
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
        PointerCaptureLost += Border_PointerCaptureLost;
        PointerWheelChanged += Border_PointerWheelChanged;
        PointerPressed += Border_PointerPressed;
        PointerReleased += Border_PointerReleased;
        PointerMoved += Border_PointerMoved;

    }

    private void DetachElement()
    {
        if (_element == null)
        {
            return;
        }

        PointerCaptureLost -= Border_PointerCaptureLost;
        PointerWheelChanged -= Border_PointerWheelChanged;
        PointerPressed -= Border_PointerPressed;
        PointerReleased -= Border_PointerReleased;
        PointerMoved -= Border_PointerMoved;
        _element.PropertyChanged -= Element_PropertyChanged;
        _element.RenderTransform = null;
        _element = null;
    }

    private void Wheel(PointerWheelEventArgs e)
    {
        if (_element == null || _captured)
        {
            return;
        }
        var point = e.GetPosition(_element);
        ZoomDeltaTo(e.Delta.Y, point.X, point.Y);
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
            SetPseudoClass(":isPanning", _isPanning);
        }
    }

    // ReSharper disable once UnusedParameter.Local
    private void Released(PointerReleasedEventArgs e) => PanningFinished();

    private void CaptureLost(object sender, PointerCaptureLostEventArgs e) => PanningFinished();

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
        _captured = false;
        _isPanning = false;
        SetPseudoClass(":isPanning", _isPanning);
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
        var zoomX = ClampValue(_matrix.M11, MinZoomX, MaxZoomX);
        var zoomY = ClampValue(_matrix.M22, MinZoomY, MaxZoomY);
        var offsetX = ClampValue(_matrix.M31, MinOffsetX, MaxOffsetX);
        var offsetY = ClampValue(_matrix.M32, MinOffsetY, MaxOffsetY);
        _matrix = new Matrix(zoomX, 0.0, 0.0, zoomY, offsetX, offsetY);
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

        InvalidateProperties();
        InvalidateScrollable();
        InvalidateElement(skipTransitions);
        RaiseZoomChanged();

        Log("[Invalidate] End");
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
        _transformBuilder = new TransformOperations.Builder(1);
        _transformBuilder.AppendMatrix(_matrix);
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
        _matrix = matrix;
        Invalidate(skipTransitions);

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
        SetMatrix(Matrix.Identity, skipTransitions);
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
        _updating = true;

        Log("[Zoom]");
        _matrix = MatrixHelper.ScaleAt(zoom, zoom, x, y);
        Invalidate(skipTransitions);

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

        if ((ZoomX >= MaxZoomX && ZoomY >= MaxZoomY && ratio > 1) || (ZoomX <= MinZoomX && ZoomY <= MinZoomY && ratio < 1))
        {
            return;
        }

        _updating = true;

        Log("[ZoomTo]");
        _matrix = MatrixHelper.ScaleAtPrepend(_matrix, ratio, ratio, x, y);
        Invalidate(skipTransitions);

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

        var x = _element.Bounds.Width / 2.0;
        var y = _element.Bounds.Height / 2.0;
        ZoomTo(ZoomSpeed, x, y, skipTransitions);
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

        var x = _element.Bounds.Width / 2.0;
        var y = _element.Bounds.Height / 2.0;
        ZoomTo(1 / ZoomSpeed, x, y, skipTransitions);
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
        _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, _matrix.M31 + dx, _matrix.M32 + dy);
        Invalidate(skipTransitions);

        _updating = false;
    }

    /// <summary>
    /// Pan control to provided target point.
    /// </summary>
    /// <param name="x">The target point x axis coordinate.</param>
    /// <param name="y">The target point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Pan(double x, double y, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log("[Pan]");
        _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, x, y);
        Invalidate(skipTransitions);

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
        var dx = x - _previous.X;
        var dy = y - _previous.Y;
        var delta = new Point(dx, dy);
        _previous = new Point(x, y);
        _pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
        _matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);
        Invalidate(skipTransitions);

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
        Fill(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
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
        Uniform(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
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
        UniformToFill(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
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
        AutoFit(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
    }

    private void SetPseudoClass(string name, bool flag) => PseudoClasses.Set(name, flag);
}
