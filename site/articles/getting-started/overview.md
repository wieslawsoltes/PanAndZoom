---
title: "Overview"
---

# Overview

`PanAndZoom` is an interactive viewport control with zoom, pan, history, bounds, resize, rotation, and state persistence support.

Start with [Quickstart: ZoomBorder](quickstart-zoom-border.md) to build an interactive canvas, editor, or viewer.

## What `ZoomBorder` Adds

`Avalonia.Controls.PanAndZoom.ZoomBorder` wraps a single child and manages a transform matrix over that content. Common capabilities include:

- mouse-wheel zoom and pointer-based pan
- pinch and scroll gestures
- bounds policies such as `KeepContentVisible` and `FillViewport`
- view history, saved views, and import/export of serialized state
- keyboard navigation and `ICommand` bindings for MVVM surfaces

## Automated Testing

Headless testing helpers are maintained in [ProTest](https://wieslawsoltes.github.io/ProTest/). Use its xUnit 3 or xUnit 4 adapter to test Avalonia interaction without a desktop session. The [ProTest setup guide](https://wieslawsoltes.github.io/ProTest/getting-started/xunit) explains the matching package and framework versions.

## Related

- [Installation](installation.md)
- [Concepts](../concepts/readme.md)
- [Diagnostics and Testing](../advanced/diagnostics-and-testing.md)
