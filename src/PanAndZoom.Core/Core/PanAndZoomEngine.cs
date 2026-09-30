// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using static System.Math;

namespace PanAndZoom.Core;

/// <summary>
/// UI framework independent pan and zoom engine.
/// </summary>
/// <remarks>
/// <para>
/// The engine owns the pan and zoom state (transform matrix, view history, saved views, gesture state)
/// and implements all pan, zoom, rotation, constraint and input handling logic. UI framework specific
/// controls (for example the Avalonia and Uno Platform <c>ZoomBorder</c> controls) forward input and
/// layout notifications to the engine and apply the resulting transform through <see cref="IPanAndZoomHost"/>.
/// </para>
/// <para>
/// Points passed to the input processing methods must be expressed in the child element coordinate
/// space (the untransformed content space), matching <c>PointerEventArgs.GetPosition(child)</c>.
/// </para>
/// </remarks>
public sealed class PanAndZoomEngine
{
    private readonly IPanAndZoomHost _host;
    private readonly IPanAndZoomSettings _settings;

    private CoreMatrix _matrix = CoreMatrix.Identity;
    private CoreMatrix _lastValidMatrix = CoreMatrix.Identity;
    private CorePoint _pan;
    private CorePoint _previous;
    private bool _isPanning;
    private bool _captured;
    private bool _updating;
    private double _zoomX = 1.0;
    private double _zoomY = 1.0;
    private double _offsetX;
    private double _offsetY;
    private CoreSize _sizeBeforeResize;
    private bool _autoFitPending = true;
    private bool _suppressConstraints;

    // View history
    private readonly List<CoreViewState> _viewHistory = new();
    private int _viewHistoryIndex = -1;
    private bool _isNavigating;

    // Saved views
    private readonly Dictionary<string, CoreSavedView> _savedViews = new();

    // Multi-touch gesture tracking
    private DateTime _gestureStartTime;
    private bool _gestureRecognized;
    private bool _simultaneousGestureActive;
    private bool _pinchActive;
    private double _lastPinchScale = 1.0;

    // Zoom indicator
    private bool _zoomIndicatorVisible;

    // Logical scrolling
    private CoreSize _extent;
    private CoreSize _viewport;
    private CoreVector _scrollOffset;

    // Commands
    private ZoomBorderCommand? _zoomInCommand;
    private ZoomBorderCommand? _zoomOutCommand;
    private ZoomBorderCommand? _resetCommand;
    private ZoomBorderCommand? _fitCommand;
    private ZoomBorderCommand? _fillCommand;
    private ZoomBorderCommand? _uniformCommand;
    private ZoomBorderCommand? _uniformToFillCommand;
    private ZoomBorderCommand? _navigateBackCommand;
    private ZoomBorderCommand? _navigateForwardCommand;
    private ZoomBorderCommand? _toggleStretchCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="PanAndZoomEngine"/> class.
    /// </summary>
    /// <param name="host">The UI framework host.</param>
    /// <param name="settings">The settings provider.</param>
    public PanAndZoomEngine(IPanAndZoomHost host, IPanAndZoomSettings settings)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    [Conditional("DEBUG")]
    private static void Log(string message) => Debug.WriteLine(message);

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
    public event EventHandler<CorePanEventArgs>? PanStarted;

    /// <summary>
    /// Pan continued event.
    /// </summary>
    public event EventHandler<CorePanEventArgs>? PanContinued;

    /// <summary>
    /// Pan ended event.
    /// </summary>
    public event EventHandler<CorePanEventArgs>? PanEnded;

    /// <summary>
    /// Zoom started event.
    /// </summary>
    public event EventHandler<CoreZoomEventArgs>? ZoomStarted;

    /// <summary>
    /// Zoom ended event.
    /// </summary>
    public event EventHandler<CoreZoomEventArgs>? ZoomEnded;

    /// <summary>
    /// Zoom delta changed event.
    /// </summary>
    public event EventHandler<CoreZoomEventArgs>? ZoomDeltaChanged;

    /// <summary>
    /// Matrix changed event.
    /// </summary>
    public event EventHandler<CoreMatrixChangedEventArgs>? MatrixChanged;

    /// <summary>
    /// Matrix reset event.
    /// </summary>
    public event EventHandler<CoreMatrixChangedEventArgs>? MatrixReset;

    /// <summary>
    /// Stretch mode changed event.
    /// </summary>
    public event EventHandler<CoreStretchModeChangedEventArgs>? StretchModeChanged;

    /// <summary>
    /// Auto fit applied event.
    /// </summary>
    public event EventHandler<CoreStretchModeChangedEventArgs>? AutoFitApplied;

    /// <summary>
    /// Gesture started event.
    /// </summary>
    public event EventHandler<CoreGestureEventArgs>? GestureStarted;

    /// <summary>
    /// Gesture ended event.
    /// </summary>
    public event EventHandler<CoreGestureEventArgs>? GestureEnded;

    /// <summary>
    /// Raised when the logical scroll state (<see cref="Extent"/>, <see cref="Viewport"/>, <see cref="ScrollOffset"/>) changes.
    /// </summary>
    public event EventHandler? ScrollInvalidated;

    /// <summary>
    /// Gets the host.
    /// </summary>
    public IPanAndZoomHost Host => _host;

    /// <summary>
    /// Gets the settings provider.
    /// </summary>
    public IPanAndZoomSettings Settings => _settings;

    /// <summary>
    /// Gets the pan and zoom matrix.
    /// </summary>
    public CoreMatrix Matrix => _matrix;

    /// <summary>
    /// Gets the zoom ratio for x axis.
    /// </summary>
    public double ZoomX => _zoomX;

    /// <summary>
    /// Gets the zoom ratio for y axis.
    /// </summary>
    public double ZoomY => _zoomY;

    /// <summary>
    /// Gets the pan offset for x axis.
    /// </summary>
    public double OffsetX => _offsetX;

    /// <summary>
    /// Gets the pan offset for y axis.
    /// </summary>
    public double OffsetY => _offsetY;

    /// <summary>
    /// Gets a value indicating whether a pointer pan is in progress.
    /// </summary>
    public bool IsPanning => _isPanning;

    /// <summary>
    /// Gets a value indicating whether an operation is currently updating the matrix.
    /// </summary>
    public bool IsUpdating => _updating;

    /// <summary>
    /// Gets a value indicating whether the zoom indicator is currently visible.
    /// </summary>
    public bool IsZoomIndicatorVisible => _zoomIndicatorVisible;

    /// <summary>
    /// Gets a value indicating whether an automatic fit is pending for the next arrange pass.
    /// </summary>
    public bool IsAutoFitPending => _autoFitPending;

    /// <summary>
    /// Gets a value indicating whether it is possible to navigate back in view history.
    /// </summary>
    public bool CanNavigateBack => _settings.EnableViewHistory && _viewHistoryIndex > 0;

    /// <summary>
    /// Gets a value indicating whether it is possible to navigate forward in view history.
    /// </summary>
    public bool CanNavigateForward => _settings.EnableViewHistory && _viewHistoryIndex < _viewHistory.Count - 1;

    /// <summary>
    /// Gets the recorded view history.
    /// </summary>
    public IReadOnlyList<CoreViewState> ViewHistory => _viewHistory;

    /// <summary>
    /// Gets the current view history index.
    /// </summary>
    public int ViewHistoryIndex => _viewHistoryIndex;

    /// <summary>
    /// Gets the logical scroll extent.
    /// </summary>
    public CoreSize Extent => _extent;

    /// <summary>
    /// Gets the logical scroll viewport.
    /// </summary>
    public CoreSize Viewport => _viewport;

    /// <summary>
    /// Gets the logical scroll offset.
    /// </summary>
    public CoreVector ScrollOffset => _scrollOffset;

    /// <summary>
    /// Gets the command to zoom in.
    /// </summary>
    public ICommand ZoomInCommand => _zoomInCommand ??= new ZoomBorderCommand(() => ZoomIn(ShouldSkipTransitions()), () => _settings.EnableZoom && _host.HasElement);

    /// <summary>
    /// Gets the command to zoom out.
    /// </summary>
    public ICommand ZoomOutCommand => _zoomOutCommand ??= new ZoomBorderCommand(() => ZoomOut(ShouldSkipTransitions()), () => _settings.EnableZoom && _host.HasElement);

    /// <summary>
    /// Gets the command to reset the view.
    /// </summary>
    public ICommand ResetCommand => _resetCommand ??= new ZoomBorderCommand(() => ResetMatrix(ShouldSkipTransitions()));

    /// <summary>
    /// Gets the command to fit content to viewport.
    /// </summary>
    public ICommand FitCommand => _fitCommand ??= new ZoomBorderCommand(() => AutoFit(ShouldSkipTransitions()), () => _host.HasElement);

    /// <summary>
    /// Gets the command to fill viewport.
    /// </summary>
    public ICommand FillCommand => _fillCommand ??= new ZoomBorderCommand(() => Fill(ShouldSkipTransitions()), () => _host.HasElement);

    /// <summary>
    /// Gets the command to apply uniform stretch.
    /// </summary>
    public ICommand UniformCommand => _uniformCommand ??= new ZoomBorderCommand(() => Uniform(ShouldSkipTransitions()), () => _host.HasElement);

    /// <summary>
    /// Gets the command to apply uniform to fill stretch.
    /// </summary>
    public ICommand UniformToFillCommand => _uniformToFillCommand ??= new ZoomBorderCommand(() => UniformToFill(ShouldSkipTransitions()), () => _host.HasElement);

    /// <summary>
    /// Gets the command to navigate back in view history.
    /// </summary>
    public ICommand NavigateBackCommand => _navigateBackCommand ??= new ZoomBorderCommand(() => NavigateBack(ShouldAnimate()), () => CanNavigateBack);

    /// <summary>
    /// Gets the command to navigate forward in view history.
    /// </summary>
    public ICommand NavigateForwardCommand => _navigateForwardCommand ??= new ZoomBorderCommand(() => NavigateForward(ShouldAnimate()), () => CanNavigateForward);

    /// <summary>
    /// Gets the command to toggle stretch mode.
    /// </summary>
    public ICommand ToggleStretchCommand => _toggleStretchCommand ??= new ZoomBorderCommand(ToggleStretchMode);

    private CorePoint LayoutOffset => _host.HasElement ? _host.ElementLayoutOffset : default;

    #region Static helpers

    /// <summary>
    /// Clamps a value to the provided range.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="minimum">The minimum value.</param>
    /// <param name="maximum">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    /// <exception cref="ArgumentException">The minimum is greater than the maximum.</exception>
    public static double ClampValue(double value, double minimum, double maximum)
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
    /// <returns>The calculated matrix.</returns>
    public static CoreMatrix CalculateMatrix(double panelWidth, double panelHeight, double elementWidth, double elementHeight, StretchMode mode)
    {
        var zx = panelWidth / elementWidth;
        var zy = panelHeight / elementHeight;
        var cx = elementWidth / 2.0;
        var cy = elementHeight / 2.0;

        switch (mode)
        {
            default:
            case StretchMode.None:
                return CoreMatrix.Identity;
            case StretchMode.Fill:
                return MatrixMath.ScaleAt(zx, zy, cx, cy);
            case StretchMode.Uniform:
            {
                var zoom = Min(zx, zy);
                return MatrixMath.ScaleAt(zoom, zoom, cx, cy);
            }
            case StretchMode.UniformToFill:
            {
                var zoom = Max(zx, zy);
                return MatrixMath.ScaleAt(zoom, zoom, cx, cy);
            }
        }
    }

    /// <summary>
    /// Calculate scrollable properties for content-coordinate bounds and a separate layout offset.
    /// </summary>
    /// <param name="contentBounds">The effective bounds in content coordinates.</param>
    /// <param name="layoutOffset">The layout position of the child inside the control.</param>
    /// <param name="borderSize">The size of the viewport.</param>
    /// <param name="matrix">The transform matrix.</param>
    /// <param name="extent">The extent of the scrollable content.</param>
    /// <param name="viewport">The size of the viewport.</param>
    /// <param name="offset">The current scroll offset.</param>
    public static void CalculateScrollable(
        CoreRect contentBounds,
        CorePoint layoutOffset,
        CoreSize borderSize,
        CoreMatrix matrix,
        out CoreSize extent,
        out CoreSize viewport,
        out CoreVector offset)
    {
        viewport = borderSize;

        var transformed = TransformContentToViewport(contentBounds, layoutOffset, matrix);

        Log($"[CalculateScrollable] contentBounds: {contentBounds}, layoutOffset: {layoutOffset}, transformed: {transformed}");

        var width = transformed.Width;
        var height = transformed.Height;

        if (width < viewport.Width)
        {
            width = viewport.Width;

            if (transformed.X < 0.0)
            {
                width += Abs(transformed.X);
            }
            else
            {
                var widthTranslated = transformed.Width + transformed.X;
                if (widthTranslated > width)
                {
                    width += widthTranslated - width;
                }
            }
        }
        else if (!(width > viewport.Width))
        {
            width += Abs(transformed.X);
        }

        if (height < viewport.Height)
        {
            height = viewport.Height;

            if (transformed.Y < 0.0)
            {
                height += Abs(transformed.Y);
            }
            else
            {
                var heightTranslated = transformed.Height + transformed.Y;
                if (heightTranslated > height)
                {
                    height += heightTranslated - height;
                }
            }
        }
        else if (!(height > viewport.Height))
        {
            height += Abs(transformed.Y);
        }

        extent = new CoreSize(width, height);

        var ox = transformed.X;
        var oy = transformed.Y;

        var offsetX = ox < 0 ? Abs(ox) : 0;
        var offsetY = oy < 0 ? Abs(oy) : 0;

        offset = new CoreVector(offsetX, offsetY);

        Log($"[CalculateScrollable] Extent: {extent} | Offset: {offset} | Viewport: {viewport}");
    }

    /// <summary>
    /// Transforms a rectangle in content coordinates to viewport coordinates, accounting for layout offset.
    /// </summary>
    /// <param name="contentRect">A rectangle in content coordinates (where 0,0 is the top-left of the content).</param>
    /// <param name="layoutOffset">The layout offset where the UI framework positioned the element within its parent.</param>
    /// <param name="matrix">The transform matrix.</param>
    /// <returns>The rectangle in viewport coordinates.</returns>
    public static CoreRect TransformContentToViewport(CoreRect contentRect, CorePoint layoutOffset, CoreMatrix matrix)
    {
        var transformedContent = contentRect.TransformToAABB(matrix);

        return new CoreRect(
            transformedContent.X + layoutOffset.X,
            transformedContent.Y + layoutOffset.Y,
            transformedContent.Width,
            transformedContent.Height);
    }

    /// <summary>
    /// Calculates the zoom indicator position inside a viewport.
    /// </summary>
    /// <param name="position">The indicator position.</param>
    /// <param name="viewportSize">The viewport size.</param>
    /// <returns>The top left position of the indicator.</returns>
    public static CorePoint CalculateZoomIndicatorPosition(ZoomIndicatorPosition position, CoreSize viewportSize)
    {
        const double margin = 10.0;
        const double indicatorWidth = 80.0;
        const double indicatorHeight = 30.0;

        return position switch
        {
            ZoomIndicatorPosition.TopLeft => new CorePoint(margin, margin),
            ZoomIndicatorPosition.TopRight => new CorePoint(viewportSize.Width - indicatorWidth - margin, margin),
            ZoomIndicatorPosition.BottomLeft => new CorePoint(margin, viewportSize.Height - indicatorHeight - margin),
            ZoomIndicatorPosition.BottomRight => new CorePoint(viewportSize.Width - indicatorWidth - margin, viewportSize.Height - indicatorHeight - margin),
            _ => new CorePoint(viewportSize.Width - indicatorWidth - margin, viewportSize.Height - indicatorHeight - margin)
        };
    }

    #endregion

    #region Lifecycle and layout notifications

    /// <summary>
    /// Notifies the engine that the control was attached to the visual tree.
    /// </summary>
    /// <remarks>
    /// Call after the child element has been attached (see <see cref="OnElementAttached"/>).
    /// </remarks>
    public void OnAttachedToVisualTree()
    {
        _updating = true;
        Invalidate(skipTransitions: false);
        _updating = false;

        // Add initial state to history
        AddToViewHistory();
    }

    /// <summary>
    /// Notifies the engine that the control was detached from the visual tree.
    /// </summary>
    public void OnDetachedFromVisualTree()
    {
        ResetGestureState();
    }

    /// <summary>
    /// Notifies the engine that a child element was attached (after <see cref="IPanAndZoomHost.HasElement"/> became true).
    /// </summary>
    public void OnElementAttached()
    {
        _autoFitPending = true;
        RaiseCommandsCanExecuteChanged();
    }

    /// <summary>
    /// Notifies the engine that the child element was detached (after <see cref="IPanAndZoomHost.HasElement"/> became false).
    /// </summary>
    public void OnElementDetached()
    {
        _autoFitPending = true;
        RaiseCommandsCanExecuteChanged();
    }

    /// <summary>
    /// Notifies the engine that the control was arranged. Applies the pending automatic fit.
    /// </summary>
    /// <param name="finalSize">The arranged size of the control.</param>
    /// <remarks>
    /// Call only when the child element exists and has a valid measure.
    /// </remarks>
    public void OnArranged(CoreSize finalSize)
    {
        if (!_host.HasElement)
        {
            return;
        }

        if (_autoFitPending)
        {
            var elementSize = _host.ElementSize;
            AutoFit(finalSize.Width, finalSize.Height, elementSize.Width, elementSize.Height);
            _autoFitPending = false;
        }
    }

    /// <summary>
    /// Notifies the engine that the viewport (control) size changed.
    /// </summary>
    /// <param name="newSize">The new viewport size.</param>
    public void OnViewportSizeChanged(CoreSize newSize)
    {
        InvalidateScrollable();
        HandleResizeBehavior(newSize);
    }

    /// <summary>
    /// Notifies the engine that the child element bounds changed.
    /// </summary>
    public void OnElementBoundsChanged()
    {
        InvalidateScrollable();
    }

    /// <summary>
    /// Notifies the engine that the <see cref="IPanAndZoomSettings.Stretch"/> setting changed.
    /// </summary>
    public void OnStretchChanged()
    {
        _autoFitPending = true;
    }

    /// <summary>
    /// Notifies the engine that one of the content bounds settings changed
    /// (<see cref="IPanAndZoomSettings.BoundsMode"/>, <see cref="IPanAndZoomSettings.BoundsPadding"/>
    /// or <see cref="IPanAndZoomSettings.MinimumVisibleContentPercentage"/>).
    /// </summary>
    public void OnBoundsSettingsChanged()
    {
        Invalidate(skipTransitions: true);
    }

    /// <summary>
    /// Resets the multi-touch gesture tracking state.
    /// </summary>
    public void ResetGestureState()
    {
        _gestureRecognized = false;
        _gestureStartTime = default;
        _simultaneousGestureActive = false;
        _pinchActive = false;
        _lastPinchScale = 1.0;
    }

    /// <summary>
    /// Reapplies constraints and refreshes the transform, properties and logical scroll state.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Refresh(bool skipTransitions = true)
    {
        Invalidate(skipTransitions);
    }

    /// <summary>
    /// Hides the zoom indicator. Called by the host when the auto-hide timer elapses.
    /// </summary>
    public void HideZoomIndicator()
    {
        if (!_zoomIndicatorVisible)
        {
            return;
        }

        _zoomIndicatorVisible = false;
        _host.OnZoomIndicatorVisibilityChanged(false);
    }

    /// <summary>
    /// Gets a value indicating whether operations should be animated.
    /// </summary>
    /// <returns>True when animations are enabled and the duration is positive.</returns>
    public bool ShouldAnimate()
    {
        return _settings.EnableAnimations && _settings.AnimationDuration > TimeSpan.Zero;
    }

    private bool ShouldSkipTransitions()
    {
        return !ShouldAnimate();
    }

    #endregion

    #region Input processing

    /// <summary>
    /// Processes a pointer pressed event.
    /// </summary>
    /// <param name="point">The pointer position in child element coordinates.</param>
    /// <param name="buttons">The currently pressed pointer buttons.</param>
    /// <returns>True when pointer panning started (the host should capture the pointer).</returns>
    public bool ProcessPointerPressed(CorePoint point, ZoomBorderPointerButtons buttons)
    {
        if (!_settings.EnablePan)
        {
            return false;
        }

        if (_host.HasElement && !_captured && !_isPanning && IsPanButtonPressed(buttons))
        {
            BeginPanTo(point.X, point.Y);
            _captured = true;
            _isPanning = true;
            _host.OnIsPanningChanged(_isPanning);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Processes a pointer moved event.
    /// </summary>
    /// <param name="point">The pointer position in child element coordinates.</param>
    /// <param name="buttons">The currently pressed pointer buttons.</param>
    /// <returns>True when the pointer move continued a pan.</returns>
    public bool ProcessPointerMoved(CorePoint point, ZoomBorderPointerButtons buttons)
    {
        if (!_settings.EnablePan)
        {
            return false;
        }

        if (!_host.HasElement || _captured != true || _isPanning != true || !IsPanButtonPressed(buttons))
        {
            return false;
        }

        ContinuePanTo(point.X, point.Y, true);
        return true;
    }

    /// <summary>
    /// Processes a pointer released event.
    /// </summary>
    /// <returns>True when a pan was finished.</returns>
    public bool ProcessPointerReleased() => PanningFinished();

    /// <summary>
    /// Processes a pointer capture lost event.
    /// </summary>
    /// <returns>True when a pan was finished.</returns>
    public bool ProcessPointerCaptureLost() => PanningFinished();

    private bool PanningFinished()
    {
        if (!_settings.EnablePan)
        {
            return false;
        }

        if (!_host.HasElement || _captured != true || _isPanning != true)
        {
            return false;
        }

        // Raise PanEnded event
        PanEnded?.Invoke(this, new CorePanEventArgs(
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            _offsetX,
            _offsetY,
            0,
            0,
            _matrix,
            _matrix));

        // Add to view history after pan gesture completes
        AddToViewHistory();

        _captured = false;
        _isPanning = false;
        _host.OnIsPanningChanged(_isPanning);
        return true;
    }

    /// <summary>
    /// Determines whether the configured pan button is pressed.
    /// </summary>
    /// <param name="buttons">The currently pressed pointer buttons.</param>
    /// <returns>True when the pan button is pressed.</returns>
    public bool IsPanButtonPressed(ZoomBorderPointerButtons buttons)
    {
        var button = _settings.PanButton;
        return ((buttons & ZoomBorderPointerButtons.Left) != 0 && button == ButtonName.Left)
            || ((buttons & ZoomBorderPointerButtons.Right) != 0 && button == ButtonName.Right)
            || ((buttons & ZoomBorderPointerButtons.Middle) != 0 && button == ButtonName.Middle);
    }

    /// <summary>
    /// Gets the wheel behavior for the provided keyboard modifiers.
    /// </summary>
    /// <param name="modifiers">The keyboard modifiers.</param>
    /// <returns>The wheel behavior.</returns>
    public WheelBehaviorMode GetWheelBehavior(ZoomBorderKeyModifiers modifiers)
    {
        if ((modifiers & ZoomBorderKeyModifiers.Control) != 0)
        {
            return _settings.WheelWithCtrl;
        }

        if ((modifiers & ZoomBorderKeyModifiers.Shift) != 0)
        {
            return _settings.WheelWithShift;
        }

        return _settings.WheelBehavior;
    }

    /// <summary>
    /// Processes a pointer wheel event.
    /// </summary>
    /// <param name="delta">The wheel delta where one notch equals 1.0.</param>
    /// <param name="point">The pointer position in child element coordinates.</param>
    /// <param name="modifiers">The keyboard modifiers.</param>
    /// <returns>True when the event was handled.</returns>
    public bool ProcessPointerWheel(CoreVector delta, CorePoint point, ZoomBorderKeyModifiers modifiers)
    {
        var behavior = GetWheelBehavior(modifiers);
        var panSensitivity = _settings.WheelPanSensitivity;

        switch (behavior)
        {
            case WheelBehaviorMode.Zoom:
                if (_settings.EnableZoom)
                {
                    Wheel(delta, point, _settings.WheelZoomSensitivity);
                    return true;
                }

                if (_settings.EnablePan && modifiers == ZoomBorderKeyModifiers.None)
                {
                    // Backward compatibility: If zoom is disabled but pan is enabled,
                    // fall back to panning with the wheel (old behavior)
                    PanDelta(10 * delta.X * panSensitivity, 10 * delta.Y * panSensitivity);
                    return true;
                }

                break;

            case WheelBehaviorMode.PanVertical:
                if (_settings.EnablePan)
                {
                    PanDelta(10 * delta.X * panSensitivity, 10 * delta.Y * panSensitivity);
                    return true;
                }

                break;

            case WheelBehaviorMode.PanHorizontal:
                if (_settings.EnablePan)
                {
                    PanDelta(10 * delta.Y * panSensitivity, 10 * delta.X * panSensitivity);
                    return true;
                }

                break;

            case WheelBehaviorMode.None:
                break;
        }

        return false;
    }

    private void Wheel(CoreVector delta, CorePoint point, double sensitivity)
    {
        if (!_host.HasElement || _captured)
        {
            return;
        }

        ZoomDeltaTo(delta.Y * sensitivity, point.X, point.Y);
    }

    /// <summary>
    /// Processes a double tap (double click) event.
    /// </summary>
    /// <param name="point">The tap position in child element coordinates.</param>
    /// <returns>True when the event was handled.</returns>
    public bool ProcessDoubleTapped(CorePoint point)
    {
        if (!_settings.EnableDoubleClickZoom || !_host.HasElement)
            return false;

        var factor = _settings.DoubleClickZoomFactor;

        switch (_settings.DoubleClickZoomMode)
        {
            case DoubleClickZoomMode.ZoomIn:
                ZoomTo(factor, point.X, point.Y, ShouldSkipTransitions());
                break;

            case DoubleClickZoomMode.ZoomOut:
                ZoomTo(1.0 / factor, point.X, point.Y, ShouldSkipTransitions());
                break;

            case DoubleClickZoomMode.ZoomInOut:
                // Toggle between zoom in and zoom out based on current zoom level
                if (_zoomX >= ZoomBorderDefaults.DoubleClickZoomThreshold)
                {
                    // Zoom out or reset
                    ResetMatrix(ShouldSkipTransitions());
                }
                else
                {
                    // Zoom in
                    ZoomTo(factor, point.X, point.Y, ShouldSkipTransitions());
                }

                break;

            case DoubleClickZoomMode.ZoomToFit:
                AutoFit(ShouldSkipTransitions());
                break;

            case DoubleClickZoomMode.None:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Processes a key down event.
    /// </summary>
    /// <param name="key">The pressed key.</param>
    /// <param name="modifiers">The keyboard modifiers.</param>
    /// <returns>True when the key was handled.</returns>
    public bool ProcessKeyDown(ZoomBorderKey key, ZoomBorderKeyModifiers modifiers)
    {
        if (!_settings.EnableKeyboardNavigation || !_host.HasElement)
            return false;

        var handled = true;
        var control = (modifiers & ZoomBorderKeyModifiers.Control) != 0;
        var panStep = _settings.KeyboardPanStep;

        switch (key)
        {
            case ZoomBorderKey.Left:
                if (control)
                {
                    // Ctrl+Left: Navigate back
                    if (_settings.EnableViewHistory && CanNavigateBack)
                    {
                        NavigateBack(ShouldAnimate());
                    }
                }
                else
                {
                    // Left arrow: Pan left
                    PanDelta(-panStep, 0, ShouldSkipTransitions());
                }

                break;

            case ZoomBorderKey.Right:
                if (control)
                {
                    // Ctrl+Right: Navigate forward
                    if (_settings.EnableViewHistory && CanNavigateForward)
                    {
                        NavigateForward(ShouldAnimate());
                    }
                }
                else
                {
                    // Right arrow: Pan right
                    PanDelta(panStep, 0, ShouldSkipTransitions());
                }

                break;

            case ZoomBorderKey.Up:
                // Up arrow: Pan up
                PanDelta(0, -panStep, ShouldSkipTransitions());
                break;

            case ZoomBorderKey.Down:
                // Down arrow: Pan down
                PanDelta(0, panStep, ShouldSkipTransitions());
                break;

            case ZoomBorderKey.Add:
            {
                // +/=: Zoom in
                var elementSize = _host.ElementSize;
                ZoomTo(_settings.KeyboardZoomStep, elementSize.Width / 2.0, elementSize.Height / 2.0, ShouldSkipTransitions());
                break;
            }

            case ZoomBorderKey.Subtract:
            {
                // -: Zoom out
                var elementSize = _host.ElementSize;
                ZoomTo(1.0 / _settings.KeyboardZoomStep, elementSize.Width / 2.0, elementSize.Height / 2.0, ShouldSkipTransitions());
                break;
            }

            case ZoomBorderKey.D0:
                if (control)
                {
                    // Ctrl+0: Reset to 100% zoom (1:1)
                    ResetMatrix(ShouldSkipTransitions());
                }
                else
                {
                    handled = false;
                }

                break;

            case ZoomBorderKey.Home:
                // Home: Fit to viewport
                AutoFit(ShouldSkipTransitions());
                break;

            default:
                handled = false;
                break;
        }

        return handled;
    }

    /// <summary>
    /// Processes a touch pad magnify gesture.
    /// </summary>
    /// <param name="delta">The magnify delta.</param>
    /// <param name="point">The gesture position in child element coordinates.</param>
    public void ProcessMagnify(double delta, CorePoint point)
    {
        Log($"[Magnified] {delta}");
        ZoomDeltaTo(delta, point.X, point.Y);
    }

    /// <summary>
    /// Processes a pinch gesture update.
    /// </summary>
    /// <param name="scale">The cumulative scale since the gesture started.</param>
    /// <param name="angleDelta">The incremental rotation angle in degrees (positive is clockwise).</param>
    /// <param name="origin">The pinch origin in child element coordinates.</param>
    /// <returns>True when the gesture was handled.</returns>
    public bool ProcessPinch(double scale, double angleDelta, CorePoint origin)
    {
        if (!_settings.EnableGestures || !_host.HasElement)
            return false;

        // Need either zoom or rotation enabled for pinch gesture to do anything
        if (!_settings.EnableGestureZoom && !_settings.EnableGestureRotation)
            return false;

        // Check if we're within touch point limits (pinch requires 2 points)
        if (_settings.MinimumTouchPoints > 2 || _settings.MaximumTouchPoints < 2)
        {
            return false;
        }

        // Check gesture recognition delay
        if (!_gestureRecognized)
        {
            if (_gestureStartTime == default)
            {
                _gestureStartTime = DateTime.Now;
            }

            if ((DateTime.Now - _gestureStartTime) < _settings.GestureRecognitionDelay)
            {
                return false;
            }

            _gestureRecognized = true;
        }

        // Check if simultaneous pan/zoom is allowed
        if (!_settings.EnableSimultaneousPanZoom && _isPanning)
        {
            return false;
        }

        Log($"[PinchGesture] Scale: {scale}, AngleDelta: {angleDelta}");

        var zoomCenter = origin;

        // Mark simultaneous gesture as active
        _simultaneousGestureActive = _settings.EnableSimultaneousPanZoom && _isPanning;

        // Raise GestureStarted event
        var previousMatrix = _matrix;
        GestureStarted?.Invoke(this, new CoreGestureEventArgs(
            "Pinch",
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            zoomCenter.X,
            zoomCenter.Y,
            scale - 1.0,
            _matrix,
            previousMatrix));

        // Apply zoom if enabled
        if (_settings.EnableGestureZoom)
        {
            if (!_pinchActive)
            {
                _pinchActive = true;
                _lastPinchScale = 1.0;
            }

            // Pinch scale is cumulative since gesture start, so convert it to an incremental factor.
            var deltaScale = scale / _lastPinchScale;
            _lastPinchScale = scale;

            if (!double.IsNaN(deltaScale) && !double.IsInfinity(deltaScale) && deltaScale > 0.0)
            {
                Log($"[ZoomTo] factor: {deltaScale}, center: {zoomCenter.X}, {zoomCenter.Y}");
                ZoomTo(deltaScale, zoomCenter.X, zoomCenter.Y);
            }
        }

        // Apply rotation if enabled
        if (_settings.EnableGestureRotation && Abs(angleDelta) > 0.001)
        {
            // Use RotateAt to rotate around the pinch center point
            RotateAt(angleDelta, zoomCenter, animate: false);
        }

        return true;
    }

    /// <summary>
    /// Processes the end of a pinch gesture.
    /// </summary>
    /// <returns>True when the gesture end was handled.</returns>
    public bool ProcessPinchEnded()
    {
        ResetGestureState();

        if (!_settings.EnableGestures)
            return false;

        Log("[PinchGestureEnded]");

        GestureEnded?.Invoke(this, new CoreGestureEventArgs(
            "Pinch",
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            0,
            0,
            0,
            _matrix,
            _matrix));

        // Add to view history after pinch gesture completes
        AddToViewHistory();

        return true;
    }

    /// <summary>
    /// Processes a scroll (touch translation) gesture update.
    /// </summary>
    /// <param name="delta">The scroll delta in scroll direction semantics (positive values scroll down/right).</param>
    /// <returns>True when the gesture was handled.</returns>
    public bool ProcessScrollGesture(CoreVector delta)
    {
        if (!_settings.EnableGestureTranslation || !_host.HasElement)
            return false;

        // Check touch point limits (scroll gesture typically uses 2 fingers)
        if (_settings.MinimumTouchPoints > 2 || _settings.MaximumTouchPoints < 2)
        {
            return false;
        }

        // Check if simultaneous pan/zoom is allowed when another gesture is active
        if (!_settings.EnableSimultaneousPanZoom && _simultaneousGestureActive)
        {
            return false;
        }

        Log($"[ScrollGesture] Delta: {delta}");

        var previousMatrix = _matrix;
        GestureStarted?.Invoke(this, new CoreGestureEventArgs(
            "Scroll",
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            0,
            0,
            Sqrt(delta.X * delta.X + delta.Y * delta.Y),
            _matrix,
            previousMatrix));

        // Scroll gesture delta follows scroll direction semantics (positive = scroll down/right),
        // which is opposite to direct manipulation (content following finger). Invert
        // it so the content moves with the finger on touch/gesture devices.
        PanDelta(-delta.X, -delta.Y);

        return true;
    }

    /// <summary>
    /// Processes the end of a scroll gesture.
    /// </summary>
    /// <returns>Always true.</returns>
    public bool ProcessScrollGestureEnded()
    {
        Log("[ScrollGestureEnded]");

        GestureEnded?.Invoke(this, new CoreGestureEventArgs(
            "Scroll",
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            0,
            0,
            0,
            _matrix,
            _matrix));

        // Add to view history after scroll gesture completes
        AddToViewHistory();

        return true;
    }

    #endregion

    #region Resize behavior

    private void HandleResizeBehavior(CoreSize newSize)
    {
        if (!_host.HasElement || _sizeBeforeResize == default || _sizeBeforeResize.Width == 0 || _sizeBeforeResize.Height == 0)
        {
            _sizeBeforeResize = newSize;
            return;
        }

        var oldSize = _sizeBeforeResize;

        if (oldSize == newSize)
        {
            return;
        }

        switch (_settings.ResizeBehavior)
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
                _host.OnResized(oldSize, newSize);
                break;
        }

        _sizeBeforeResize = newSize;
    }

    private void MaintainCenterOnResize(CoreSize oldSize, CoreSize newSize)
    {
        if (!_host.HasElement)
            return;

        var oldCenterX = (oldSize.Width / 2.0 - _offsetX) / _zoomX;
        var oldCenterY = (oldSize.Height / 2.0 - _offsetY) / _zoomY;

        var newOffsetX = newSize.Width / 2.0 - oldCenterX * _zoomX;
        var newOffsetY = newSize.Height / 2.0 - oldCenterY * _zoomY;

        _matrix = MatrixMath.ScaleAndTranslate(_zoomX, _zoomY, newOffsetX, newOffsetY);
        Invalidate(skipTransitions: true);
    }

    private void MaintainZoomOnResize(CoreSize oldSize, CoreSize newSize)
    {
        if (!_host.HasElement)
            return;

        var scaleX = newSize.Width / oldSize.Width;
        var scaleY = newSize.Height / oldSize.Height;

        var newOffsetX = _offsetX * scaleX;
        var newOffsetY = _offsetY * scaleY;

        _matrix = MatrixMath.ScaleAndTranslate(_zoomX, _zoomY, newOffsetX, newOffsetY);
        Invalidate(skipTransitions: true);
    }

    #endregion

    #region View history

    /// <summary>
    /// Adds the current view state to history.
    /// </summary>
    public void AddToViewHistory()
    {
        if (!_settings.EnableViewHistory || _isNavigating)
            return;

        var viewState = new CoreViewState(_matrix, _settings.Stretch, DateTime.UtcNow);

        // Remove any forward history when adding a new state
        if (_viewHistoryIndex < _viewHistory.Count - 1)
        {
            _viewHistory.RemoveRange(_viewHistoryIndex + 1, _viewHistory.Count - _viewHistoryIndex - 1);
        }

        // Add new state
        _viewHistory.Add(viewState);
        _viewHistoryIndex = _viewHistory.Count - 1;

        // Maintain size limit
        if (_viewHistory.Count > _settings.ViewHistorySize)
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
        _settings.Stretch = state.Stretch;
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
        _settings.Stretch = state.Stretch;
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

    #endregion

    #region Centering and coordinate conversion

    /// <summary>
    /// Centers the viewport on a specific point in content coordinates.
    /// </summary>
    /// <param name="point">The point to center on.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void CenterOn(CorePoint point, bool animate = true)
    {
        if (!_host.HasElement)
            return;

        var layoutOffset = LayoutOffset;
        var viewportSize = _host.ViewportSize;
        var centerPadding = _settings.CenterPadding;

        var viewportCenterX = (viewportSize.Width - centerPadding.Left - centerPadding.Right) / 2.0 + centerPadding.Left;
        var viewportCenterY = (viewportSize.Height - centerPadding.Top - centerPadding.Bottom) / 2.0 + centerPadding.Top;

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
    public void CenterOn(CorePoint point, double zoom, bool animate = true)
    {
        if (!_host.HasElement)
            return;

        var layoutOffset = LayoutOffset;
        var viewportSize = _host.ViewportSize;
        var centerPadding = _settings.CenterPadding;

        var viewportCenterX = (viewportSize.Width - centerPadding.Left - centerPadding.Right) / 2.0 + centerPadding.Left;
        var viewportCenterY = (viewportSize.Height - centerPadding.Top - centerPadding.Bottom) / 2.0 + centerPadding.Top;

        // The actual visual position is: layoutOffset + matrix offset + point * zoom
        // So: matrix offset = viewportCenter - layoutOffset - point * zoom
        var offsetX = viewportCenterX - layoutOffset.X - point.X * zoom;
        var offsetY = viewportCenterY - layoutOffset.Y - point.Y * zoom;

        _matrix = MatrixMath.ScaleAndTranslate(zoom, zoom, offsetX, offsetY);
        Invalidate(!animate);
    }

    /// <summary>
    /// Centers the viewport on a rectangle in content coordinates.
    /// </summary>
    /// <param name="rect">The rectangle to center on.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void CenterOn(CoreRect rect, bool animate = true)
    {
        if (!_host.HasElement)
            return;

        var viewportSize = _host.ViewportSize;
        var centerPadding = _settings.CenterPadding;

        var viewportWidth = viewportSize.Width - centerPadding.Left - centerPadding.Right;
        var viewportHeight = viewportSize.Height - centerPadding.Top - centerPadding.Bottom;

        var zoomX = viewportWidth / rect.Width;
        var zoomY = viewportHeight / rect.Height;
        var zoom = Min(zoomX, zoomY);

        var centerX = rect.X + rect.Width / 2.0;
        var centerY = rect.Y + rect.Height / 2.0;

        CenterOn(new CorePoint(centerX, centerY), zoom, animate);
    }

    /// <summary>
    /// Converts a viewport point to content coordinates.
    /// </summary>
    /// <param name="viewportPoint">The point in viewport coordinates.</param>
    /// <returns>The point in content coordinates.</returns>
    public CorePoint ViewportToContent(CorePoint viewportPoint)
    {
        if (!_matrix.TryInvert(out var inverted))
            return viewportPoint;

        var layoutOffset = LayoutOffset;
        var adjustedPoint = new CorePoint(viewportPoint.X - layoutOffset.X, viewportPoint.Y - layoutOffset.Y);

        return inverted.Transform(adjustedPoint);
    }

    /// <summary>
    /// Converts a content point to viewport coordinates.
    /// </summary>
    /// <param name="contentPoint">The point in content coordinates.</param>
    /// <returns>The point in viewport coordinates.</returns>
    public CorePoint ContentToViewport(CorePoint contentPoint)
    {
        var transformed = _matrix.Transform(contentPoint);

        var layoutOffset = LayoutOffset;
        return new CorePoint(transformed.X + layoutOffset.X, transformed.Y + layoutOffset.Y);
    }

    /// <summary>
    /// Converts a viewport rectangle to content coordinates.
    /// </summary>
    /// <param name="viewportRect">The rectangle in viewport coordinates.</param>
    /// <returns>The rectangle in content coordinates.</returns>
    public CoreRect ViewportToContent(CoreRect viewportRect)
    {
        if (!_matrix.TryInvert(out var inverted))
            return viewportRect;

        var layoutOffset = LayoutOffset;
        var adjustedRect = new CoreRect(
            viewportRect.X - layoutOffset.X,
            viewportRect.Y - layoutOffset.Y,
            viewportRect.Width,
            viewportRect.Height);

        var topLeft = inverted.Transform(adjustedRect.TopLeft);
        var bottomRight = inverted.Transform(adjustedRect.BottomRight);

        return new CoreRect(topLeft, bottomRight);
    }

    /// <summary>
    /// Converts a content rectangle to viewport coordinates.
    /// </summary>
    /// <param name="contentRect">The rectangle in content coordinates.</param>
    /// <returns>The rectangle in viewport coordinates.</returns>
    public CoreRect ContentToViewport(CoreRect contentRect)
    {
        var topLeft = _matrix.Transform(contentRect.TopLeft);
        var bottomRight = _matrix.Transform(contentRect.BottomRight);

        var layoutOffset = LayoutOffset;
        return new CoreRect(
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
    public CoreVector ScreenToContent(CoreVector screenVector)
    {
        if (!_matrix.TryInvert(out var inverted))
            return screenVector;

        var origin = inverted.Transform(new CorePoint(0, 0));
        var transformed = inverted.Transform(new CorePoint(screenVector.X, screenVector.Y));

        return new CoreVector(transformed.X - origin.X, transformed.Y - origin.Y);
    }

    /// <summary>
    /// Converts a content vector to screen vector.
    /// </summary>
    /// <param name="contentVector">The vector in content coordinates.</param>
    /// <returns>The vector in screen coordinates.</returns>
    public CoreVector ContentToScreen(CoreVector contentVector)
    {
        var origin = _matrix.Transform(new CorePoint(0, 0));
        var transformed = _matrix.Transform(new CorePoint(contentVector.X, contentVector.Y));

        return new CoreVector(transformed.X - origin.X, transformed.Y - origin.Y);
    }

    /// <summary>
    /// Converts a screen size to content size.
    /// </summary>
    /// <param name="screenSize">The size in screen coordinates.</param>
    /// <returns>The size in content coordinates.</returns>
    public CoreSize ScreenToContent(CoreSize screenSize)
    {
        return new CoreSize(screenSize.Width / _zoomX, screenSize.Height / _zoomY);
    }

    /// <summary>
    /// Converts a content size to screen size.
    /// </summary>
    /// <param name="contentSize">The size in content coordinates.</param>
    /// <returns>The size in screen coordinates.</returns>
    public CoreSize ContentToScreen(CoreSize contentSize)
    {
        return new CoreSize(contentSize.Width * _zoomX, contentSize.Height * _zoomY);
    }

    /// <summary>
    /// Gets the transformation matrix from screen to content coordinates.
    /// </summary>
    /// <returns>The inverted matrix, or the default matrix when not invertible.</returns>
    public CoreMatrix GetScreenToContentMatrix()
    {
        _matrix.TryInvert(out var inverted);
        return inverted;
    }

    /// <summary>
    /// Gets the visible content bounds in content coordinates.
    /// </summary>
    /// <returns>The visible content bounds.</returns>
    public CoreRect GetVisibleContentBounds()
    {
        var viewportSize = _host.ViewportSize;
        return ViewportToContent(new CoreRect(0, 0, viewportSize.Width, viewportSize.Height));
    }

    /// <summary>
    /// Gets the viewport bounds in viewport coordinates.
    /// </summary>
    /// <returns>The viewport bounds.</returns>
    public CoreRect GetViewportBounds()
    {
        var viewportSize = _host.ViewportSize;
        return new CoreRect(0, 0, viewportSize.Width, viewportSize.Height);
    }

    /// <summary>
    /// Determines if a rectangle in content coordinates is visible in the viewport.
    /// </summary>
    /// <param name="rect">The rectangle in content coordinates.</param>
    /// <returns>True if any part of the rectangle is visible.</returns>
    public bool IsRectangleVisible(CoreRect rect)
    {
        return rect.Intersects(GetVisibleContentBounds());
    }

    /// <summary>
    /// Determines if a point in content coordinates is visible in the viewport.
    /// </summary>
    /// <param name="point">The point in content coordinates.</param>
    /// <returns>True if the point is visible.</returns>
    public bool IsPointVisible(CorePoint point)
    {
        return GetVisibleContentBounds().Contains(point);
    }

    /// <summary>
    /// Gets the intersection of a rectangle with the visible content bounds.
    /// </summary>
    /// <param name="rect">The rectangle in content coordinates.</param>
    /// <returns>The visible portion of the rectangle.</returns>
    public CoreRect GetVisiblePortion(CoreRect rect)
    {
        return rect.Intersect(GetVisibleContentBounds());
    }

    /// <summary>
    /// Pans the content so that a rectangle in content coordinates becomes fully visible.
    /// </summary>
    /// <param name="targetBounds">The target rectangle in content coordinates.</param>
    /// <returns>True when the target is (or was made) visible.</returns>
    public bool BringIntoView(CoreRect targetBounds)
    {
        if (!_host.HasElement)
        {
            return false;
        }

        // Account for the layout offset of the element and transform the target bounds to viewport coordinates
        var adjustedBounds = TransformContentToViewport(targetBounds, LayoutOffset, _matrix);

        // Get current viewport
        var viewportRect = GetViewportBounds();

        // Check if already fully visible
        if (viewportRect.Contains(adjustedBounds))
        {
            return true;
        }

        // Calculate required pan to bring target into view
        var deltaX = 0.0;
        var deltaY = 0.0;

        // Check horizontal visibility
        if (adjustedBounds.Left < viewportRect.Left)
        {
            deltaX = viewportRect.Left - adjustedBounds.Left;
        }
        else if (adjustedBounds.Right > viewportRect.Right)
        {
            deltaX = viewportRect.Right - adjustedBounds.Right;
        }

        // Check vertical visibility
        if (adjustedBounds.Top < viewportRect.Top)
        {
            deltaY = viewportRect.Top - adjustedBounds.Top;
        }
        else if (adjustedBounds.Bottom > viewportRect.Bottom)
        {
            deltaY = viewportRect.Bottom - adjustedBounds.Bottom;
        }

        // Apply pan if needed
        if (deltaX != 0 || deltaY != 0)
        {
            PanDelta(deltaX, deltaY);
        }

        return true;
    }

    #endregion

    #region Zoom to rectangle, saved views, discrete zoom

    /// <summary>
    /// Zooms to fit a specific rectangle in content coordinates.
    /// </summary>
    /// <param name="rect">The rectangle to zoom to.</param>
    /// <param name="padding">Optional padding around the rectangle.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void ZoomToRectangle(CoreRect rect, CoreThickness? padding = null, bool animate = true)
    {
        if (!_host.HasElement || rect.Width == 0 || rect.Height == 0)
            return;

        if (_updating)
            return;
        _updating = true;

        var layoutOffset = LayoutOffset;
        var viewportSize = _host.ViewportSize;

        var pad = padding ?? new CoreThickness(0);
        var viewportWidth = viewportSize.Width - pad.Left - pad.Right;
        var viewportHeight = viewportSize.Height - pad.Top - pad.Bottom;

        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            _updating = false;
            return;
        }

        var zoomX = viewportWidth / rect.Width;
        var zoomY = viewportHeight / rect.Height;
        var zoom = Min(zoomX, zoomY);

        // Apply discrete zoom levels if enabled
        if (HasDiscreteZoomLevels())
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

        _matrix = new CoreMatrix(zoom, 0, 0, zoom, offsetX, offsetY);
        Invalidate(!animate);

        _updating = false;
        AddToViewHistory();
    }

    /// <summary>
    /// Zooms to fit a specific rectangle into an exact viewport rectangle.
    /// </summary>
    /// <param name="rect">The rectangle in content coordinates.</param>
    /// <param name="viewportRect">The target rectangle in viewport coordinates.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    public void ZoomToRectangleExact(CoreRect rect, CoreRect viewportRect, bool animate = true)
    {
        if (!_host.HasElement || rect.Width == 0 || rect.Height == 0)
            return;

        if (_updating)
            return;
        _updating = true;

        var layoutOffset = LayoutOffset;

        var zoomX = viewportRect.Width / rect.Width;
        var zoomY = viewportRect.Height / rect.Height;
        var zoom = Min(zoomX, zoomY);

        if (HasDiscreteZoomLevels())
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
        var previousSuppressConstraints = _suppressConstraints;
        _suppressConstraints = true;

        _matrix = new CoreMatrix(zoom, 0, 0, zoom, offsetX, offsetY);
        Invalidate(!animate);

        _suppressConstraints = previousSuppressConstraints;
        _updating = false;
        AddToViewHistory();
    }

    /// <summary>
    /// Saves the current view with a name.
    /// </summary>
    /// <param name="name">The name for this view.</param>
    /// <param name="description">Optional description.</param>
    /// <exception cref="ArgumentException">The name is empty.</exception>
    public void SaveView(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("View name cannot be empty", nameof(name));

        _savedViews[name] = new CoreSavedView(name, _matrix, _settings.Stretch, description, DateTime.UtcNow);
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

        _settings.Stretch = savedView.Stretch;
        SetMatrix(savedView.Matrix, !animate);
        return true;
    }

    /// <summary>
    /// Gets a saved view by name.
    /// </summary>
    /// <param name="name">The name of the view.</param>
    /// <returns>The saved view, or null if not found.</returns>
    public CoreSavedView? GetSavedView(string name)
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
    public IReadOnlyCollection<CoreSavedView> GetSavedViews()
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

    private bool HasDiscreteZoomLevels()
    {
        var levels = _settings.DiscreteZoomLevels;
        return _settings.EnableDiscreteZoomLevels && levels != null && levels.Length > 0;
    }

    /// <summary>
    /// Gets the nearest discrete zoom level to the target zoom.
    /// </summary>
    /// <param name="targetZoom">The target zoom level.</param>
    /// <returns>The nearest discrete zoom level, or the target zoom when no levels are configured.</returns>
    public double GetNearestDiscreteZoomLevel(double targetZoom)
    {
        var levels = _settings.DiscreteZoomLevels;
        if (levels == null || levels.Length == 0)
            return targetZoom;

        var nearest = levels[0];
        var minDiff = Abs(targetZoom - nearest);

        foreach (var level in levels)
        {
            var diff = Abs(targetZoom - level);
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
    /// <returns>The next zoom level, or the maximum level when at maximum.</returns>
    public double GetNextDiscreteZoomLevel()
    {
        if (!HasDiscreteZoomLevels())
            return _zoomX * _settings.ZoomSpeed;

        var sorted = _settings.DiscreteZoomLevels!.OrderBy(z => z).ToArray();
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
    /// <returns>The previous zoom level, or the minimum level when at minimum.</returns>
    public double GetPreviousDiscreteZoomLevel()
    {
        if (!HasDiscreteZoomLevels())
            return _zoomX / _settings.ZoomSpeed;

        var sorted = _settings.DiscreteZoomLevels!.OrderByDescending(z => z).ToArray();
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
        if (!_settings.EnableZoom || !_host.HasElement)
            return;

        var zoom = level;
        if (HasDiscreteZoomLevels())
        {
            zoom = GetNearestDiscreteZoomLevel(level);
        }

        ZoomTo(zoom / _zoomX, centerX, centerY, !animate);
    }

    #endregion

    #region Zoom limits, indicator, grid, rotation, state, accessibility

    /// <summary>
    /// Calculates automatic zoom limits based on content and viewport size.
    /// </summary>
    /// <returns>A tuple containing the minimum and maximum zoom values.</returns>
    public (double minZoom, double maxZoom) CalculateDefaultAutoZoomLimits()
    {
        if (!_host.HasElement)
            return (double.NegativeInfinity, double.PositiveInfinity);

        var minZoom = double.NegativeInfinity;
        var maxZoom = double.PositiveInfinity;

        var elementSize = _host.ElementSize;
        var viewportSize = _host.ViewportSize;
        var contentWidth = elementSize.Width;
        var contentHeight = elementSize.Height;
        var viewportWidth = viewportSize.Width;
        var viewportHeight = viewportSize.Height;

        if (_settings.AutoCalculateMinZoom && contentWidth > 0 && contentHeight > 0 && viewportWidth > 0 && viewportHeight > 0)
        {
            // Min zoom is when the entire content fits in the viewport
            var zoomX = viewportWidth / contentWidth;
            var zoomY = viewportHeight / contentHeight;
            minZoom = Min(zoomX, zoomY);
        }

        if (_settings.AutoCalculateMaxZoom && contentWidth > 0 && contentHeight > 0)
        {
            // Max zoom is based on pixel size (1 content pixel = MaxZoomPixelSize screen pixels)
            maxZoom = _settings.MaxZoomPixelSize;
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
    public void GetEffectiveZoomLimits(out double minZoomX, out double maxZoomX, out double minZoomY, out double maxZoomY)
    {
        minZoomX = _settings.MinZoomX;
        maxZoomX = _settings.MaxZoomX;
        minZoomY = _settings.MinZoomY;
        maxZoomY = _settings.MaxZoomY;

        var autoCalculateMinZoom = _settings.AutoCalculateMinZoom;
        var autoCalculateMaxZoom = _settings.AutoCalculateMaxZoom;

        if (autoCalculateMinZoom || autoCalculateMaxZoom)
        {
            var (autoMinZoom, autoMaxZoom) = _host.CalculateAutoZoomLimits();

            if (autoCalculateMinZoom && !double.IsNegativeInfinity(autoMinZoom))
            {
                minZoomX = Max(minZoomX, autoMinZoom);
                minZoomY = Max(minZoomY, autoMinZoom);
            }

            if (autoCalculateMaxZoom && !double.IsPositiveInfinity(autoMaxZoom))
            {
                maxZoomX = Min(maxZoomX, autoMaxZoom);
                maxZoomY = Min(maxZoomY, autoMaxZoom);
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
        return string.Format(_settings.ZoomIndicatorFormat, zoomValue);
    }

    /// <summary>
    /// Snaps a value to the nearest grid point.
    /// </summary>
    /// <param name="value">The value to snap.</param>
    /// <returns>The snapped value.</returns>
    public double SnapToGrid(double value)
    {
        var gridSize = _settings.GridSize;
        if (!_settings.EnableSnapToGrid || gridSize <= 0)
            return value;

        return Round(value / gridSize) * gridSize;
    }

    /// <summary>
    /// Snaps a point to the nearest grid point.
    /// </summary>
    /// <param name="point">The point to snap.</param>
    /// <returns>The snapped point.</returns>
    public CorePoint SnapToGrid(CorePoint point)
    {
        return new CorePoint(SnapToGrid(point.X), SnapToGrid(point.Y));
    }

    /// <summary>
    /// Snaps a rectangle to the nearest grid points.
    /// </summary>
    /// <param name="rect">The rectangle to snap.</param>
    /// <returns>The snapped rectangle.</returns>
    public CoreRect SnapToGrid(CoreRect rect)
    {
        var topLeft = SnapToGrid(new CorePoint(rect.X, rect.Y));
        var bottomRight = SnapToGrid(new CorePoint(rect.Right, rect.Bottom));
        return new CoreRect(topLeft, bottomRight);
    }

    /// <summary>
    /// Rotates the content by the specified angle in degrees.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    /// <param name="animate">Whether to animate the rotation.</param>
    public void Rotate(double degrees, bool animate = true)
    {
        if (!_settings.EnableGestureRotation)
            return;

        var newRotation = _settings.Rotation + degrees;

        // Apply rotation snapping
        var snapAngle = _settings.RotationSnapAngle;
        if (_settings.EnableRotationSnapping && snapAngle > 0)
        {
            newRotation = Round(newRotation / snapAngle) * snapAngle;
        }

        // Apply rotation constraints
        newRotation = ClampValue(newRotation, _settings.MinRotation, _settings.MaxRotation);

        _settings.Rotation = newRotation;

        // Invalidate to apply the rotation transformation
        Invalidate(!animate);
    }

    /// <summary>
    /// Rotates the content by the specified angle around a center point.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    /// <param name="center">The center point for rotation in content coordinates.</param>
    /// <param name="animate">Whether to animate the rotation.</param>
    public void RotateAt(double degrees, CorePoint center, bool animate = true)
    {
        if (!_settings.EnableGestureRotation)
            return;

        // Apply rotation (the center is handled when the render transform is built based on content bounds)
        Rotate(degrees, animate);
    }

    /// <summary>
    /// Resets the rotation to zero.
    /// </summary>
    /// <param name="animate">Whether to animate the reset.</param>
    public void ResetRotation(bool animate = true)
    {
        _settings.Rotation = 0.0;

        // Invalidate to apply the rotation change
        Invalidate(!animate);
    }

    /// <summary>
    /// Snaps the current rotation to the nearest snap angle.
    /// </summary>
    public void SnapRotation()
    {
        var snapAngle = _settings.RotationSnapAngle;
        if (!_settings.EnableRotationSnapping || snapAngle <= 0)
            return;

        _settings.Rotation = Round(_settings.Rotation / snapAngle) * snapAngle;

        // Invalidate to apply the rotation change
        Invalidate(skipTransitions: false);
    }

    /// <summary>
    /// Exports the current state.
    /// </summary>
    /// <returns>A state snapshot.</returns>
    public CoreZoomBorderState ExportState()
    {
        return new CoreZoomBorderState
        {
            Matrix = _matrix,
            Stretch = _settings.Stretch,
            ZoomSpeed = _settings.ZoomSpeed,
            EnablePan = _settings.EnablePan,
            EnableZoom = _settings.EnableZoom,
            Rotation = _settings.Rotation,
            MinZoomX = _settings.MinZoomX,
            MaxZoomX = _settings.MaxZoomX,
            MinZoomY = _settings.MinZoomY,
            MaxZoomY = _settings.MaxZoomY,
            EnableConstrains = _settings.EnableConstrains,
            EnableAnimations = _settings.EnableAnimations,
            AnimationDuration = _settings.AnimationDuration,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Imports a previously exported state.
    /// </summary>
    /// <param name="state">The state to import.</param>
    /// <param name="animate">Whether to animate the state change.</param>
    public void ImportState(CoreZoomBorderState? state, bool animate = true)
    {
        if (state == null)
            return;

        _settings.Stretch = state.Stretch;
        _settings.ZoomSpeed = state.ZoomSpeed;
        _settings.EnablePan = state.EnablePan;
        _settings.EnableZoom = state.EnableZoom;
        _settings.Rotation = state.Rotation;
        _settings.MinZoomX = state.MinZoomX;
        _settings.MaxZoomX = state.MaxZoomX;
        _settings.MinZoomY = state.MinZoomY;
        _settings.MaxZoomY = state.MaxZoomY;
        _settings.EnableConstrains = state.EnableConstrains;
        _settings.EnableAnimations = state.EnableAnimations;
        _settings.AnimationDuration = state.AnimationDuration;

        SetMatrix(state.Matrix, !animate);
    }

    /// <summary>
    /// Updates accessibility descriptions based on current state.
    /// </summary>
    public void UpdateAccessibilityDescriptions()
    {
        // Update zoom level description
        var zoomPercent = _zoomX * 100;
        _settings.ZoomLevelDescription = $"Zoom level: {zoomPercent:F0}%";

        // Update pan position description
        _settings.PanPositionDescription = $"Pan position: X={_offsetX:F0}, Y={_offsetY:F0}";
    }

    /// <summary>
    /// Gets the current accessibility description.
    /// </summary>
    /// <returns>A combined accessibility description.</returns>
    public string GetAccessibilityDescription()
    {
        UpdateAccessibilityDescriptions();
        return $"{_settings.ZoomLevelDescription}. {_settings.PanPositionDescription}";
    }

    #endregion

    #region Constraints and invalidation

    private void Constrain()
    {
        // Get effective zoom limits (considering auto-calculation)
        GetEffectiveZoomLimits(out var effectiveMinZoomX, out var effectiveMaxZoomX, out var effectiveMinZoomY, out var effectiveMaxZoomY);

        var zoomX = ClampValue(_matrix.M11, effectiveMinZoomX, effectiveMaxZoomX);
        var zoomY = ClampValue(_matrix.M22, effectiveMinZoomY, effectiveMaxZoomY);
        var offsetX = ClampValue(_matrix.M31, _settings.MinOffsetX, _settings.MaxOffsetX);
        var offsetY = ClampValue(_matrix.M32, _settings.MinOffsetY, _settings.MaxOffsetY);
        _matrix = new CoreMatrix(zoomX, 0.0, 0.0, zoomY, offsetX, offsetY);

        // Apply content bounds restriction
        ApplyContentBoundsRestriction();

        if (_settings.BoundsMode == ContentBoundsMode.Custom && !_host.ValidateTransform(_matrix))
        {
            _matrix = _lastValidMatrix;
            return;
        }

        _lastValidMatrix = _matrix;
    }

    private void ApplyContentBoundsRestriction()
    {
        var boundsMode = _settings.BoundsMode;
        if (boundsMode == ContentBoundsMode.Unrestricted || !_host.HasElement)
            return;

        switch (boundsMode)
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
                ApplyCustomBounds(_host.GetContentBounds());
                break;
        }
    }

    /// <summary>
    /// Gets the default content bounds (the untransformed child element bounds in content coordinates).
    /// </summary>
    /// <returns>The content bounds.</returns>
    public CoreRect GetDefaultContentBounds()
    {
        if (!_host.HasElement)
        {
            return default;
        }

        var elementSize = _host.ElementSize;
        return new CoreRect(0, 0, elementSize.Width, elementSize.Height);
    }

    private void ApplyKeepContentVisible()
    {
        var layoutOffset = LayoutOffset;
        var elementSize = _host.ElementSize;
        var viewportSize = _host.ViewportSize;

        var contentWidth = elementSize.Width * _matrix.M11;
        var contentHeight = elementSize.Height * _matrix.M22;
        var viewportWidth = viewportSize.Width;
        var viewportHeight = viewportSize.Height;

        var minimumVisibleContentPercentage = _settings.MinimumVisibleContentPercentage;
        var minVisibleWidth = contentWidth * minimumVisibleContentPercentage;
        var minVisibleHeight = contentHeight * minimumVisibleContentPercentage;

        var offsetX = _matrix.M31;
        var offsetY = _matrix.M32;

        // Apply padding
        var padding = _settings.BoundsPadding;

        // The actual visual position is: layoutOffset + matrix offset
        // So: matrix offset = desired visual position - layoutOffset

        // Constrain X
        var maxOffsetX = viewportWidth - minVisibleWidth + padding.Right - layoutOffset.X;
        var minOffsetX = -contentWidth + minVisibleWidth - padding.Left - layoutOffset.X;
        offsetX = ClampValue(offsetX, minOffsetX, maxOffsetX);

        // Constrain Y
        var maxOffsetY = viewportHeight - minVisibleHeight + padding.Bottom - layoutOffset.Y;
        var minOffsetY = -contentHeight + minVisibleHeight - padding.Top - layoutOffset.Y;
        offsetY = ClampValue(offsetY, minOffsetY, maxOffsetY);

        _matrix = new CoreMatrix(_matrix.M11, 0.0, 0.0, _matrix.M22, offsetX, offsetY);
    }

    private void ApplyFillViewport()
    {
        var layoutOffset = LayoutOffset;
        var elementSize = _host.ElementSize;
        var viewportSize = _host.ViewportSize;

        var contentWidth = elementSize.Width * _matrix.M11;
        var contentHeight = elementSize.Height * _matrix.M22;
        var viewportWidth = viewportSize.Width;
        var viewportHeight = viewportSize.Height;

        var offsetX = _matrix.M31;
        var offsetY = _matrix.M32;

        var padding = _settings.BoundsPadding;

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
            offsetY = ClampValue(offsetY, viewportHeight - contentHeight - padding.Top - layoutOffset.Y, padding.Bottom - layoutOffset.Y);
        }

        _matrix = new CoreMatrix(_matrix.M11, 0.0, 0.0, _matrix.M22, offsetX, offsetY);
    }

    private void ApplyKeepCentered()
    {
        var layoutOffset = LayoutOffset;
        var elementSize = _host.ElementSize;
        var viewportSize = _host.ViewportSize;

        var contentWidth = elementSize.Width * _matrix.M11;
        var contentHeight = elementSize.Height * _matrix.M22;
        var viewportWidth = viewportSize.Width;
        var viewportHeight = viewportSize.Height;

        // Calculate where the content should be centered in viewport coordinates
        var centeredX = (viewportWidth - contentWidth) / 2.0;
        var centeredY = (viewportHeight - contentHeight) / 2.0;

        // The actual visual position is: layoutOffset + matrix offset
        // So: matrix offset = desired position - layoutOffset
        var offsetX = centeredX - layoutOffset.X;
        var offsetY = centeredY - layoutOffset.Y;

        _matrix = new CoreMatrix(_matrix.M11, 0.0, 0.0, _matrix.M22, offsetX, offsetY);
    }

    private void ApplyCustomBounds(CoreRect customBounds)
    {
        if (!_host.HasElement || customBounds.Width <= 0 || customBounds.Height <= 0)
        {
            return;
        }

        var transformedBounds = TransformContentToViewport(customBounds, LayoutOffset, _matrix);
        var viewportSize = _host.ViewportSize;
        var viewportWidth = viewportSize.Width;
        var viewportHeight = viewportSize.Height;
        var padding = _settings.BoundsPadding;

        double targetX;
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

        double targetY;
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

        _matrix = new CoreMatrix(
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

        if (!_host.HasElement)
        {
            Log("[Invalidate] End");
            return;
        }

        if (_settings.EnableConstrains && !_suppressConstraints)
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

    private void ShowZoomIndicatorTemporarily()
    {
        if (!_settings.ShowZoomIndicator)
            return;

        _zoomIndicatorVisible = true;
        _host.OnZoomIndicatorVisibilityChanged(true);

        // Reset or start the auto-hide timer
        _host.StartZoomIndicatorTimer(_settings.ZoomIndicatorAutoHideDuration);
    }

    private void InvalidateProperties()
    {
        _zoomX = _matrix.M11;
        _zoomY = _matrix.M22;
        _offsetX = _matrix.M31;
        _offsetY = _matrix.M32;
        _host.UpdateViewProperties(_zoomX, _zoomY, _offsetX, _offsetY);
    }

    /// <summary>
    /// Calculates the render matrix (pan, zoom and rotation) applied to the child element.
    /// </summary>
    /// <returns>The render matrix.</returns>
    public CoreMatrix GetRenderMatrix()
    {
        // Apply rotation if enabled and non-zero
        var rotation = _settings.Rotation;
        if (_settings.EnableGestureRotation && Abs(rotation) > 0.0001)
        {
            // Build transform with rotation: first apply scale/translate matrix, then rotate around content center
            var elementSize = _host.ElementSize;
            var centerX = elementSize.Width / 2.0;
            var centerY = elementSize.Height / 2.0;

            // Convert degrees to radians
            var radians = rotation * PI / 180.0;

            // Create rotation matrix around the center of the content
            var rotationMatrix = MatrixMath.Rotation(radians, centerX, centerY);

            // Combine: first apply pan/zoom matrix, then rotation
            return _matrix * rotationMatrix;
        }

        return _matrix;
    }

    private void InvalidateElement(bool skipTransitions)
    {
        if (!_host.HasElement)
        {
            return;
        }

        _host.ApplyTransform(GetRenderMatrix(), skipTransitions);
    }

    /// <summary>
    /// Recalculates the logical scroll state and raises <see cref="ScrollInvalidated"/>.
    /// </summary>
    public void InvalidateScrollable()
    {
        if (!_host.HasElement)
        {
            return;
        }

        var contentBounds = _settings.BoundsMode == ContentBoundsMode.Custom
            ? _host.GetContentBounds()
            : GetDefaultContentBounds();

        CalculateScrollable(contentBounds, LayoutOffset, _host.ViewportSize, _matrix, out var extent, out var viewport, out var offset);

        _extent = extent;
        _scrollOffset = offset;
        _viewport = viewport;

        // Temporarily set updating flag to prevent feedback loop when a scroll viewer
        // reads the new offset and sets it back through SetScrollOffset
        var wasUpdating = _updating;
        _updating = true;

        ScrollInvalidated?.Invoke(this, EventArgs.Empty);

        _updating = wasUpdating;
    }

    /// <summary>
    /// Sets the logical scroll offset (used by scroll viewer integrations).
    /// </summary>
    /// <param name="value">The new scroll offset.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void SetScrollOffset(CoreVector value, bool skipTransitions)
    {
        Log($"[Offset] offset value: {value}");
        if (_updating)
        {
            return;
        }

        _updating = true;

        var dx = _scrollOffset.X - value.X;
        var dy = _scrollOffset.Y - value.Y;

        _scrollOffset = value;

        Log($"[Offset] offset: {_scrollOffset}, dx: {dx}, dy: {dy}");

        if (dx != 0 || dy != 0)
        {
            _matrix = MatrixMath.ScaleAndTranslate(_zoomX, _zoomY, _matrix.M31 + dx, _matrix.M32 + dy);
            Invalidate(skipTransitions);
        }

        _updating = false;
    }

    #endregion

    #region Pan and zoom operations

    /// <summary>
    /// Set pan and zoom matrix.
    /// </summary>
    /// <param name="matrix">The matrix to set as current.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void SetMatrix(CoreMatrix matrix, bool skipTransitions = false)
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
        MatrixChanged?.Invoke(this, new CoreMatrixChangedEventArgs(
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
            "SetMatrix"));

        _updating = false;
    }

    /// <summary>
    /// Reset pan and zoom matrix.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ResetMatrix(bool skipTransitions = false)
    {
        var previousMatrix = _matrix;
        SetMatrix(CoreMatrix.Identity, skipTransitions);

        // Raise MatrixReset event
        MatrixReset?.Invoke(this, new CoreMatrixChangedEventArgs(
            CoreMatrix.Identity,
            previousMatrix,
            1.0,
            1.0,
            0.0,
            0.0,
            previousMatrix.M11,
            previousMatrix.M22,
            previousMatrix.M31,
            previousMatrix.M32,
            "ResetMatrix"));

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
        if (_settings.EnableConstrains)
        {
            GetEffectiveZoomLimits(out var effectiveMinZoomX, out var effectiveMaxZoomX, out var effectiveMinZoomY, out var effectiveMaxZoomY);
            var effectiveMinZoom = Max(effectiveMinZoomX, effectiveMinZoomY);
            var effectiveMaxZoom = Min(effectiveMaxZoomX, effectiveMaxZoomY);
            clampedZoom = Max(effectiveMinZoom, Min(zoom, effectiveMaxZoom));
        }

        _updating = true;

        Log("[Zoom]");
        var previousMatrix = _matrix;
        var previousZoomX = _zoomX;
        var previousZoomY = _zoomY;

        _matrix = MatrixMath.ScaleAt(clampedZoom, clampedZoom, x, y);
        Invalidate(skipTransitions);

        // Raise ZoomStarted event
        ZoomStarted?.Invoke(this, new CoreZoomEventArgs(
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
            previousMatrix));

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

        if (_settings.EnableConstrains)
        {
            GetEffectiveZoomLimits(out var effectiveMinZoomX, out var effectiveMaxZoomX, out var effectiveMinZoomY, out var effectiveMaxZoomY);

            if ((_zoomX >= effectiveMaxZoomX && _zoomY >= effectiveMaxZoomY && ratio > 1) ||
                (_zoomX <= effectiveMinZoomX && _zoomY <= effectiveMinZoomY && ratio < 1))
            {
                return;
            }

            // Calculate clamped ratio to prevent exceeding limits and causing translation jump
            if (ratio > 1) // Zooming in
            {
                var maxRatioX = effectiveMaxZoomX / _zoomX;
                var maxRatioY = effectiveMaxZoomY / _zoomY;
                effectiveRatio = Min(ratio, Min(maxRatioX, maxRatioY));
            }
            else if (ratio < 1) // Zooming out
            {
                var minRatioX = effectiveMinZoomX / _zoomX;
                var minRatioY = effectiveMinZoomY / _zoomY;
                effectiveRatio = Max(ratio, Max(minRatioX, minRatioY));
            }

            // Don't proceed if effective ratio is essentially 1 (no zoom change)
            if (Abs(effectiveRatio - 1.0) < 1e-10)
            {
                return;
            }
        }

        _updating = true;

        Log($"[ZoomTo] factor: {ratio}, center: {x}, {y}");
        var previousMatrix = _matrix;
        var previousZoomX = _zoomX;
        var previousZoomY = _zoomY;

        _matrix = MatrixMath.ScaleAtPrepend(_matrix, effectiveRatio, effectiveRatio, x, y);
        Invalidate(skipTransitions);

        // Raise ZoomDeltaChanged event
        ZoomDeltaChanged?.Invoke(this, new CoreZoomEventArgs(
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
            previousMatrix));

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
        ZoomStep(_settings.ZoomSpeed, skipTransitions);
    }

    /// <summary>
    /// Zoom out one step positive delta ratio and panel center point.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomOut(bool skipTransitions = false)
    {
        ZoomStep(1 / _settings.ZoomSpeed, skipTransitions);
    }

    private void ZoomStep(double ratio, bool skipTransitions)
    {
        if (!_host.HasElement)
        {
            return;
        }

        var previousZoomX = _zoomX;
        var previousZoomY = _zoomY;
        var previousMatrix = _matrix;

        var elementSize = _host.ElementSize;
        var x = elementSize.Width / 2.0;
        var y = elementSize.Height / 2.0;
        ZoomTo(ratio, x, y, skipTransitions);

        // Raise ZoomEnded event
        ZoomEnded?.Invoke(this, new CoreZoomEventArgs(
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
            previousMatrix));
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
        var realDelta = Sign(delta) * Pow(Abs(delta), _settings.PowerFactor);
        ZoomTo(Pow(_settings.ZoomSpeed, realDelta), x, y, skipTransitions || Abs(realDelta) <= _settings.TransitionThreshold);
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

        _matrix = MatrixMath.ScaleAndTranslate(_zoomX, _zoomY, _matrix.M31 + dx, _matrix.M32 + dy);
        Invalidate(skipTransitions);

        // Raise PanContinued event
        PanContinued?.Invoke(this, new CorePanEventArgs(
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            previousOffsetX,
            previousOffsetY,
            dx,
            dy,
            _matrix,
            previousMatrix));

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

        _matrix = MatrixMath.ScaleAndTranslate(_zoomX, _zoomY, x, y);
        Invalidate(skipTransitions);

        // Raise PanContinued event
        PanContinued?.Invoke(this, new CorePanEventArgs(
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            previousOffsetX,
            previousOffsetY,
            x - previousOffsetX,
            y - previousOffsetY,
            _matrix,
            previousMatrix));
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
        _pan = new CorePoint();
        _previous = new CorePoint(x, y);

        // Raise PanStarted event
        PanStarted?.Invoke(this, new CorePanEventArgs(
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            _offsetX,
            _offsetY,
            0.0,
            0.0,
            _matrix,
            _matrix));
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
        _previous = new CorePoint(x, y);
        _pan = new CorePoint(_pan.X + dx, _pan.Y + dy);
        _matrix = MatrixMath.TranslatePrepend(_matrix, _pan.X, _pan.Y);
        Invalidate(skipTransitions);

        // Raise PanContinued event
        PanContinued?.Invoke(this, new CorePanEventArgs(
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            previousOffsetX,
            previousOffsetY,
            dx,
            dy,
            _matrix,
            previousMatrix));

        _updating = false;
    }

    private void ApplyStretch(StretchMode mode, double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions)
    {
        if (_updating)
        {
            return;
        }

        _updating = true;

        Log($"[{mode}] {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
        if (!_host.HasElement)
        {
            _updating = false;
            return;
        }

        _matrix = CalculateMatrix(panelWidth, panelHeight, elementWidth, elementHeight, mode);
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
        => ApplyStretch(StretchMode.None, panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);

    /// <summary>
    /// Zoom and pan to fill panel.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Fill(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        => ApplyStretch(StretchMode.Fill, panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Uniform(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        => ApplyStretch(StretchMode.Uniform, panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio. If aspect of panel is different panel is filled.
    /// </summary>
    /// <param name="panelWidth">The panel width.</param>
    /// <param name="panelHeight">The panel height.</param>
    /// <param name="elementWidth">The element width.</param>
    /// <param name="elementHeight">The element height.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void UniformToFill(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        => ApplyStretch(StretchMode.UniformToFill, panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);

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
        if (!_host.HasElement)
        {
            return;
        }

        switch (_settings.Stretch)
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
        _settings.Stretch = _settings.Stretch switch
        {
            StretchMode.None => StretchMode.Fill,
            StretchMode.Fill => StretchMode.Uniform,
            StretchMode.Uniform => StretchMode.UniformToFill,
            StretchMode.UniformToFill => StretchMode.None,
            _ => _settings.Stretch
        };
    }

    /// <summary>
    /// Zoom and pan using the current viewport and element sizes.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void None(bool skipTransitions = false)
    {
        if (!_host.HasElement)
        {
            return;
        }

        var viewportSize = _host.ViewportSize;
        var elementSize = _host.ElementSize;
        None(viewportSize.Width, viewportSize.Height, elementSize.Width, elementSize.Height, skipTransitions);
    }

    /// <summary>
    /// Zoom and pan to fill panel.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Fill(bool skipTransitions = false) => ApplyStretchWithEvent(StretchMode.Fill, skipTransitions);

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Uniform(bool skipTransitions = false) => ApplyStretchWithEvent(StretchMode.Uniform, skipTransitions);

    /// <summary>
    /// Zoom and pan to panel extents while maintaining aspect ratio. If aspect of panel is different panel is filled.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void UniformToFill(bool skipTransitions = false) => ApplyStretchWithEvent(StretchMode.UniformToFill, skipTransitions);

    private void ApplyStretchWithEvent(StretchMode mode, bool skipTransitions)
    {
        if (!_host.HasElement)
        {
            return;
        }

        var previousStretch = _settings.Stretch;
        var previousMatrix = _matrix;
        var viewportSize = _host.ViewportSize;
        var elementSize = _host.ElementSize;

        ApplyStretch(mode, viewportSize.Width, viewportSize.Height, elementSize.Width, elementSize.Height, skipTransitions);

        // Raise StretchModeChanged event
        viewportSize = _host.ViewportSize;
        elementSize = _host.ElementSize;
        StretchModeChanged?.Invoke(this, new CoreStretchModeChangedEventArgs(
            mode,
            previousStretch,
            _matrix,
            previousMatrix,
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            viewportSize.Width,
            viewportSize.Height,
            elementSize.Width,
            elementSize.Height));

        // Add to view history after the stretch completes
        AddToViewHistory();
    }

    /// <summary>
    /// Zoom and pan child element inside panel using stretch mode.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void AutoFit(bool skipTransitions = false)
    {
        if (!_host.HasElement)
        {
            return;
        }

        var previousMatrix = _matrix;
        var viewportSize = _host.ViewportSize;
        var elementSize = _host.ElementSize;

        AutoFit(viewportSize.Width, viewportSize.Height, elementSize.Width, elementSize.Height, skipTransitions);

        // Raise AutoFitApplied event
        var stretch = _settings.Stretch;
        viewportSize = _host.ViewportSize;
        elementSize = _host.ElementSize;
        AutoFitApplied?.Invoke(this, new CoreStretchModeChangedEventArgs(
            stretch,
            stretch,
            _matrix,
            previousMatrix,
            _zoomX,
            _zoomY,
            _offsetX,
            _offsetY,
            viewportSize.Width,
            viewportSize.Height,
            elementSize.Width,
            elementSize.Height));

        // Add to view history after auto fit completes
        AddToViewHistory();
    }

    private void RaiseZoomChanged()
    {
        ZoomChanged?.Invoke(this, new ZoomChangedEventArgs(_zoomX, _zoomY, _offsetX, _offsetY));
    }

    #endregion

    #region Commands

    /// <summary>
    /// Raises CanExecuteChanged on all commands to refresh their enabled state.
    /// </summary>
    public void RaiseCommandsCanExecuteChanged()
    {
        _zoomInCommand?.RaiseCanExecuteChanged();
        _zoomOutCommand?.RaiseCanExecuteChanged();
        _resetCommand?.RaiseCanExecuteChanged();
        _fitCommand?.RaiseCanExecuteChanged();
        _fillCommand?.RaiseCanExecuteChanged();
        _uniformCommand?.RaiseCanExecuteChanged();
        _uniformToFillCommand?.RaiseCanExecuteChanged();
        _navigateBackCommand?.RaiseCanExecuteChanged();
        _navigateForwardCommand?.RaiseCanExecuteChanged();
        _toggleStretchCommand?.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Raises CanExecuteChanged on navigation commands to refresh their enabled state.
    /// </summary>
    public void RaiseNavigationCommandsCanExecuteChanged()
    {
        _navigateBackCommand?.RaiseCanExecuteChanged();
        _navigateForwardCommand?.RaiseCanExecuteChanged();
    }

    #endregion
}
