---
title: "Introduction"
---

# Introduction

`PanAndZoom` provides the `ZoomBorder` control, matrix helpers, commands, bounds logic, and view-state APIs for Avalonia pan-and-zoom experiences.

Use it to:

- build zoomable, pannable canvases, diagram surfaces, and image viewers
- persist view state and expose keyboard shortcuts
- constrain viewport behavior and support pointer, wheel, and touch gestures

## Start With

- [Getting Started Overview](getting-started/overview.md) for the control's capabilities
- [Quickstart: ZoomBorder](getting-started/quickstart-zoom-border.md) to wire the control into an Avalonia view
- [API Documentation](../api/index.md) for member-level lookup

For automated UI testing, use the separately maintained [ProTest packages and documentation](https://wieslawsoltes.github.io/ProTest/).

## Repository Layout

- `src/PanAndZoom`: shipping control package
- `samples/AvaloniaDemo.Base` and `samples/AvaloniaDemo.Desktop`: interactive sample app
- `tests/Avalonia.Controls.PanAndZoom.UnitTests`: control tests using ProTest from NuGet
