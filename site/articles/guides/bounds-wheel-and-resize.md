---
title: "Bounds, Wheel, and Resize"
---

# Bounds, Wheel, and Resize

This guide covers the knobs that make `ZoomBorder` feel predictable in real applications.

## Bounds Modes

`BoundsMode` controls how the viewport is constrained:

- `Unrestricted`
- `KeepContentVisible`
- `FillViewport`
- `KeepCentered`
- `Custom`

Additional supporting properties:

- `BoundsPadding`
- `MinimumVisibleContentPercentage`
- `EnableConstrains`

Use `Custom` when a subclass overrides `GetContentBounds()` or `ValidateTransform(...)`. The returned rectangle is expressed in child content coordinates and drives both pan constraints and logical scrollbar extent. See [Custom Bounds, Resize, and Rotation](../advanced/custom-bounds-resize-and-rotation.md) for a large-workspace example.

## Wheel Behavior

Wheel input is configurable instead of being hard-coded:

```csharp
zoomBorder.WheelBehavior = WheelBehaviorMode.Zoom;
zoomBorder.WheelWithCtrl = WheelBehaviorMode.Zoom;
zoomBorder.WheelWithShift = WheelBehaviorMode.PanHorizontal;
zoomBorder.WheelZoomSensitivity = 1.0;
zoomBorder.WheelPanSensitivity = 1.0;
```

This is useful when a surface needs horizontal scrolling, precision zoom, or different modifier semantics.

## Resize Behavior

Resize handling is controlled by `ResizeBehavior`:

- `None`
- `MaintainCenter`
- `MaintainTopLeft`
- `MaintainZoom`
- `ReapplyStretch`
- `Custom`

Choose `ReapplyStretch` when the control should behave like a viewer that always re-fits content after host layout changes.

## Dynamic Zoom Limits

Auto-calculated zoom limits are available through:

- `AutoCalculateMinZoom`
- `AutoCalculateMaxZoom`
- `MaxZoomPixelSize`

That keeps very small or very large content usable without manually tuning `MinZoomX`, `MaxZoomX`, `MinZoomY`, and `MaxZoomY` for every surface.
