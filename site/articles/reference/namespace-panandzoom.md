---
title: "Namespace Avalonia.Controls.PanAndZoom"
---

# Namespace Avalonia.Controls.PanAndZoom

This namespace contains the Avalonia control (`PanAndZoom` package), its state objects, and its event args. The shared enums and the `ZoomChanged` event types live in the `PanAndZoom` namespace of the `PanAndZoom.Core` assembly (see [Namespaces PanAndZoom.Core and PanAndZoom](namespace-panandzoom-core.md)), so Avalonia code typically needs both `using Avalonia.Controls.PanAndZoom;` and `using PanAndZoom;`.

## Core Types

| Type | Kind | Primary article |
|---|---|---|
| `Avalonia.Controls.PanAndZoom.ZoomBorder` | Control | [getting-started/quickstart-zoom-border.md](../getting-started/quickstart-zoom-border.md) |
| `Avalonia.Controls.PanAndZoom.MatrixHelper` | Static class | [concepts/transform-and-coordinate-spaces.md](../concepts/transform-and-coordinate-spaces.md) |
| `Avalonia.Controls.PanAndZoom.ZoomBorderState` | Class | [concepts/view-state-and-persistence.md](../concepts/view-state-and-persistence.md) |
| `Avalonia.Controls.PanAndZoom.ViewState` | Struct | [concepts/view-state-and-persistence.md](../concepts/view-state-and-persistence.md) |
| `Avalonia.Controls.PanAndZoom.SavedView` | Struct | [concepts/view-state-and-persistence.md](../concepts/view-state-and-persistence.md) |

## Enums (namespace `PanAndZoom`, assembly `PanAndZoom.Core`)

| Type | Purpose |
|---|---|
| `PanAndZoom.ButtonName` | Selects the mouse button used for panning |
| `PanAndZoom.StretchMode` | Chooses fit behavior for content |
| `PanAndZoom.ContentBoundsMode` | Constrains how content can move inside the viewport |
| `PanAndZoom.ResizeBehaviorMode` | Controls resize reactions |
| `PanAndZoom.WheelBehaviorMode` | Maps wheel input to zoom or panning |
| `PanAndZoom.DoubleClickZoomMode` | Controls double-click zoom behavior |
| `PanAndZoom.ZoomIndicatorPosition` | Positions custom zoom indicators |

## Event Args And Delegates

| Type | Typical use |
|---|---|
| `PanAndZoom.ZoomChangedEventArgs` and `PanAndZoom.ZoomChangedEventHandler` (assembly `PanAndZoom.Core`) | Listen for viewport zoom and offset changes |
| `Avalonia.Controls.PanAndZoom.ZoomEventArgs` and `Avalonia.Controls.PanAndZoom.ZoomEventHandler` | Observe zoom-specific operations |
| `Avalonia.Controls.PanAndZoom.PanEventArgs` and `Avalonia.Controls.PanAndZoom.PanEventHandler` | Observe pan operations |
| `Avalonia.Controls.PanAndZoom.GestureEventArgs` and `Avalonia.Controls.PanAndZoom.GestureEventHandler` | Observe gesture-driven actions |
| `Avalonia.Controls.PanAndZoom.MatrixChangedEventArgs` and `Avalonia.Controls.PanAndZoom.MatrixChangedEventHandler` | React to transform-matrix updates |
| `Avalonia.Controls.PanAndZoom.StretchModeChangedEventArgs` and `Avalonia.Controls.PanAndZoom.StretchModeChangedEventHandler` | Track stretch-mode transitions |

## Engine Access

`Avalonia.Controls.PanAndZoom.ZoomBorder.Engine` exposes the underlying `PanAndZoom.Core.PanAndZoomEngine`. The built-in commands are `PanAndZoom.Core.ZoomBorderCommand` instances. See [Architecture](../concepts/architecture.md).
