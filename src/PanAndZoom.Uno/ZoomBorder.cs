// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;

namespace PanAndZoom;

/// <summary>
/// Pan and zoom control for Uno Platform (WinUI).
/// </summary>
/// <remarks>
/// <para>
/// The pan and zoom logic is implemented by the UI framework independent <see cref="PanAndZoomEngine"/>
/// from the <c>PanAndZoom.Core</c> package (shared with the Avalonia <c>ZoomBorder</c>). This control
/// forwards WinUI input, layout and property notifications to the engine and applies the resulting
/// render transform to its <see cref="Child"/>.
/// </para>
/// <para>
/// Mouse input pans with <see cref="PanButton"/> and zooms with the wheel, touch input uses
/// manipulations (one finger pans, two fingers pinch zoom and rotate) and keyboard navigation is
/// available when the control has focus.
/// </para>
/// </remarks>
[ContentProperty(Name = nameof(Child))]
[TemplatePart(Name = BorderPartName, Type = typeof(Border))]
[TemplateVisualState(GroupName = PanningStatesGroupName, Name = NotPanningStateName)]
[TemplateVisualState(GroupName = PanningStatesGroupName, Name = PanningStateName)]
public partial class ZoomBorder : Control
{
    private const string BorderPartName = "PART_Border";
    private const string PanningStatesGroupName = "PanningStates";
    private const string NotPanningStateName = "NotPanning";
    private const string PanningStateName = "Panning";

    [Conditional("DEBUG")]
    private static void Log(string message) => Debug.WriteLine(message);

    /// <summary>
    /// Gets available stretch modes.
    /// </summary>
    public static StretchMode[] StretchModes { get; } = (StretchMode[])Enum.GetValues(typeof(StretchMode));

    /// <summary>
    /// Gets available button names.
    /// </summary>
    public static ButtonName[] ButtonNames { get; } = (ButtonName[])Enum.GetValues(typeof(ButtonName));

    /// <summary>
    /// Identifies the <see cref="Child"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ChildProperty =
        DependencyProperty.Register(nameof(Child), typeof(UIElement), typeof(ZoomBorder), new PropertyMetadata(null, OnChildPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="ClipToBounds"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ClipToBoundsProperty =
        DependencyProperty.Register(nameof(ClipToBounds), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(true, OnClipToBoundsPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="ZoomX"/> dependency property (read-only, updated by the control).
    /// </summary>
    public static readonly DependencyProperty ZoomXProperty =
        DependencyProperty.Register(nameof(ZoomX), typeof(double), typeof(ZoomBorder), new PropertyMetadata(1.0));

    /// <summary>
    /// Identifies the <see cref="ZoomY"/> dependency property (read-only, updated by the control).
    /// </summary>
    public static readonly DependencyProperty ZoomYProperty =
        DependencyProperty.Register(nameof(ZoomY), typeof(double), typeof(ZoomBorder), new PropertyMetadata(1.0));

    /// <summary>
    /// Identifies the <see cref="OffsetX"/> dependency property (read-only, updated by the control).
    /// </summary>
    public static readonly DependencyProperty OffsetXProperty =
        DependencyProperty.Register(nameof(OffsetX), typeof(double), typeof(ZoomBorder), new PropertyMetadata(0.0));

    /// <summary>
    /// Identifies the <see cref="OffsetY"/> dependency property (read-only, updated by the control).
    /// </summary>
    public static readonly DependencyProperty OffsetYProperty =
        DependencyProperty.Register(nameof(OffsetY), typeof(double), typeof(ZoomBorder), new PropertyMetadata(0.0));

    /// <summary>
    /// Identifies the <see cref="IsZoomIndicatorVisible"/> dependency property (read-only, updated by the control).
    /// </summary>
    public static readonly DependencyProperty IsZoomIndicatorVisibleProperty =
        DependencyProperty.Register(nameof(IsZoomIndicatorVisible), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(false));

    /// <summary>
    /// Identifies the <see cref="IsPanning"/> dependency property (read-only, updated by the control).
    /// </summary>
    public static readonly DependencyProperty IsPanningProperty =
        DependencyProperty.Register(nameof(IsPanning), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(false));

    /// <summary>
    /// Calculate pan and zoom matrix based on provided stretch mode.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="mode">The stretch mode.</param>
    /// <returns>The calculated matrix.</returns>
    public static Matrix CalculateMatrix(double panelWidth, double panelHeight, double elementWidth, double elementHeight, StretchMode mode)
    {
        return PanAndZoomEngine.CalculateMatrix(panelWidth, panelHeight, elementWidth, elementHeight, mode).ToUno();
    }

    private readonly PanAndZoomEngine _engine;
    private readonly MatrixTransition _transition = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private MatrixTransform? _renderTransform;
    private CoreMatrix _renderMatrix = CoreMatrix.Identity;
    private bool _renderingHooked;
    private FrameworkElement? _element;
    private Border? _border;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _zoomIndicatorTimer;
    private Pointer? _capturedPointer;
    private bool _manipulationPinch;
    private bool _manipulationScroll;
    private bool _manipulationActive;
    private Rect _lastElementRect;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoomBorder"/> class.
    /// </summary>
    public ZoomBorder()
    {
        DefaultStyleKey = typeof(ZoomBorder);

        var host = new EngineHost(this);
        _engine = new PanAndZoomEngine(host, host);
        SubscribeEngineEvents();

        IsTabStop = true;
        UpdateManipulationMode();

        Loaded += ZoomBorder_Loaded;
        Unloaded += ZoomBorder_Unloaded;
        SizeChanged += ZoomBorder_SizeChanged;
        BringIntoViewRequested += ZoomBorder_BringIntoViewRequested;
    }

    /// <summary>
    /// Gets the UI framework independent pan and zoom engine driving this control.
    /// </summary>
    public PanAndZoomEngine Engine => _engine;

    /// <summary>
    /// Gets or sets the child element that is panned and zoomed.
    /// </summary>
    public UIElement? Child
    {
        get => (UIElement?)GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the content is clipped to the control bounds. The default is <c>true</c>.
    /// </summary>
    public bool ClipToBounds
    {
        get => (bool)GetValue(ClipToBoundsProperty);
        set => SetValue(ClipToBoundsProperty, value);
    }

    /// <summary>
    /// Gets the render transform matrix.
    /// </summary>
    public Matrix Matrix => _engine.Matrix.ToUno();

    /// <summary>
    /// Gets the zoom ratio for x axis.
    /// </summary>
    public double ZoomX
    {
        get => (double)GetValue(ZoomXProperty);
        private set => SetValue(ZoomXProperty, value);
    }

    /// <summary>
    /// Gets the zoom ratio for y axis.
    /// </summary>
    public double ZoomY
    {
        get => (double)GetValue(ZoomYProperty);
        private set => SetValue(ZoomYProperty, value);
    }

    /// <summary>
    /// Gets the pan offset for x axis.
    /// </summary>
    public double OffsetX
    {
        get => (double)GetValue(OffsetXProperty);
        private set => SetValue(OffsetXProperty, value);
    }

    /// <summary>
    /// Gets the pan offset for y axis.
    /// </summary>
    public double OffsetY
    {
        get => (double)GetValue(OffsetYProperty);
        private set => SetValue(OffsetYProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the zoom indicator is currently visible.
    /// This property is read-only and controlled by the auto-hide timer.
    /// </summary>
    public bool IsZoomIndicatorVisible => (bool)GetValue(IsZoomIndicatorVisibleProperty);

    /// <summary>
    /// Gets a value indicating whether a pointer pan is in progress.
    /// </summary>
    public bool IsPanning => (bool)GetValue(IsPanningProperty);

    /// <summary>
    /// Gets a value indicating whether a render transform animation is currently running.
    /// </summary>
    public bool IsAnimating => _transition.IsRunning;

    /// <summary>
    /// Gets a value indicating whether the control can navigate back in view history.
    /// </summary>
    public bool CanNavigateBack => _engine.CanNavigateBack;

    /// <summary>
    /// Gets a value indicating whether the control can navigate forward in view history.
    /// </summary>
    public bool CanNavigateForward => _engine.CanNavigateForward;

    /// <summary>
    /// Gets the command to zoom in.
    /// </summary>
    public System.Windows.Input.ICommand ZoomInCommand => _engine.ZoomInCommand;

    /// <summary>
    /// Gets the command to zoom out.
    /// </summary>
    public System.Windows.Input.ICommand ZoomOutCommand => _engine.ZoomOutCommand;

    /// <summary>
    /// Gets the command to reset the view.
    /// </summary>
    public System.Windows.Input.ICommand ResetCommand => _engine.ResetCommand;

    /// <summary>
    /// Gets the command to fit content to viewport.
    /// </summary>
    public System.Windows.Input.ICommand FitCommand => _engine.FitCommand;

    /// <summary>
    /// Gets the command to fill viewport.
    /// </summary>
    public System.Windows.Input.ICommand FillCommand => _engine.FillCommand;

    /// <summary>
    /// Gets the command to apply uniform stretch.
    /// </summary>
    public System.Windows.Input.ICommand UniformCommand => _engine.UniformCommand;

    /// <summary>
    /// Gets the command to apply uniform to fill stretch.
    /// </summary>
    public System.Windows.Input.ICommand UniformToFillCommand => _engine.UniformToFillCommand;

    /// <summary>
    /// Gets the command to navigate back in view history.
    /// </summary>
    public System.Windows.Input.ICommand NavigateBackCommand => _engine.NavigateBackCommand;

    /// <summary>
    /// Gets the command to navigate forward in view history.
    /// </summary>
    public System.Windows.Input.ICommand NavigateForwardCommand => _engine.NavigateForwardCommand;

    /// <summary>
    /// Gets the command to toggle stretch mode.
    /// </summary>
    public System.Windows.Input.ICommand ToggleStretchCommand => _engine.ToggleStretchCommand;

    /// <summary>
    /// Zoom changed event.
    /// </summary>
    public event ZoomChangedEventHandler? ZoomChanged;

    /// <summary>
    /// View history changed event.
    /// </summary>
    public event EventHandler? ViewHistoryChanged;

    /// <summary>
    /// Pan started event.
    /// </summary>
    public event PanEventHandler? PanStarted;

    /// <summary>
    /// Pan continued event.
    /// </summary>
    public event PanEventHandler? PanContinued;

    /// <summary>
    /// Pan ended event.
    /// </summary>
    public event PanEventHandler? PanEnded;

    /// <summary>
    /// Zoom started event.
    /// </summary>
    public event ZoomEventHandler? ZoomStarted;

    /// <summary>
    /// Zoom ended event.
    /// </summary>
    public event ZoomEventHandler? ZoomEnded;

    /// <summary>
    /// Zoom delta changed event.
    /// </summary>
    public event ZoomEventHandler? ZoomDeltaChanged;

    /// <summary>
    /// Matrix changed event.
    /// </summary>
    public event MatrixChangedEventHandler? MatrixChanged;

    /// <summary>
    /// Matrix reset event.
    /// </summary>
    public event MatrixChangedEventHandler? MatrixReset;

    /// <summary>
    /// Stretch mode changed event.
    /// </summary>
    public event StretchModeChangedEventHandler? StretchModeChanged;

    /// <summary>
    /// Auto fit applied event.
    /// </summary>
    public event StretchModeChangedEventHandler? AutoFitApplied;

    /// <summary>
    /// Gesture started event.
    /// </summary>
    public event GestureEventHandler? GestureStarted;

    /// <summary>
    /// Gesture ended event.
    /// </summary>
    public event GestureEventHandler? GestureEnded;

    private void SubscribeEngineEvents()
    {
        _engine.ZoomChanged += (_, e) => OnZoomChanged(e);
        _engine.ViewHistoryChanged += (_, _) => ViewHistoryChanged?.Invoke(this, EventArgs.Empty);
        _engine.PanStarted += (_, e) => RaisePanStarted(e.ToUno());
        _engine.PanContinued += (_, e) => RaisePanContinued(e.ToUno());
        _engine.PanEnded += (_, e) => RaisePanEnded(e.ToUno());
        _engine.ZoomStarted += (_, e) => RaiseZoomStarted(e.ToUno());
        _engine.ZoomEnded += (_, e) => RaiseZoomEnded(e.ToUno());
        _engine.ZoomDeltaChanged += (_, e) => RaiseZoomDeltaChanged(e.ToUno());
        _engine.MatrixChanged += (_, e) => RaiseMatrixChanged(e.ToUno());
        _engine.MatrixReset += (_, e) => RaiseMatrixReset(e.ToUno());
        _engine.StretchModeChanged += (_, e) => RaiseStretchModeChanged(e.ToUno());
        _engine.AutoFitApplied += (_, e) => RaiseAutoFitApplied(e.ToUno());
        _engine.GestureStarted += (_, e) => RaiseGestureStarted(e.ToUno());
        _engine.GestureEnded += (_, e) => RaiseGestureEnded(e.ToUno());
        _engine.ScrollInvalidated += (_, _) => ScrollInvalidated?.Invoke(this, EventArgs.Empty);
    }

    #region Property changed callbacks

    private static void OnChildPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var zoomBorder = (ZoomBorder)d;
        if (zoomBorder._border != null)
        {
            zoomBorder._border.Child = e.NewValue as UIElement;
        }

        zoomBorder.ChildChanged(e.NewValue as FrameworkElement);
        zoomBorder.InvalidateMeasure();
    }

    private static void OnClipToBoundsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var zoomBorder = (ZoomBorder)d;
        zoomBorder.UpdateClip(new Size(zoomBorder.ActualWidth, zoomBorder.ActualHeight));
    }

    private static void OnArrangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ZoomBorder)d).InvalidateArrange();
    }

    private static void OnStretchPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var zoomBorder = (ZoomBorder)d;
        zoomBorder._engine.OnStretchChanged();
        zoomBorder.InvalidateArrange();
    }

    private static void OnBoundsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ZoomBorder)d)._engine.OnBoundsSettingsChanged();
    }

    private static void OnGesturesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var zoomBorder = (ZoomBorder)d;
        if (!zoomBorder.EnableGestures)
        {
            zoomBorder._engine.ResetGestureState();
        }

        zoomBorder.UpdateManipulationMode();
        zoomBorder.InvalidateArrange();
    }

    private void UpdateManipulationMode()
    {
        ManipulationMode = EnableGestures
            ? ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.Scale | ManipulationModes.Rotate
            : ManipulationModes.System;
    }

    #endregion

    #region Template and layout

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_border != null)
        {
            _border.Child = null;
        }

        _border = GetTemplateChild(BorderPartName) as Border;

        if (_border != null)
        {
            _border.Child = Child;
        }

        VisualStateManager.GoToState(this, IsPanning ? PanningStateName : NotPanningStateName, false);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);

        if (_element != null && IsElementHosted())
        {
            var elementRect = new Rect(GetElementLayoutOffset().ToUno(), new Size(_element.ActualWidth, _element.ActualHeight));
            if (elementRect != _lastElementRect)
            {
                _lastElementRect = elementRect;
                _engine.OnElementBoundsChanged();
            }

            _engine.OnArranged(size.ToCore());
        }

        UpdateClip(size);

        return size;
    }

    private bool IsElementHosted()
    {
        return _element != null && _border != null && ReferenceEquals(_border.Child, _element);
    }

    private void UpdateClip(Size size)
    {
        if (ClipToBounds && size.Width > 0 && size.Height > 0)
        {
            if (Clip is RectangleGeometry geometry)
            {
                geometry.Rect = new Rect(0, 0, size.Width, size.Height);
            }
            else
            {
                Clip = new RectangleGeometry { Rect = new Rect(0, 0, size.Width, size.Height) };
            }
        }
        else if (!ClipToBounds && Clip != null)
        {
            Clip = null;
        }
    }

    private CoreSize GetViewportSize()
    {
        return new CoreSize(ActualWidth, ActualHeight);
    }

    private CoreSize GetElementSize()
    {
        return _element == null ? default : new CoreSize(_element.ActualWidth, _element.ActualHeight);
    }

    /// <summary>
    /// Gets the layout offset of the child element relative to this control by accumulating
    /// <see cref="UIElement.ActualOffset"/> of the child and its ancestors up to this control.
    /// Render transforms are not included.
    /// </summary>
    private CorePoint GetElementLayoutOffset()
    {
        if (_element == null)
        {
            return default;
        }

        double x = 0;
        double y = 0;
        DependencyObject? current = _element;

        while (current is UIElement element && !ReferenceEquals(current, this))
        {
            var offset = element.ActualOffset;
            x += offset.X;
            y += offset.Y;
            current = VisualTreeHelper.GetParent(current);
        }

        return ReferenceEquals(current, this) ? new CorePoint(x, y) : default;
    }

    private void ZoomBorder_Loaded(object sender, RoutedEventArgs e)
    {
        Log($"[Loaded] {Name}");
        ChildChanged(Child as FrameworkElement);
        UpdateManipulationMode();
        _engine.OnAttachedToVisualTree();
    }

    private void ZoomBorder_Unloaded(object sender, RoutedEventArgs e)
    {
        Log($"[Unloaded] {Name}");
        _engine.OnDetachedFromVisualTree();
        _zoomIndicatorTimer?.Stop();
        DetachElement();
    }

    private void ZoomBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateClip(e.NewSize);
        _engine.OnViewportSizeChanged(e.NewSize.ToCore());
    }

    private void Element_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _engine.OnElementBoundsChanged();
    }

    private void ChildChanged(FrameworkElement? element)
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

    private void AttachElement(FrameworkElement element)
    {
        _element = element;
        _element.SizeChanged += Element_SizeChanged;
        _lastElementRect = default;

        // Notify the engine (schedules auto fit and refreshes command state)
        _engine.OnElementAttached();
    }

    private void DetachElement()
    {
        if (_element == null)
        {
            return;
        }

        StopTransition();
        _element.SizeChanged -= Element_SizeChanged;

        if (_renderTransform != null && ReferenceEquals(_element.RenderTransform, _renderTransform))
        {
            _element.RenderTransform = null;
        }

        _renderTransform = null;
        _renderMatrix = CoreMatrix.Identity;
        _element = null;

        // Notify the engine (schedules auto fit and refreshes command state)
        _engine.OnElementDetached();
    }

    #endregion

    #region Render transform and animation

    /// <summary>
    /// Gets the render matrix currently applied to the child element (it differs from
    /// <see cref="Matrix"/> while an animation is running or when the content is rotated).
    /// </summary>
    public Matrix RenderMatrix => _renderMatrix.ToUno();

    private void ApplyElementTransform(CoreMatrix matrix, bool skipTransitions)
    {
        if (_element == null)
        {
            return;
        }

        if (_renderTransform == null || !ReferenceEquals(_element.RenderTransform, _renderTransform))
        {
            _renderTransform = new MatrixTransform { Matrix = _renderMatrix.ToUno() };
            _element.RenderTransformOrigin = new Point(0, 0);
            _element.RenderTransform = _renderTransform;
        }

        if (!skipTransitions && _engine.ShouldAnimate())
        {
            _transition.Start(_renderMatrix, matrix, AnimationDuration, _clock.Elapsed);
            if (_transition.IsRunning)
            {
                HookRendering();
                return;
            }
        }

        StopTransition();
        SetRenderMatrix(matrix);
    }

    private void SetRenderMatrix(CoreMatrix matrix)
    {
        _renderMatrix = matrix;
        if (_renderTransform != null)
        {
            _renderTransform.Matrix = matrix.ToUno();
        }
    }

    private void HookRendering()
    {
        if (_renderingHooked)
        {
            return;
        }

        _renderingHooked = true;
        CompositionTarget.Rendering += CompositionTarget_Rendering;
    }

    private void UnhookRendering()
    {
        if (!_renderingHooked)
        {
            return;
        }

        _renderingHooked = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
    }

    private void StopTransition()
    {
        if (_transition.IsRunning)
        {
            _transition.Stop();
        }

        UnhookRendering();
    }

    private void CompositionTarget_Rendering(object? sender, object e)
    {
        SetRenderMatrix(_transition.GetCurrentValue(_clock.Elapsed));

        if (!_transition.IsRunning)
        {
            UnhookRendering();
        }
    }

    #endregion

    #region Engine host callbacks

    private void UpdateViewProperties(double zoomX, double zoomY, double offsetX, double offsetY)
    {
        if (ZoomX != zoomX) ZoomX = zoomX;
        if (ZoomY != zoomY) ZoomY = zoomY;
        if (OffsetX != offsetX) OffsetX = offsetX;
        if (OffsetY != offsetY) OffsetY = offsetY;
    }

    private void OnIsPanningChanged(bool isPanning)
    {
        SetValue(IsPanningProperty, isPanning);
        VisualStateManager.GoToState(this, isPanning ? PanningStateName : NotPanningStateName, true);
    }

    private void StartZoomIndicatorTimer(TimeSpan duration)
    {
        if (_zoomIndicatorTimer == null)
        {
            var dispatcherQueue = DispatcherQueue ?? Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue == null)
            {
                return;
            }

            _zoomIndicatorTimer = dispatcherQueue.CreateTimer();
            _zoomIndicatorTimer.IsRepeating = false;
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

    #endregion

    #region Input

    private CorePoint GetElementPosition(PointerRoutedEventArgs e)
    {
        return _element != null ? e.GetCurrentPoint(_element).Position.ToCore() : default;
    }

    private ZoomBorderPointerButtons GetPointerButtons(PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        var buttons = ZoomBorderPointerButtons.None;
        if (properties.IsLeftButtonPressed) buttons |= ZoomBorderPointerButtons.Left;
        if (properties.IsRightButtonPressed) buttons |= ZoomBorderPointerButtons.Right;
        if (properties.IsMiddleButtonPressed) buttons |= ZoomBorderPointerButtons.Middle;
        return buttons;
    }

    private bool IsTouchHandledByManipulations(PointerRoutedEventArgs e)
    {
        return IsManipulationDevice(e.Pointer.PointerDeviceType) && EnableGestures;
    }

    // Touch and pen drive the gestures (like the Avalonia scroll gesture recognizer); the mouse uses the PanButton path.
    private static bool IsManipulationDevice(PointerDeviceType type)
    {
        return type == PointerDeviceType.Touch || type == PointerDeviceType.Pen;
    }

    private static ZoomBorderKeyModifiers GetCurrentKeyModifiers()
    {
        var modifiers = ZoomBorderKeyModifiers.None;
        if (IsKeyDown(VirtualKey.Control)) modifiers |= ZoomBorderKeyModifiers.Control;
        if (IsKeyDown(VirtualKey.Shift)) modifiers |= ZoomBorderKeyModifiers.Shift;
        if (IsKeyDown(VirtualKey.Menu)) modifiers |= ZoomBorderKeyModifiers.Alt;
        if (IsKeyDown(VirtualKey.LeftWindows) || IsKeyDown(VirtualKey.RightWindows)) modifiers |= ZoomBorderKeyModifiers.Meta;
        return modifiers;

        static bool IsKeyDown(VirtualKey key)
        {
            try
            {
                return (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (IsTabStop && FocusState == FocusState.Unfocused)
        {
            Focus(FocusState.Pointer);
        }

        if (IsTouchHandledByManipulations(e))
        {
            return;
        }

        if (_engine.ProcessPointerPressed(GetElementPosition(e), GetPointerButtons(e)))
        {
            if (CapturePointer(e.Pointer))
            {
                _capturedPointer = e.Pointer;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);

        if (IsTouchHandledByManipulations(e))
        {
            return;
        }

        _engine.ProcessPointerMoved(GetElementPosition(e), GetPointerButtons(e));
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (IsTouchHandledByManipulations(e))
        {
            return;
        }

        _engine.ProcessPointerReleased();
        ReleaseCapturedPointer();
    }

    /// <inheritdoc/>
    protected override void OnPointerCanceled(PointerRoutedEventArgs e)
    {
        base.OnPointerCanceled(e);
        _engine.ProcessPointerCaptureLost();
        ReleaseCapturedPointer();
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _capturedPointer = null;
        _engine.ProcessPointerCaptureLost();
        e.Handled = true;
    }

    private void ReleaseCapturedPointer()
    {
        var pointer = _capturedPointer;
        _capturedPointer = null;
        if (pointer != null)
        {
            ReleasePointerCapture(pointer);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var properties = e.GetCurrentPoint(this).Properties;
        var modifiers = e.KeyModifiers.ToCore() | GetCurrentKeyModifiers();

        if (HandlePointerWheel(properties.MouseWheelDelta, properties.IsHorizontalMouseWheel, GetElementPosition(e), modifiers))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Processes a mouse wheel change.
    /// </summary>
    /// <param name="mouseWheelDelta">The WinUI wheel delta (120 per notch, positive when rotated forward or tilted right).</param>
    /// <param name="isHorizontal">True for horizontal (tilt) wheel input.</param>
    /// <param name="point">The pointer position in child element coordinates.</param>
    /// <param name="modifiers">The keyboard modifiers.</param>
    /// <returns>True when the wheel input was handled.</returns>
    internal bool HandlePointerWheel(int mouseWheelDelta, bool isHorizontal, CorePoint point, ZoomBorderKeyModifiers modifiers)
    {
        var notches = mouseWheelDelta / 120.0;
        var delta = isHorizontal ? new CoreVector(-notches, 0) : new CoreVector(0, notches);
        return _engine.ProcessPointerWheel(delta, point, modifiers);
    }

    /// <inheritdoc/>
    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        base.OnDoubleTapped(e);

        if (_element == null)
        {
            return;
        }

        if (_engine.ProcessDoubleTapped(e.GetPosition(_element).ToCore()))
        {
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled)
        {
            return;
        }

        if (HandleKeyDown(e.Key, GetCurrentKeyModifiers()))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Processes a key down event.
    /// </summary>
    /// <param name="key">The pressed key.</param>
    /// <param name="modifiers">The keyboard modifiers.</param>
    /// <returns>True when the key was handled.</returns>
    internal bool HandleKeyDown(VirtualKey key, ZoomBorderKeyModifiers modifiers)
    {
        return _engine.ProcessKeyDown(key.ToCore(), modifiers);
    }

    /// <inheritdoc/>
    protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
    {
        base.OnManipulationStarted(e);

        if (!IsManipulationDevice(e.PointerDeviceType))
        {
            // The mouse is handled by the pointer events (PanButton), not by manipulations.
            e.Complete();
            return;
        }

        _manipulationActive = true;
        _manipulationPinch = false;
        _manipulationScroll = false;
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        base.OnManipulationDelta(e);

        if (!_manipulationActive || !IsManipulationDevice(e.PointerDeviceType))
        {
            return;
        }

        var delta = e.Delta;
        HandleManipulationDelta(e.Position, delta.Translation, delta.Scale, delta.Rotation, e.Cumulative.Scale);
        e.Handled = true;
    }

    /// <summary>
    /// Processes a touch manipulation delta.
    /// </summary>
    /// <param name="position">The manipulation origin in control coordinates.</param>
    /// <param name="translation">The incremental translation.</param>
    /// <param name="scale">The incremental scale.</param>
    /// <param name="rotation">The incremental rotation in degrees.</param>
    /// <param name="cumulativeScale">The cumulative scale since the manipulation started.</param>
    internal void HandleManipulationDelta(Point position, Point translation, double scale, double rotation, double cumulativeScale)
    {
        if (_element == null)
        {
            return;
        }

        if (Math.Abs(scale - 1.0) > 1e-9 || Math.Abs(rotation) > 1e-9)
        {
            var origin = position;
            try
            {
                origin = TransformToVisual(_element).TransformPoint(position);
            }
            catch (Exception)
            {
                // Fall back to the control coordinates when the transform is not available.
            }

            if (_engine.ProcessPinch(cumulativeScale, rotation, origin.ToCore()))
            {
                _manipulationPinch = true;
            }
        }

        if (translation.X != 0 || translation.Y != 0)
        {
            // Manipulation translation follows the finger (direct manipulation); the engine
            // expects scroll direction semantics so invert it.
            if (_engine.ProcessScrollGesture(new CoreVector(-translation.X, -translation.Y)))
            {
                _manipulationScroll = true;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
    {
        base.OnManipulationCompleted(e);

        if (!_manipulationActive)
        {
            return;
        }

        HandleManipulationCompleted();
        e.Handled = true;
    }

    /// <summary>
    /// Processes the end of a touch manipulation.
    /// </summary>
    internal void HandleManipulationCompleted()
    {
        _manipulationActive = false;

        if (_manipulationPinch)
        {
            _engine.ProcessPinchEnded();
        }

        if (_manipulationScroll)
        {
            _engine.ProcessScrollGestureEnded();
        }

        _manipulationPinch = false;
        _manipulationScroll = false;
    }

    /// <summary>
    /// Begins a touch manipulation (used by tests and custom gesture sources).
    /// </summary>
    internal void HandleManipulationStarted()
    {
        _manipulationActive = true;
        _manipulationPinch = false;
        _manipulationScroll = false;
    }

    #endregion

    #region Public API

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
    /// Centers the viewport on an element that is a descendant of the child element.
    /// </summary>
    /// <param name="element">The element to center on.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void CenterOn(FrameworkElement element, bool animate = true)
    {
        if (element == null)
            return;

        CenterOn(GetElementContentBounds(element), animate);
    }

    /// <summary>
    /// Gets the bounds of an element in content (child element) coordinates.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The element bounds in content coordinates.</returns>
    public Rect GetElementContentBounds(FrameworkElement element)
    {
        if (element == null)
        {
            return default;
        }

        if (_element == null || ReferenceEquals(element, _element))
        {
            var offset = ReferenceEquals(element, _element) ? GetElementLayoutOffset() : default;
            return new Rect(offset.X, offset.Y, element.ActualWidth, element.ActualHeight);
        }

        try
        {
            var transform = element.TransformToVisual(_element);
            return transform.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        }
        catch (Exception)
        {
            return new Rect(0, 0, element.ActualWidth, element.ActualHeight);
        }
    }

    /// <summary>
    /// Converts a viewport point to content coordinates.
    /// </summary>
    /// <param name="viewportPoint">The point in viewport coordinates.</param>
    /// <returns>The point in content coordinates.</returns>
    public Point ViewportToContent(Point viewportPoint) => _engine.ViewportToContent(viewportPoint.ToCore()).ToUno();

    /// <summary>
    /// Converts a content point to viewport coordinates.
    /// </summary>
    /// <param name="contentPoint">The point in content coordinates.</param>
    /// <returns>The point in viewport coordinates.</returns>
    public Point ContentToViewport(Point contentPoint) => _engine.ContentToViewport(contentPoint.ToCore()).ToUno();

    /// <summary>
    /// Converts a viewport rectangle to content coordinates.
    /// </summary>
    /// <param name="viewportRect">The rectangle in viewport coordinates.</param>
    /// <returns>The rectangle in content coordinates.</returns>
    public Rect ViewportToContent(Rect viewportRect) => _engine.ViewportToContent(viewportRect.ToCore()).ToUno();

    /// <summary>
    /// Converts a content rectangle to viewport coordinates.
    /// </summary>
    /// <param name="contentRect">The rectangle in content coordinates.</param>
    /// <returns>The rectangle in viewport coordinates.</returns>
    public Rect ContentToViewport(Rect contentRect) => _engine.ContentToViewport(contentRect.ToCore()).ToUno();

    /// <summary>
    /// Converts a screen vector (expressed as a <see cref="Point"/>, like WinUI manipulation translations) to a content vector.
    /// </summary>
    /// <param name="screenVector">The vector in screen coordinates.</param>
    /// <returns>The vector in content coordinates.</returns>
    public Point ScreenToContentVector(Point screenVector) => _engine.ScreenToContent(screenVector.ToCoreVector()).ToUno();

    /// <summary>
    /// Converts a content vector (expressed as a <see cref="Point"/>) to a screen vector.
    /// </summary>
    /// <param name="contentVector">The vector in content coordinates.</param>
    /// <returns>The vector in screen coordinates.</returns>
    public Point ContentToScreenVector(Point contentVector) => _engine.ContentToScreen(contentVector.ToCoreVector()).ToUno();

    /// <summary>
    /// Converts a screen size to content size.
    /// </summary>
    /// <param name="screenSize">The size in screen coordinates.</param>
    /// <returns>The size in content coordinates.</returns>
    public Size ScreenToContent(Size screenSize) => _engine.ScreenToContent(screenSize.ToCore()).ToUno();

    /// <summary>
    /// Converts a content size to screen size.
    /// </summary>
    /// <param name="contentSize">The size in content coordinates.</param>
    /// <returns>The size in screen coordinates.</returns>
    public Size ContentToScreen(Size contentSize) => _engine.ContentToScreen(contentSize.ToCore()).ToUno();

    /// <summary>
    /// Gets the transformation matrix from content to screen coordinates.
    /// </summary>
    /// <returns>The transformation matrix.</returns>
    public Matrix GetContentToScreenMatrix() => _engine.Matrix.ToUno();

    /// <summary>
    /// Gets the transformation matrix from screen to content coordinates.
    /// </summary>
    /// <returns>The transformation matrix (all zeros when the current matrix is not invertible).</returns>
    public Matrix GetScreenToContentMatrix() => _engine.GetScreenToContentMatrix().ToUno();

    /// <summary>
    /// Gets the visible content bounds in content coordinates.
    /// </summary>
    /// <returns>The visible content bounds.</returns>
    public Rect GetVisibleContentBounds() => _engine.GetVisibleContentBounds().ToUno();

    /// <summary>
    /// Gets the viewport bounds in viewport coordinates.
    /// </summary>
    /// <returns>The viewport bounds.</returns>
    public Rect GetViewportBounds() => _engine.GetViewportBounds().ToUno();

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
            Matrix = view.Matrix.ToUno(),
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
    public Rect GetVisiblePortion(Rect rect) => _engine.GetVisiblePortion(rect.ToCore()).ToUno();

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
        return PanAndZoomEngine.CalculateZoomIndicatorPosition(ZoomIndicatorPosition, GetViewportSize()).ToUno();
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
    public Point SnapToGrid(Point point) => _engine.SnapToGrid(point.ToCore()).ToUno();

    /// <summary>
    /// Snaps a rectangle to the nearest grid points.
    /// </summary>
    /// <param name="rect">The rectangle to snap.</param>
    /// <returns>The snapped rectangle.</returns>
    public Rect SnapToGrid(Rect rect) => _engine.SnapToGrid(rect.ToCore()).ToUno();

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
            Matrix = state.Matrix.ToUno(),
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
    /// Virtual method called when control is resized in Custom resize behavior mode.
    /// </summary>
    /// <param name="oldSize">The previous size.</param>
    /// <param name="newSize">The new size.</param>
    protected virtual void OnResized(Size oldSize, Size newSize)
    {
    }

    /// <summary>
    /// Virtual method to get custom content bounds.
    /// </summary>
    /// <returns>The content bounds rectangle.</returns>
    protected virtual Rect GetContentBounds()
    {
        return _engine.GetDefaultContentBounds().ToUno();
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

    #endregion

    #region Event raisers

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

    #endregion
}
