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

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>
/// <remarks>
/// The pan and zoom logic is implemented by the UI framework independent <see cref="PanAndZoomEngine"/>
/// from the <c>PanAndZoom.Core</c> package. This control forwards Avalonia input, layout and
/// property notifications to the engine and applies the resulting render transform to its child.
/// </remarks>
[PseudoClasses(":isPanning")]
public partial class ZoomBorder : Border
{
    [Conditional("DEBUG")]
    private static void Log(string message) => Debug.WriteLine(message);

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
        return PanAndZoomEngine.CalculateMatrix(panelWidth, panelHeight, elementWidth, elementHeight, mode).ToAvalonia();
    }

    private readonly PanAndZoomEngine _engine;
    private PinchGestureRecognizer? _pinchGestureRecognizer;
    private ScrollGestureRecognizer? _scrollGestureRecognizer;
    private bool _gestureRecognizersAdded;
    private Animation.TransformOperationsTransition? _animationTransition;

    // Zoom indicator
    private DispatcherTimer? _zoomIndicatorTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoomBorder"/> class.
    /// </summary>
    public ZoomBorder()
    {
        var host = new EngineHost(this);
        _engine = new PanAndZoomEngine(host, host);
        SubscribeEngineEvents();

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

        // Subscribe to property changes
        this.GetObservable(EnableGesturesProperty).Subscribe(new AnonymousObserver<bool>(_ => UpdateGestureRecognizers()));
        this.GetObservable(StretchProperty).Subscribe(new AnonymousObserver<StretchMode>(_ => _engine.OnStretchChanged()));
        this.GetObservable(EnableAnimationsProperty).Subscribe(new AnonymousObserver<bool>(_ => UpdateAnimationTransition()));
        this.GetObservable(AnimationDurationProperty).Subscribe(new AnonymousObserver<TimeSpan>(_ => UpdateAnimationTransition()));
        this.GetObservable(BoundsModeProperty).Subscribe(new AnonymousObserver<ContentBoundsMode>(_ => _engine.OnBoundsSettingsChanged()));
        this.GetObservable(BoundsPaddingProperty).Subscribe(new AnonymousObserver<Thickness>(_ => _engine.OnBoundsSettingsChanged()));
        this.GetObservable(MinimumVisibleContentPercentageProperty).Subscribe(new AnonymousObserver<double>(_ => _engine.OnBoundsSettingsChanged()));
    }

    /// <summary>
    /// Gets the UI framework independent pan and zoom engine driving this control.
    /// </summary>
    public PanAndZoomEngine Engine => _engine;

    private void SubscribeEngineEvents()
    {
        _engine.ZoomChanged += (_, e) => OnZoomChanged(e);
        _engine.ViewHistoryChanged += (_, _) => ViewHistoryChanged?.Invoke(this, EventArgs.Empty);
        _engine.PanStarted += (_, e) => RaisePanStarted(e.ToAvalonia());
        _engine.PanContinued += (_, e) => RaisePanContinued(e.ToAvalonia());
        _engine.PanEnded += (_, e) => RaisePanEnded(e.ToAvalonia());
        _engine.ZoomStarted += (_, e) => RaiseZoomStarted(e.ToAvalonia());
        _engine.ZoomEnded += (_, e) => RaiseZoomEnded(e.ToAvalonia());
        _engine.ZoomDeltaChanged += (_, e) => RaiseZoomDeltaChanged(e.ToAvalonia());
        _engine.MatrixChanged += (_, e) => RaiseMatrixChanged(e.ToAvalonia());
        _engine.MatrixReset += (_, e) => RaiseMatrixReset(e.ToAvalonia());
        _engine.StretchModeChanged += (_, e) => RaiseStretchModeChanged(e.ToAvalonia());
        _engine.AutoFitApplied += (_, e) => RaiseAutoFitApplied(e.ToAvalonia());
        _engine.GestureStarted += (_, e) => RaiseGestureStarted(e.ToAvalonia());
        _engine.GestureEnded += (_, e) => RaiseGestureEnded(e.ToAvalonia());
        _engine.ScrollInvalidated += (_, _) => _scrollInvalidated?.Invoke(this, EventArgs.Empty);
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
            _engine.ResetGestureState();

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

        _engine.OnArranged(size.ToCore());

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

        _engine.OnAttachedToVisualTree();
    }

    private void PanAndZoom_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Log($"[DetachedFromVisualTree] {Name}");
        _engine.OnDetachedFromVisualTree();
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

    private CorePoint GetElementPosition(PointerEventArgs e)
    {
        return _element != null ? e.GetPosition(_element).ToCore() : default;
    }

    private ZoomBorderPointerButtons GetPointerButtons(PointerEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        var buttons = ZoomBorderPointerButtons.None;
        if (properties.IsLeftButtonPressed) buttons |= ZoomBorderPointerButtons.Left;
        if (properties.IsRightButtonPressed) buttons |= ZoomBorderPointerButtons.Right;
        if (properties.IsMiddleButtonPressed) buttons |= ZoomBorderPointerButtons.Middle;
        return buttons;
    }

    private void Border_Magnified(object? sender, PointerDeltaEventArgs e)
    {
        Log($"[Magnified] {Name} {e.Delta}");
        var point = e.GetPosition(_element);
        _engine.ProcessMagnify(e.Delta.X, point.ToCore());
    }

    private void Border_PinchGesture(object? sender, PinchEventArgs e)
    {
        if (_element == null)
            return;

        // Convert the pinch origin from ZoomBorder coordinates into child coordinates.
        var visualPoint = e.ScaleOrigin;
        var translatedPoint = this.TranslatePoint(visualPoint, _element);
        var zoomCenter = translatedPoint ?? visualPoint;

        // Avalonia pinch scale is cumulative since gesture start and AngleDelta is in degrees.
        if (_engine.ProcessPinch(e.Scale, e.AngleDelta, zoomCenter.ToCore()))
        {
            e.Handled = true;
        }
    }

    private void Border_PinchGestureEnded(object? sender, PinchEndedEventArgs e)
    {
        if (_engine.ProcessPinchEnded())
        {
            e.Handled = true;
        }
    }

    private void Border_ScrollGesture(object? sender, ScrollGestureEventArgs e)
    {
        if (_engine.ProcessScrollGesture(e.Delta.ToCore()))
        {
            e.Handled = true;
        }
    }

    private void Border_ScrollGestureEnded(object? sender, ScrollGestureEndedEventArgs e)
    {
        _engine.ProcessScrollGestureEnded();
        e.Handled = true;
    }

    private void Border_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_engine.ProcessPointerWheel(e.Delta.ToCore(), GetElementPosition(e), e.KeyModifiers.ToCore()))
        {
            e.Handled = true;
        }
    }

    private void Border_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_element == null)
            return;

        var point = e.GetPosition(_element);
        if (_engine.ProcessDoubleTapped(point.ToCore()))
        {
            e.Handled = true;
        }
    }

    private void Border_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_engine.ProcessKeyDown(e.Key.ToCore(), e.KeyModifiers.ToCore()))
        {
            e.Handled = true;
        }
    }

    private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _engine.ProcessPointerPressed(GetElementPosition(e), GetPointerButtons(e));
    }

    private void Border_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _engine.ProcessPointerReleased();
    }

    private void Border_PointerMoved(object? sender, PointerEventArgs e)
    {
        _engine.ProcessPointerMoved(GetElementPosition(e), GetPointerButtons(e));
    }

    private void Border_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _engine.ProcessPointerCaptureLost();
        e.Handled = true;
    }

    private void Element_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
        {
            _engine.OnElementBoundsChanged();
        }
    }

    private void BoundsChangedHandler(Rect bounds)
    {
        _engine.OnViewportSizeChanged(bounds.Size.ToCore());
    }

    /// <summary>
    /// Virtual method called when control is resized in Custom resize behavior mode.
    /// </summary>
    /// <param name="oldSize">The previous size.</param>
    /// <param name="newSize">The new size.</param>
    protected virtual void OnResized(Size oldSize, Size newSize)
    {
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
        UpdateAnimationTransition();

        // Notify the engine (schedules auto fit and refreshes command state)
        _engine.OnElementAttached();
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

        // Notify the engine (schedules auto fit and refreshes command state)
        _engine.OnElementDetached();
    }

    private void UpdateAnimationTransition()
    {
        if (_element == null)
        {
            return;
        }

        RemoveAnimationTransition();

        if (!_engine.ShouldAnimate())
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

    private void ApplyElementTransform(Matrix matrix, bool skipTransitions)
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

        var transformBuilder = new TransformOperations.Builder(1);
        transformBuilder.AppendMatrix(matrix);
        _element.RenderTransform = transformBuilder.Build();

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

    private void StartZoomIndicatorTimer(TimeSpan duration)
    {
        // Reset or start the auto-hide timer
        if (_zoomIndicatorTimer == null)
        {
            _zoomIndicatorTimer = new DispatcherTimer
            {
                Interval = duration
            };
            _zoomIndicatorTimer.Tick += (s, e) =>
            {
                _zoomIndicatorTimer?.Stop();
                _engine.HideZoomIndicator();
            };
        }

        _zoomIndicatorTimer.Stop();
        _zoomIndicatorTimer.Interval = duration;
        _zoomIndicatorTimer.Start();
    }

    /// <summary>
    /// Navigate back to the previous view state in history.
    /// </summary>
    /// <param name="animate">Whether to animate the transition.</param>
    public void NavigateBack(bool animate = true) => _engine.NavigateBack(animate);

    /// <summary>
    /// Navigate forward to the next view state in history.
    /// </summary>
    /// <param name="animate">Whether to animate the transition.</param>
    public void NavigateForward(bool animate = true) => _engine.NavigateForward(animate);

    /// <summary>
    /// Clears the view history.
    /// </summary>
    public void ClearViewHistory() => _engine.ClearViewHistory();

    /// <summary>
    /// Centers the viewport on a specific point in content coordinates.
    /// </summary>
    /// <param name="point">The point to center on.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void CenterOn(Point point, bool animate = true) => _engine.CenterOn(point.ToCore(), animate);

    /// <summary>
    /// Centers the viewport on a specific point in content coordinates with a specific zoom level.
    /// </summary>
    /// <param name="point">The point to center on.</param>
    /// <param name="zoom">The target zoom level.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void CenterOn(Point point, double zoom, bool animate = true) => _engine.CenterOn(point.ToCore(), zoom, animate);

    /// <summary>
    /// Centers the viewport on a rectangle in content coordinates.
    /// </summary>
    /// <param name="rect">The rectangle to center on.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void CenterOn(Rect rect, bool animate = true) => _engine.CenterOn(rect.ToCore(), animate);

    /// <summary>
    /// Centers the viewport on a control element.
    /// </summary>
    /// <param name="element">The element to center on.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void CenterOn(Control element, bool animate = true)
    {
        if (element == null)
            return;

        CenterOn(element.Bounds, animate);
    }

    /// <summary>
    /// Converts a viewport point to content coordinates.
    /// </summary>
    /// <param name="viewportPoint">The point in viewport coordinates.</param>
    /// <returns>The point in content coordinates.</returns>
    public Point ViewportToContent(Point viewportPoint) => _engine.ViewportToContent(viewportPoint.ToCore()).ToAvalonia();

    /// <summary>
    /// Converts a content point to viewport coordinates.
    /// </summary>
    /// <param name="contentPoint">The point in content coordinates.</param>
    /// <returns>The point in viewport coordinates.</returns>
    public Point ContentToViewport(Point contentPoint) => _engine.ContentToViewport(contentPoint.ToCore()).ToAvalonia();

    /// <summary>
    /// Converts a viewport rectangle to content coordinates.
    /// </summary>
    /// <param name="viewportRect">The rectangle in viewport coordinates.</param>
    /// <returns>The rectangle in content coordinates.</returns>
    public Rect ViewportToContent(Rect viewportRect) => _engine.ViewportToContent(viewportRect.ToCore()).ToAvalonia();

    /// <summary>
    /// Converts a content rectangle to viewport coordinates.
    /// </summary>
    /// <param name="contentRect">The rectangle in content coordinates.</param>
    /// <returns>The rectangle in viewport coordinates.</returns>
    public Rect ContentToViewport(Rect contentRect) => _engine.ContentToViewport(contentRect.ToCore()).ToAvalonia();

    /// <summary>
    /// Converts a screen vector to content vector.
    /// </summary>
    /// <param name="screenVector">The vector in screen coordinates.</param>
    /// <returns>The vector in content coordinates.</returns>
    public Vector ScreenToContent(Vector screenVector) => _engine.ScreenToContent(screenVector.ToCore()).ToAvalonia();

    /// <summary>
    /// Converts a content vector to screen vector.
    /// </summary>
    /// <param name="contentVector">The vector in content coordinates.</param>
    /// <returns>The vector in screen coordinates.</returns>
    public Vector ContentToScreen(Vector contentVector) => _engine.ContentToScreen(contentVector.ToCore()).ToAvalonia();

    /// <summary>
    /// Converts a screen size to content size.
    /// </summary>
    /// <param name="screenSize">The size in screen coordinates.</param>
    /// <returns>The size in content coordinates.</returns>
    public Size ScreenToContent(Size screenSize) => _engine.ScreenToContent(screenSize.ToCore()).ToAvalonia();

    /// <summary>
    /// Converts a content size to screen size.
    /// </summary>
    /// <param name="contentSize">The size in content coordinates.</param>
    /// <returns>The size in screen coordinates.</returns>
    public Size ContentToScreen(Size contentSize) => _engine.ContentToScreen(contentSize.ToCore()).ToAvalonia();

    /// <summary>
    /// Gets the transformation matrix from content to screen coordinates.
    /// </summary>
    /// <returns>The transformation matrix.</returns>
    public Matrix GetContentToScreenMatrix() => _engine.Matrix.ToAvalonia();

    /// <summary>
    /// Gets the transformation matrix from screen to content coordinates.
    /// </summary>
    /// <returns>The transformation matrix.</returns>
    public Matrix GetScreenToContentMatrix()
    {
        return _engine.Matrix.HasInverse ? _engine.GetScreenToContentMatrix().ToAvalonia() : default;
    }

    /// <summary>
    /// Gets the visible content bounds in content coordinates.
    /// </summary>
    /// <returns>The visible content bounds.</returns>
    public Rect GetVisibleContentBounds() => _engine.GetVisibleContentBounds().ToAvalonia();

    /// <summary>
    /// Gets the viewport bounds in viewport coordinates.
    /// </summary>
    /// <returns>The viewport bounds.</returns>
    public Rect GetViewportBounds() => _engine.GetViewportBounds().ToAvalonia();

    /// <summary>
    /// Zooms to fit a specific rectangle in content coordinates.
    /// </summary>
    /// <param name="rect">The rectangle to zoom to.</param>
    /// <param name="padding">Optional padding around the rectangle.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void ZoomToRectangle(Rect rect, Thickness? padding = null, bool animate = true)
        => _engine.ZoomToRectangle(rect.ToCore(), padding?.ToCore(), animate);

    /// <summary>
    /// Zooms to fit a specific rectangle with exact pixel dimensions.
    /// </summary>
    /// <param name="rect">The rectangle in content coordinates.</param>
    /// <param name="viewportRect">The target rectangle in viewport coordinates.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void ZoomToRectangleExact(Rect rect, Rect viewportRect, bool animate = true)
        => _engine.ZoomToRectangleExact(rect.ToCore(), viewportRect.ToCore(), animate);

    /// <summary>
    /// Saves the current view with a name.
    /// </summary>
    /// <param name="name">The name for this view.</param>
    /// <param name="description">Optional description.</param>
    public void SaveView(string name, string? description = null) => _engine.SaveView(name, description);

    /// <summary>
    /// Restores a previously saved view.
    /// </summary>
    /// <param name="name">The name of the view to restore.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    /// <returns>True if the view was found and restored, false otherwise.</returns>
    public bool RestoreView(string name, bool animate = true) => _engine.RestoreView(name, animate);

    /// <summary>
    /// Gets a saved view by name.
    /// </summary>
    /// <param name="name">The name of the view.</param>
    /// <returns>The saved view, or null if not found.</returns>
    public SavedView? GetSavedView(string name)
    {
        return _engine.GetSavedView(name) is { } view ? ToSavedView(view) : null;
    }

    /// <summary>
    /// Gets all saved view names.
    /// </summary>
    /// <returns>An array of saved view names.</returns>
    public string[] GetSavedViewNames() => _engine.GetSavedViewNames();

    /// <summary>
    /// Gets all saved views.
    /// </summary>
    /// <returns>A collection of saved views.</returns>
    public IReadOnlyCollection<SavedView> GetSavedViews()
    {
        return _engine.GetSavedViews().Select(ToSavedView).ToList().AsReadOnly();
    }

    private static SavedView ToSavedView(CoreSavedView view)
    {
        return new SavedView
        {
            Name = view.Name,
            Matrix = view.Matrix.ToAvalonia(),
            Stretch = view.Stretch,
            Description = view.Description,
            Timestamp = view.Timestamp
        };
    }

    /// <summary>
    /// Deletes a saved view.
    /// </summary>
    /// <param name="name">The name of the view to delete.</param>
    /// <returns>True if the view was found and deleted, false otherwise.</returns>
    public bool DeleteSavedView(string name) => _engine.DeleteSavedView(name);

    /// <summary>
    /// Clears all saved views.
    /// </summary>
    public void ClearSavedViews() => _engine.ClearSavedViews();

    /// <summary>
    /// Gets the next discrete zoom level up from current zoom.
    /// </summary>
    /// <returns>The next zoom level, or current zoom if at maximum.</returns>
    public double GetNextDiscreteZoomLevel() => _engine.GetNextDiscreteZoomLevel();

    /// <summary>
    /// Gets the previous discrete zoom level down from current zoom.
    /// </summary>
    /// <returns>The previous zoom level, or current zoom if at minimum.</returns>
    public double GetPreviousDiscreteZoomLevel() => _engine.GetPreviousDiscreteZoomLevel();

    /// <summary>
    /// Zooms to a specific discrete level.
    /// </summary>
    /// <param name="level">The zoom level to zoom to.</param>
    /// <param name="centerX">The x-coordinate of the zoom center point.</param>
    /// <param name="centerY">The y-coordinate of the zoom center point.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void ZoomToLevel(double level, double centerX, double centerY, bool animate = true)
        => _engine.ZoomToLevel(level, centerX, centerY, animate);

    /// <summary>
    /// Determines if a rectangle in content coordinates is visible in the viewport.
    /// </summary>
    /// <param name="rect">The rectangle in content coordinates.</param>
    /// <returns>True if any part of the rectangle is visible.</returns>
    public bool IsRectangleVisible(Rect rect) => _engine.IsRectangleVisible(rect.ToCore());

    /// <summary>
    /// Determines if a point in content coordinates is visible in the viewport.
    /// </summary>
    /// <param name="point">The point in content coordinates.</param>
    /// <returns>True if the point is visible.</returns>
    public bool IsPointVisible(Point point) => _engine.IsPointVisible(point.ToCore());

    /// <summary>
    /// Gets the intersection of a rectangle with the visible content bounds.
    /// </summary>
    /// <param name="rect">The rectangle in content coordinates.</param>
    /// <returns>The visible portion of the rectangle.</returns>
    public Rect GetVisiblePortion(Rect rect) => _engine.GetVisiblePortion(rect.ToCore()).ToAvalonia();

    /// <summary>
    /// Calculates automatic zoom limits based on content and viewport size.
    /// </summary>
    /// <returns>A tuple containing the minimum and maximum zoom values.</returns>
    protected virtual (double minZoom, double maxZoom) CalculateAutoZoomLimits()
    {
        return _engine.CalculateDefaultAutoZoomLimits();
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
        _engine.GetEffectiveZoomLimits(out minZoomX, out maxZoomX, out minZoomY, out maxZoomY);
    }

    /// <summary>
    /// Gets the zoom indicator text based on the current zoom level.
    /// </summary>
    /// <returns>The formatted zoom indicator text.</returns>
    public string GetZoomIndicatorText() => _engine.GetZoomIndicatorText();

    /// <summary>
    /// Gets the position for the zoom indicator based on the configured position.
    /// </summary>
    /// <returns>A point representing the indicator position.</returns>
    protected virtual Point GetZoomIndicatorPosition()
    {
        return PanAndZoomEngine.CalculateZoomIndicatorPosition(ZoomIndicatorPosition, Bounds.Size.ToCore()).ToAvalonia();
    }

    /// <summary>
    /// Snaps a value to the nearest grid point.
    /// </summary>
    /// <param name="value">The value to snap.</param>
    /// <returns>The snapped value.</returns>
    public double SnapToGrid(double value) => _engine.SnapToGrid(value);

    /// <summary>
    /// Snaps a point to the nearest grid point.
    /// </summary>
    /// <param name="point">The point to snap.</param>
    /// <returns>The snapped point.</returns>
    public Point SnapToGrid(Point point) => _engine.SnapToGrid(point.ToCore()).ToAvalonia();

    /// <summary>
    /// Snaps a rectangle to the nearest grid points.
    /// </summary>
    /// <param name="rect">The rectangle to snap.</param>
    /// <returns>The snapped rectangle.</returns>
    public Rect SnapToGrid(Rect rect) => _engine.SnapToGrid(rect.ToCore()).ToAvalonia();

    /// <summary>
    /// Rotates the content by the specified angle in degrees.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    /// <param name="animate">Whether to animate the rotation.</param>
    public void Rotate(double degrees, bool animate = true) => _engine.Rotate(degrees, animate);

    /// <summary>
    /// Rotates the content by the specified angle around a center point.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    /// <param name="center">The center point for rotation in content coordinates.</param>
    /// <param name="animate">Whether to animate the rotation.</param>
    public void RotateAt(double degrees, Point center, bool animate = true) => _engine.RotateAt(degrees, center.ToCore(), animate);

    /// <summary>
    /// Resets the rotation to zero.
    /// </summary>
    /// <param name="animate">Whether to animate the reset.</param>
    public void ResetRotation(bool animate = true) => _engine.ResetRotation(animate);

    /// <summary>
    /// Snaps the current rotation to the nearest snap angle.
    /// </summary>
    public void SnapRotation() => _engine.SnapRotation();

    /// <summary>
    /// Exports the current state of the ZoomBorder control.
    /// </summary>
    /// <returns>A ZoomBorderState object containing the current state.</returns>
    public ZoomBorderState ExportState()
    {
        var state = _engine.ExportState();
        return new ZoomBorderState
        {
            Matrix = state.Matrix.ToAvalonia(),
            Stretch = state.Stretch,
            ZoomSpeed = state.ZoomSpeed,
            EnablePan = state.EnablePan,
            EnableZoom = state.EnableZoom,
            Rotation = state.Rotation,
            MinZoomX = state.MinZoomX,
            MaxZoomX = state.MaxZoomX,
            MinZoomY = state.MinZoomY,
            MaxZoomY = state.MaxZoomY,
            EnableConstrains = state.EnableConstrains,
            EnableAnimations = state.EnableAnimations,
            AnimationDuration = state.AnimationDuration,
            Timestamp = state.Timestamp
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

        _engine.ImportState(new CoreZoomBorderState
        {
            Matrix = state.Matrix.ToCore(),
            Stretch = state.Stretch,
            ZoomSpeed = state.ZoomSpeed,
            EnablePan = state.EnablePan,
            EnableZoom = state.EnableZoom,
            Rotation = state.Rotation,
            MinZoomX = state.MinZoomX,
            MaxZoomX = state.MaxZoomX,
            MinZoomY = state.MinZoomY,
            MaxZoomY = state.MaxZoomY,
            EnableConstrains = state.EnableConstrains,
            EnableAnimations = state.EnableAnimations,
            AnimationDuration = state.AnimationDuration,
            Timestamp = state.Timestamp
        }, animate);
    }

    /// <summary>
    /// Updates accessibility descriptions based on current state.
    /// </summary>
    public void UpdateAccessibilityDescriptions() => _engine.UpdateAccessibilityDescriptions();

    /// <summary>
    /// Gets the current accessibility description.
    /// </summary>
    /// <returns>A combined accessibility description.</returns>
    public string GetAccessibilityDescription() => _engine.GetAccessibilityDescription();

    /// <summary>
    /// Raises <see cref="ZoomChanged"/> event.
    /// </summary>
    /// <param name="e">Zoom changed event arguments.</param>
    protected virtual void OnZoomChanged(ZoomChangedEventArgs e)
    {
        ZoomChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Virtual method to get custom content bounds.
    /// </summary>
    /// <returns>The content bounds rectangle.</returns>
    protected virtual Rect GetContentBounds()
    {
        return _engine.GetDefaultContentBounds().ToAvalonia();
    }

    /// <summary>
    /// Reapplies custom bounds restrictions and refreshes the logical scroll state.
    /// Call this from a subclass when the value returned by <see cref="GetContentBounds"/> changes.
    /// </summary>
    protected void InvalidateContentBounds()
    {
        _engine.Refresh(skipTransitions: true);
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

    /// <summary>
    /// Set pan and zoom matrix.
    /// </summary>
    /// <param name="matrix">The matrix to set as current.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void SetMatrix(Matrix matrix, bool skipTransitions = false) => _engine.SetMatrix(matrix.ToCore(), skipTransitions);

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
    public void ResetMatrix(bool skipTransitions) => _engine.ResetMatrix(skipTransitions);

    /// <summary>
    /// Zoom to provided zoom value and provided center point.
    /// </summary>
    /// <param name="zoom">The zoom value.</param>
    /// <param name="x">The center point x axis coordinate.</param>
    /// <param name="y">The center point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Zoom(double zoom, double x, double y, bool skipTransitions = false) => _engine.Zoom(zoom, x, y, skipTransitions);

    /// <summary>
    /// Zoom to provided zoom ratio and provided center point.
    /// </summary>
    /// <param name="ratio">The zoom ratio.</param>
    /// <param name="x">The center point x axis coordinate.</param>
    /// <param name="y">The center point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomTo(double ratio, double x, double y, bool skipTransitions = false) => _engine.ZoomTo(ratio, x, y, skipTransitions);

    /// <summary>
    /// Zoom in one step positive delta ratio and panel center point.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomIn(bool skipTransitions = false) => _engine.ZoomIn(skipTransitions);

    /// <summary>
    /// Zoom out one step positive delta ratio and panel center point.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomOut(bool skipTransitions = false) => _engine.ZoomOut(skipTransitions);

    /// <summary>
    /// Zoom to provided zoom delta ratio and provided center point.
    /// </summary>
    /// <param name="delta">The zoom delta ratio.</param>
    /// <param name="x">The center point x axis coordinate.</param>
    /// <param name="y">The center point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomDeltaTo(double delta, double x, double y, bool skipTransitions = false) => _engine.ZoomDeltaTo(delta, x, y, skipTransitions);

    /// <summary>
    /// Pan control to provided delta.
    /// </summary>
    /// <param name="dx">The target x axis delta.</param>
    /// <param name="dy">The target y axis delta.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void PanDelta(double dx, double dy, bool skipTransitions = false) => _engine.PanDelta(dx, dy, skipTransitions);

    /// <summary>
    /// Pan control to set the viewport offset to the specified values.
    /// </summary>
    /// <param name="x">The target offset x value.</param>
    /// <param name="y">The target offset y value.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Pan(double x, double y, bool skipTransitions = false) => _engine.Pan(x, y, skipTransitions);

    /// <summary>
    /// Set pan origin.
    /// </summary>
    /// <param name="x">The origin point x axis coordinate.</param>
    /// <param name="y">The origin point y axis coordinate.</param>
    public void BeginPanTo(double x, double y) => _engine.BeginPanTo(x, y);

    /// <summary>
    /// Continue pan to provided target point.
    /// </summary>
    /// <param name="x">The target point x axis coordinate.</param>
    /// <param name="y">The target point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ContinuePanTo(double x, double y, bool skipTransitions = false) => _engine.ContinuePanTo(x, y, skipTransitions);

    /// <summary>
    /// Zoom and pan.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void None(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        => _engine.None(panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);

    /// <summary>
    /// Zoom and pan to fill panel.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Fill(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        => _engine.Fill(panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Uniform(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        => _engine.Uniform(panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio. If aspect of panel is different panel is filled.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void UniformToFill(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        => _engine.UniformToFill(panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);

    /// <summary>
    /// Zoom and pan child element inside panel using stretch mode.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void AutoFit(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        => _engine.AutoFit(panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);

    /// <summary>
    /// Set next stretch mode.
    /// </summary>
    public void ToggleStretchMode() => _engine.ToggleStretchMode();

    /// <summary>
    /// Zoom and pan.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void None(bool skipTransitions = false) => _engine.None(skipTransitions);

    /// <summary>
    /// Zoom and pan to fill panel.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Fill(bool skipTransitions = false) => _engine.Fill(skipTransitions);

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Uniform(bool skipTransitions = false) => _engine.Uniform(skipTransitions);

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio. If aspect of panel is different panel is filled.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void UniformToFill(bool skipTransitions = false) => _engine.UniformToFill(skipTransitions);

    /// <summary>
    /// Zoom and pan child element inside panel using stretch mode.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void AutoFit(bool skipTransitions = false) => _engine.AutoFit(skipTransitions);

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
