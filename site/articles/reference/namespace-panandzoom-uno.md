---
title: "Namespace PanAndZoom (Uno Platform)"
---

# Namespace PanAndZoom (Uno Platform)

The `PanAndZoom.Uno` assembly (NuGet `PanAndZoom.Uno`) contains the Uno Platform (WinUI) control. Its types live in the `PanAndZoom` namespace, the same namespace as the shared enums from `PanAndZoom.Core`, so a single `using PanAndZoom;` or `xmlns:paz="using:PanAndZoom"` covers everything.

Targets: `net10.0`, `net10.0-desktop`, `net10.0-browserwasm`, `net10.0-android`, `net10.0-ios`, and `net10.0-windows10.0.26100` (`Uno.Sdk` 6.7.30, Skia renderer).

This assembly is not part of the generated API pages (they build the `net8.0` target); the member list matches the Avalonia `ZoomBorder` with the differences listed below.

## Core Types

| Type | Kind | Primary article |
|---|---|---|
| `PanAndZoom.ZoomBorder` | Templated control (`Control`, content property `Child`) | [getting-started/quickstart-uno.md](../getting-started/quickstart-uno.md) |
| `PanAndZoom.MatrixHelper` | Static class over `Microsoft.UI.Xaml.Media.Matrix` | [concepts/transform-and-coordinate-spaces.md](../concepts/transform-and-coordinate-spaces.md) |
| `PanAndZoom.ZoomBorderState` | Class | [concepts/view-state-and-persistence.md](../concepts/view-state-and-persistence.md) |
| `PanAndZoom.ViewState` | Struct | [concepts/view-state-and-persistence.md](../concepts/view-state-and-persistence.md) |
| `PanAndZoom.SavedView` | Struct | [concepts/view-state-and-persistence.md](../concepts/view-state-and-persistence.md) |

## Event Args And Delegates

| Type | Typical use |
|---|---|
| `PanAndZoom.ZoomChangedEventArgs` and `PanAndZoom.ZoomChangedEventHandler` (from `PanAndZoom.Core`) | Listen for viewport zoom and offset changes |
| `PanAndZoom.ZoomEventArgs` and `PanAndZoom.ZoomEventHandler` | Observe zoom-specific operations |
| `PanAndZoom.PanEventArgs` and `PanAndZoom.PanEventHandler` | Observe pan operations |
| `PanAndZoom.GestureEventArgs` and `PanAndZoom.GestureEventHandler` | Observe gesture-driven actions |
| `PanAndZoom.MatrixChangedEventArgs` and `PanAndZoom.MatrixChangedEventHandler` | React to transform-matrix updates |
| `PanAndZoom.StretchModeChangedEventArgs` and `PanAndZoom.StretchModeChangedEventHandler` | Track stretch-mode transitions |

## Uno Specific Members

| Member | Description |
|---|---|
| `Child` | The panned and zoomed element (XAML content property) |
| `ClipToBounds` | Clips content to the control bounds, default `true` |
| `Engine` | The underlying `PanAndZoom.Core.PanAndZoomEngine` |
| `ZoomX`, `ZoomY`, `OffsetX`, `OffsetY`, `IsZoomIndicatorVisible`, `IsPanning` | Read-only dependency properties (bindable) |
| `IsAnimating`, `RenderMatrix` | Animation state; `RenderMatrix` is the matrix currently applied to the child and differs from `Matrix` while animating or rotated |
| `Rotation` | Hides `UIElement.Rotation`; content rotation is applied through the render matrix |
| `ScreenToContentVector(Point)`, `ContentToScreenVector(Point)` | Vector conversions (WinUI has no `Vector` type) |
| `Extent`, `Viewport`, `ScrollOffset`, `CanHorizontallyScroll`, `CanVerticallyScroll`, `ScrollInvalidated` | Logical scroll state (WinUI has no `ILogicalScrollable`) |
| `BringIntoView(FrameworkElement, Rect)` | Pans so a descendant becomes visible; `BringIntoViewRequested` raised by descendants is handled the same way |
| `CenterOn(FrameworkElement)`, `GetElementContentBounds(FrameworkElement)` | Element based centering and bounds |
| `PanningStates` visual state group | `Panning` and `NotPanning` states for templates |

## Input Handling

- Pointer: `PanButton` drag with pointer capture, wheel with `WheelBehavior`, `WheelWithCtrl`, and `WheelWithShift`, `DoubleTapped` with `DoubleClickZoomMode`.
- Keyboard: handled in `OnKeyDown` when the control has focus (`IsTabStop` is `true` by default and the control focuses itself on pointer press).
- Touch: WinUI manipulations when `EnableGestures` is `true`; one finger pans, two fingers pinch zoom and rotate.

## Related

- [Quickstart: Uno Platform](../getting-started/quickstart-uno.md)
- [Architecture](../concepts/architecture.md)
- [Namespaces PanAndZoom.Core and PanAndZoom](namespace-panandzoom-core.md)
- [Namespace Avalonia.Controls.PanAndZoom](namespace-panandzoom.md)
