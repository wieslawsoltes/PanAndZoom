---
title: "ScrollViewer and Logical Scroll"
---

# ScrollViewer and Logical Scroll

`ZoomBorder` implements `ILogicalScrollable` so it can participate in Avalonia scrolling infrastructure instead of fighting it.

## Why This Matters

Embedding zoomable content inside a `ScrollViewer` is common, but naive implementations can create feedback loops between viewport transforms and host scroll offsets.

`ZoomBorder` addresses that through:

- the `ILogicalScrollable` implementation in `ZoomBorder.ILogicalScrollable.cs`
- `CalculateScrollable(...)` for computing extent, viewport, and offset from source bounds and the active matrix

## Practical Guidance

- Put a `ScrollViewer` around the control only when you want scrollbars or host-level scrolling semantics.
- Review wheel configuration carefully; wheel input may need to pan instead of zoom in some host layouts.
- Use the existing unit tests around logical scrolling as the baseline behavior contract when modifying this area.

## Related APIs

- `Avalonia.Controls.PanAndZoom.ZoomBorder.CalculateScrollable(Avalonia.Rect,Avalonia.Size,Avalonia.Media.Transformation.Matrix,Avalonia.Size@,Avalonia.Size@,Avalonia.Vector@)`
- `BringIntoView(...)` behavior through the scrollable contract
- [Bounds, Wheel, and Resize](../guides/bounds-wheel-and-resize.md)

## Uno Platform

WinUI has no `ILogicalScrollable`, so the Uno `PanAndZoom.ZoomBorder` exposes the same logical scroll state as plain members backed by the shared engine:

- `Extent`, `Viewport`, and `ScrollOffset` (a `Windows.Foundation.Point`; setting it pans the content)
- `CanHorizontallyScroll`, `CanVerticallyScroll`, and the `ScrollInvalidated` event
- `BringIntoView(FrameworkElement, Rect)`, also used to handle `BringIntoViewRequested` raised by descendants of the child (for example when a focused control inside the content requests to be shown)
- static `CalculateScrollable(...)` and `TransformContentToViewport(...)` helpers with WinUI types

Use these members to drive custom scroll bars or a minimap instead of wrapping the control in a WinUI `ScrollViewer`.
