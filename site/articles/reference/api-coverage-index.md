---
title: "API Coverage Index"
---

# API Coverage Index

This index maps the public surface to the primary narrative entry points in this Lunet site.

## Coverage Summary

- packages documented: `4` (`PanAndZoom`, `PanAndZoom.Uno`, `PanAndZoom.Core`, `HeadlessTestingFramework`)
- primary namespaces covered in reference pages: `6`
- generated API pages: `PanAndZoom.Core`, `PanAndZoom`, and `HeadlessTestingFramework` (`PanAndZoom.Uno` targets only `net10.0` platform frameworks and is documented in narrative reference pages)

## Namespace Entry Points

- [Namespace: Avalonia.Controls.PanAndZoom](namespace-panandzoom.md)
- [Namespaces: PanAndZoom.Core and PanAndZoom](namespace-panandzoom-core.md)
- [Namespace: PanAndZoom (Uno Platform)](namespace-panandzoom-uno.md)
- [Namespace: Avalonia.HeadlessTestingFramework](namespace-headless-testingframework.md)
- [Namespaces: Recording and Appium](namespace-recording-and-appium.md)

## Focus Area Mapping

| Focus area | Primary article | Representative API |
|---|---|---|
| Shared engine | [concepts/architecture.md](../concepts/architecture.md) | `PanAndZoom.Core.PanAndZoomEngine`, `PanAndZoom.Core.IPanAndZoomHost`, `PanAndZoom.Core.IPanAndZoomSettings` |
| Uno Platform viewport | [getting-started/quickstart-uno.md](../getting-started/quickstart-uno.md) | `PanAndZoom.ZoomBorder` (`PanAndZoom.Uno`) |
| Zoomable viewport basics | [getting-started/quickstart-zoom-border.md](../getting-started/quickstart-zoom-border.md) | `Avalonia.Controls.PanAndZoom.ZoomBorder` |
| Coordinate conversion | [concepts/transform-and-coordinate-spaces.md](../concepts/transform-and-coordinate-spaces.md) | `ViewportToContent(...)`, `ContentToViewport(...)`, `Avalonia.Controls.PanAndZoom.MatrixHelper` |
| Commands and keyboard | [guides/commands-and-keyboard.md](../guides/commands-and-keyboard.md) | `ZoomInCommand`, `ResetCommand`, `NavigateBackCommand` |
| Bounds and resize | [guides/bounds-wheel-and-resize.md](../guides/bounds-wheel-and-resize.md) | `BoundsMode`, `ResizeBehavior`, `AutoCalculateMinZoom` |
| View persistence | [concepts/view-state-and-persistence.md](../concepts/view-state-and-persistence.md) | `SaveView(...)`, `ExportState()`, `Avalonia.Controls.PanAndZoom.ZoomBorderState` |
| Touch and gesture simulation | [headless-testing/input-simulators.md](../headless-testing/input-simulators.md) | `Avalonia.HeadlessTestingFramework.TouchInputSimulator`, `Avalonia.HeadlessTestingFramework.GestureSimulator` |
| Tree queries and assertions | [headless-testing/tree-helpers.md](../headless-testing/tree-helpers.md) | `Avalonia.HeadlessTestingFramework.ControlFinder`, `Avalonia.HeadlessTestingFramework.TreeValidator` |
| Appium-style automation | [headless-testing/appium-like-api.md](../headless-testing/appium-like-api.md) | `Avalonia.HeadlessTestingFramework.Appium.AvaloniaDriver`, `Avalonia.HeadlessTestingFramework.Appium.By` |
| Recording and video conversion | [headless-testing/recording-and-video.md](../headless-testing/recording-and-video.md) | `Avalonia.HeadlessTestingFramework.Recording.HeadlessScreenRecorder`, `Avalonia.HeadlessTestingFramework.Recording.VideoConverter` |

## Notes

- Generated API pages remain the authoritative member-level reference.
- These reference pages are intended to accelerate discovery, not duplicate every member signature from the API site.
