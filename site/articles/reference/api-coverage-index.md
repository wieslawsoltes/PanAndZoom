---
title: "API Coverage Index"
---

# API Coverage Index

This index maps the public surface to the primary narrative entry points in this Lunet site.

## Coverage Summary

- packages documented: `1`
- primary namespaces covered in reference pages: `1`
- generated API pages: `PanAndZoom`

## Namespace Entry Points

- [Namespace: Avalonia.Controls.PanAndZoom](namespace-panandzoom.md)

## Focus Area Mapping

| Focus area | Primary article | Representative API |
|---|---|---|
| Zoomable viewport basics | [getting-started/quickstart-zoom-border.md](../getting-started/quickstart-zoom-border.md) | `Avalonia.Controls.PanAndZoom.ZoomBorder` |
| Coordinate conversion | [concepts/transform-and-coordinate-spaces.md](../concepts/transform-and-coordinate-spaces.md) | `ViewportToContent(...)`, `ContentToViewport(...)`, `Avalonia.Controls.PanAndZoom.MatrixHelper` |
| Commands and keyboard | [guides/commands-and-keyboard.md](../guides/commands-and-keyboard.md) | `ZoomInCommand`, `ResetCommand`, `NavigateBackCommand` |
| Bounds and resize | [guides/bounds-wheel-and-resize.md](../guides/bounds-wheel-and-resize.md) | `BoundsMode`, `ResizeBehavior`, `AutoCalculateMinZoom` |
| View persistence | [concepts/view-state-and-persistence.md](../concepts/view-state-and-persistence.md) | `SaveView(...)`, `ExportState()`, `Avalonia.Controls.PanAndZoom.ZoomBorderState` |

## Notes

- Generated API pages remain the authoritative member-level reference.
- These reference pages are intended to accelerate discovery, not duplicate every member signature from the API site.
