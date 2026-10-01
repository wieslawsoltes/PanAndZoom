---
title: "Transform and Coordinate Spaces"
---

# Transform and Coordinate Spaces

`ZoomBorder` maintains a transform matrix that maps content coordinates into viewport coordinates. Most higher-level APIs are thin wrappers over that model.

## The Three Views Of Space

- content space: the original coordinate system of the child control
- viewport space: the visible region inside the `ZoomBorder`
- screen-like vectors and sizes: transformed measurements derived from the active matrix

## Core Conversion APIs

Use these methods instead of manually duplicating matrix math:

- `Avalonia.Controls.PanAndZoom.ZoomBorder.ViewportToContent(Avalonia.Point)`
- `Avalonia.Controls.PanAndZoom.ZoomBorder.ContentToViewport(Avalonia.Point)`
- `Avalonia.Controls.PanAndZoom.ZoomBorder.ViewportToContent(Avalonia.Rect)`
- `Avalonia.Controls.PanAndZoom.ZoomBorder.ContentToViewport(Avalonia.Rect)`
- `Avalonia.Controls.PanAndZoom.ZoomBorder.GetContentToScreenMatrix`
- `Avalonia.Controls.PanAndZoom.ZoomBorder.GetScreenToContentMatrix`

These become important when:

- zooming to a region selected in viewport space
- showing overlays aligned with content coordinates
- translating pointer positions into domain coordinates on a diagram or image

## Matrix Helpers

`Avalonia.Controls.PanAndZoom.MatrixHelper` contains reusable helpers such as:

- `Translate(...)`
- `ScaleAt(...)`
- `Rotation(...)`
- `TransformPoint(...)`

It is useful when you build custom overlays or test matrix calculations separately from the control.

## Visible Area Queries

Two methods are especially important for viewport-aware logic:

- `Avalonia.Controls.PanAndZoom.ZoomBorder.GetVisibleContentBounds`
- `Avalonia.Controls.PanAndZoom.ZoomBorder.GetViewportBounds`

They allow item culling, overlay rendering, and "jump to region" UX without duplicating coordinate conversion code.

## Uno Platform

The Uno `PanAndZoom.ZoomBorder` exposes the same conversion APIs with WinUI types:

- points, rectangles, and sizes use `Windows.Foundation.Point`, `Rect`, and `Size`
- matrices are `Microsoft.UI.Xaml.Media.Matrix`, whose translation components are `OffsetX` and `OffsetY` (Avalonia `M31` and `M32`)
- vectors are represented as `Point`, through `ScreenToContentVector(Point)` and `ContentToScreenVector(Point)`
- `PanAndZoom.MatrixHelper` wraps the shared `PanAndZoom.Core.MatrixMath`; `MatrixHelper.Multiply(a, b)` applies `a` first, like Avalonia
- `RenderMatrix` is the matrix currently applied to the child, which differs from `Matrix` while an animation is running

Both controls compute these values in the shared engine, so conversions are identical across frameworks. See [Architecture](architecture.md).
