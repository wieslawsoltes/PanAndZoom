---
title: "Namespaces PanAndZoom.Core and PanAndZoom"
---

# Namespaces PanAndZoom.Core and PanAndZoom

The `PanAndZoom.Core` assembly (NuGet `PanAndZoom.Core`, `net8.0` and `net10.0`, trimmable and AOT compatible) has no UI framework dependency. It contributes two namespaces:

- `PanAndZoom.Core`: the engine, its contracts, primitives, and helpers
- `PanAndZoom`: the model types shared by the Avalonia and Uno Platform controls

Both controls reference this assembly, so you rarely install it directly. See [Architecture](../concepts/architecture.md) for how the pieces fit together.

## Engine And Contracts (`PanAndZoom.Core`)

| Type | Kind | Purpose |
|---|---|---|
| `PanAndZoom.Core.PanAndZoomEngine` | Sealed class | Owns the transform matrix and implements pan, zoom, rotation, constraints, bounds, history, saved views, input rules, commands, and logical scroll state |
| `PanAndZoom.Core.IPanAndZoomHost` | Interface | UI framework side of a control: layout sizes, `ApplyTransform`, property publication, and virtual extension points |
| `PanAndZoom.Core.IPanAndZoomSettings` | Interface | All behavior settings read by the engine, implemented by the controls on top of their bindable properties |
| `PanAndZoom.Core.PanAndZoomSettings` | Class | Plain settings implementation initialized with the defaults |
| `PanAndZoom.Core.ZoomBorderDefaults` | Static class | Default values shared by both controls (for example `PanButton = Middle`, `ZoomSpeed = 1.2`, `Stretch = Uniform`) |
| `PanAndZoom.Core.ZoomBorderCommand` | Sealed class | `ICommand` implementation behind `ZoomInCommand`, `ResetCommand`, `FitCommand`, and the other built-in commands |
| `PanAndZoom.Core.MatrixTransition` | Sealed class | Linear matrix transition for hosts that drive animations from a render loop (used by the Uno control) |
| `PanAndZoom.Core.MatrixMath` | Static class | Matrix helpers (`Translate`, `ScaleAt`, `Rotation`, `TransformPoint`, ...) wrapped by each control's `MatrixHelper` |

## Primitives (`PanAndZoom.Core`)

| Type | Purpose |
|---|---|
| `PanAndZoom.Core.CoreMatrix` | 3x2 affine matrix (`M11`, `M12`, `M21`, `M22`, `M31`, `M32`) |
| `PanAndZoom.Core.CorePoint` | Point in content or viewport space |
| `PanAndZoom.Core.CoreVector` | Displacement (pan deltas, wheel deltas, scroll offsets) |
| `PanAndZoom.Core.CoreSize` | Viewport and element sizes |
| `PanAndZoom.Core.CoreRect` | Rectangles, intersections, and transformed bounds |
| `PanAndZoom.Core.CoreThickness` | Bounds and centering padding |

The controls convert these to and from native types (`Avalonia.Matrix`, `Microsoft.UI.Xaml.Media.Matrix`, `Windows.Foundation.Point`, ...), so application code normally never sees them unless it uses `ZoomBorder.Engine` directly.

## Input And Event Data (`PanAndZoom.Core`)

| Type | Purpose |
|---|---|
| `PanAndZoom.Core.ZoomBorderKey`, `PanAndZoom.Core.ZoomBorderKeyModifiers`, `PanAndZoom.Core.ZoomBorderPointerButtons` | Framework independent keyboard and pointer input passed to the engine's `Process*` methods |
| `PanAndZoom.Core.CorePanEventArgs`, `PanAndZoom.Core.CoreZoomEventArgs`, `PanAndZoom.Core.CoreMatrixChangedEventArgs`, `PanAndZoom.Core.CoreStretchModeChangedEventArgs`, `PanAndZoom.Core.CoreGestureEventArgs` | Event data records raised by the engine and re-raised by the controls with native event args |
| `PanAndZoom.Core.CoreViewState`, `PanAndZoom.Core.CoreSavedView`, `PanAndZoom.Core.CoreZoomBorderState` | View history entries, saved views, and exported state |

## Shared Model Types (`PanAndZoom`)

| Type | Purpose |
|---|---|
| `PanAndZoom.ButtonName` | Selects the mouse button used for panning |
| `PanAndZoom.StretchMode` | Chooses fit behavior for content |
| `PanAndZoom.ContentBoundsMode` | Constrains how content can move inside the viewport |
| `PanAndZoom.ResizeBehaviorMode` | Controls resize reactions |
| `PanAndZoom.WheelBehaviorMode` | Maps wheel input to zoom or panning |
| `PanAndZoom.DoubleClickZoomMode` | Controls double-click zoom behavior |
| `PanAndZoom.ZoomIndicatorPosition` | Positions custom zoom indicators |
| `PanAndZoom.ZoomChangedEventArgs` and `PanAndZoom.ZoomChangedEventHandler` | Viewport zoom and offset change notifications raised by both controls |

These types were previously in `Avalonia.Controls.PanAndZoom`. Avalonia code that references them needs `using PanAndZoom;` (C#) or `xmlns:pz="using:PanAndZoom"` (XAML, when the types are used as elements). On Uno Platform the control is in the same `PanAndZoom` namespace, so no extra import is needed.

## Driving The Engine Directly

```csharp
using PanAndZoom;
using PanAndZoom.Core;

var engine = zoomBorder.Engine; // Avalonia or Uno ZoomBorder

engine.ZoomTo(2.0, 100, 100);
CoreRect visible = engine.GetVisibleContentBounds();
CoreMatrix matrix = engine.Matrix;
```

Prefer the control members in application code; they take and return native types and are equivalent to the engine calls.
