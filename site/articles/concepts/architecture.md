---
title: "Architecture"
---

# Architecture

PanAndZoom ships one pan and zoom implementation and two thin UI adapters:

| Assembly | Namespace | Role |
|---|---|---|
| `PanAndZoom.Core` (`net8.0`, `net10.0`) | `PanAndZoom.Core`, `PanAndZoom` | UI framework independent engine, contracts, primitives, and shared model types |
| `PanAndZoom` (Avalonia) | `Avalonia.Controls.PanAndZoom` | `ZoomBorder : Border` adapter for Avalonia |
| `PanAndZoom.Uno` (Uno Platform) | `PanAndZoom` | `ZoomBorder : Control` adapter for Uno Platform (WinUI) |

```text
+----------------------------------+      +----------------------------------+
| Avalonia ZoomBorder              |      | Uno Platform ZoomBorder          |
| StyledProperty, ILogicalScroll-  |      | DependencyProperty, pointer      |
| able, gesture recognizers,       |      | capture, manipulations, render   |
| Avalonia transitions             |      | loop driven MatrixTransition     |
+----------------+-----------------+      +-----------------+----------------+
                 |  IPanAndZoomHost + IPanAndZoomSettings   |
                 v                                          v
+----------------------------------------------------------------------------+
| PanAndZoomEngine: matrix, constraints, bounds, history, saved views,       |
| gestures, wheel/keyboard/double-click rules, commands, logical scroll      |
+----------------------------------------------------------------------------+
```

## The Engine

`PanAndZoom.Core.PanAndZoomEngine` owns all pan and zoom state and behavior:

- the content-to-viewport transform matrix, zoom and offset values, and rotation
- constraints (`MinZoomX` ... `MaxOffsetY`), bounds modes, resize behaviors, and dynamic zoom limits
- input rules: pointer panning with `PanButton`, wheel behaviors with modifiers, double-click zoom, keyboard navigation, pinch, and scroll gestures
- view history, saved views, discrete zoom levels, grid snapping, state export and import
- `ICommand` implementations (`ZoomInCommand`, `ResetCommand`, `FitCommand`, ...) built on `PanAndZoom.Core.ZoomBorderCommand`
- logical scroll state (`Extent`, `Viewport`, `ScrollOffset`) and `BringIntoView`
- events (`ZoomChanged`, `MatrixChanged`, `PanStarted`, `GestureStarted`, ...) carrying core event data records such as `CorePanEventArgs` and `CoreMatrixChangedEventArgs`

The engine uses only its own primitives: `CoreMatrix`, `CorePoint`, `CoreRect`, `CoreSize`, `CoreVector`, and `CoreThickness`. `MatrixMath` provides the matrix helpers that both controls wrap in their `MatrixHelper` classes. Input positions passed to the engine are in child element coordinates (the untransformed content space).

## The Host Contract

Each control implements `PanAndZoom.Core.IPanAndZoomHost` (usually in a private nested `EngineHost` class). The host is the UI framework side of the control:

| Member | Purpose |
|---|---|
| `HasElement`, `ViewportSize`, `ElementSize`, `ElementLayoutOffset` | Report layout information |
| `ApplyTransform(CoreMatrix, bool skipTransitions)` | Apply the computed render matrix to the child element, animated or not |
| `UpdateViewProperties(zoomX, zoomY, offsetX, offsetY)` | Publish `ZoomX`, `ZoomY`, `OffsetX`, `OffsetY` so the control can raise property change notifications |
| `OnIsPanningChanged(bool)` | Update the `:isPanning` pseudo class (Avalonia) or the `IsPanning` property and `PanningStates` visual state (Uno) |
| `OnZoomIndicatorVisibilityChanged(bool)`, `StartZoomIndicatorTimer(TimeSpan)` | Drive the zoom indicator auto-hide timer with a native timer |
| `GetContentBounds()`, `ValidateTransform(CoreMatrix)`, `OnResized(...)`, `CalculateAutoZoomLimits()` | Forward to the control's `protected virtual` extension points, so subclasses of either control keep working |

## The Settings Contract

`PanAndZoom.Core.IPanAndZoomSettings` exposes every behavior setting (`PanButton`, `ZoomSpeed`, `Stretch`, `EnablePan`, `BoundsMode`, `WheelBehavior`, `EnableAnimations`, ...). The controls implement it on top of their bindable properties (Avalonia `StyledProperty`, WinUI `DependencyProperty`), so the engine always reads the live property values and bindings keep working.

`PanAndZoom.Core.PanAndZoomSettings` is a plain implementation whose initial values come from `PanAndZoom.Core.ZoomBorderDefaults`, the single source of default values for both controls. Use it when you drive the engine without a UI control, for example in unit tests.

## What The Adapters Do

Each control is responsible only for framework specific work:

- create the engine (`new PanAndZoomEngine(host, settings)`) and expose it through the `Engine` property
- forward lifecycle notifications: `OnAttachedToVisualTree`, `OnElementAttached`, `OnArranged`, `OnViewportSizeChanged`, `OnElementBoundsChanged`, `OnStretchChanged`, `OnBoundsSettingsChanged`
- translate native input into `ProcessPointerPressed`, `ProcessPointerMoved`, `ProcessPointerWheel`, `ProcessDoubleTapped`, `ProcessKeyDown`, `ProcessPinch`, and `ProcessScrollGesture` calls
- convert between native geometry types and the core primitives
- re-raise engine events with the framework specific event args (`PanEventArgs`, `ZoomEventArgs`, `MatrixChangedEventArgs`, ...)

Framework specific parts:

| Concern | Avalonia | Uno Platform |
|---|---|---|
| Animation | Avalonia `TransformOperationsTransition` on the child `RenderTransform` | `PanAndZoom.Core.MatrixTransition` advanced on `CompositionTarget.Rendering` (`IsAnimating`, `RenderMatrix`) |
| Touch | Pinch and scroll gesture recognizers | WinUI manipulations (one finger pan, two finger pinch zoom and rotate) |
| Scrolling | `ILogicalScrollable` | Equivalent members (`Extent`, `Viewport`, `ScrollOffset`, `ScrollInvalidated`, `BringIntoView`) and `BringIntoViewRequested` handling |

## Building Your Own Adapter

Because the engine has no UI dependency, another UI framework can reuse it by implementing `IPanAndZoomHost` and `IPanAndZoomSettings`, forwarding layout and input events, and applying `ApplyTransform` matrices to its content. The Avalonia and Uno `ZoomBorder.EngineHost.cs` files are complete reference implementations.

## Testing Layers

- `tests/PanAndZoom.Core.UnitTests`: xunit engine tests, no UI framework required
- `tests/Avalonia.Controls.PanAndZoom.UnitTests`: Avalonia headless tests of the Avalonia adapter
- `tests/PanAndZoom.Uno.RuntimeTests`: Uno runtime tests of the Uno adapter inside a real Uno Skia desktop app

## Related

- [Transform and Coordinate Spaces](transform-and-coordinate-spaces.md)
- [Interaction Model](interaction-model.md)
- [Namespaces PanAndZoom.Core and PanAndZoom](../reference/namespace-panandzoom-core.md)
- [Quickstart: Uno Platform](../getting-started/quickstart-uno.md)
