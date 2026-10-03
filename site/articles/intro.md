---
title: "Introduction"
---

# Introduction

`PanAndZoom` is a pan-and-zoom repository for Avalonia and Uno Platform with four complementary libraries:

- `PanAndZoom`: the Avalonia `ZoomBorder` control, matrix helpers, commands, bounds logic, and view-state APIs for pan-and-zoom experiences.
- `PanAndZoom.Uno`: the Uno Platform (WinUI) `ZoomBorder` control with the same feature set for desktop, WebAssembly, Android, iOS, and Windows apps.
- `PanAndZoom.Core`: the UI framework independent `PanAndZoom.Core.PanAndZoomEngine` shared by both controls, plus the shared enums and event types.
- `HeadlessTestingFramework`: input simulation, tree inspection, recording, and Appium-style helpers for validating Avalonia controls in automated tests.

Together they cover both sides of interactive UI work:

- build zoomable, pannable canvases, diagram surfaces, and image viewers on Avalonia or Uno Platform
- persist view state, expose keyboard shortcuts, and constrain viewport behavior
- test touch, wheel, keyboard, and gesture scenarios without a full desktop session
- capture frames or turn headless recordings into video artifacts for diagnostics

## Start With

- [Getting Started Overview](getting-started/overview.md) if you need a package-level entry point
- [Quickstart: ZoomBorder](getting-started/quickstart-zoom-border.md) to wire the control into an Avalonia view
- [Quickstart: Uno Platform](getting-started/quickstart-uno.md) to wire the control into an Uno Platform page
- [Architecture](concepts/architecture.md) to understand the shared engine and the two thin adapters
- [Quickstart: Headless Testing](getting-started/quickstart-headless-testing.md) to drive gestures in `AvaloniaFact` tests
- [API Documentation](../api/index.md) for member-level lookup

## Repository Layout

- `src/PanAndZoom.Core`: shipping UI framework independent engine package
- `src/PanAndZoom`: shipping Avalonia control package
- `src/PanAndZoom.Uno`: shipping Uno Platform control package
- `src/HeadlessTestingFramework`: shipping testing package
- `samples/AvaloniaDemo.Base` and `samples/AvaloniaDemo.Desktop`: interactive Avalonia sample app
- `samples/UnoDemo`: interactive Uno Platform sample app (desktop, WebAssembly, Android, iOS, Windows)
- `tests/Avalonia.Controls.PanAndZoom.UnitTests`: broad control and testing coverage
- `tests/PanAndZoom.Core.UnitTests`: engine unit tests without a UI framework
- `tests/PanAndZoom.Uno.RuntimeTests`: Uno runtime tests running inside a real Uno Skia desktop app
- `tests/HeadlessTestingFramework.UnitTests`: focused framework-level tests
- `PanAndZoom.slnx`: Avalonia, core, and testing framework solution
- `PanAndZoom.Uno.slnx`: core, Uno control, Uno sample, and Uno runtime tests solution

## Migrating From Earlier Versions

The shared model types moved to the `PanAndZoom.Core` assembly and the `PanAndZoom` namespace. Avalonia code that references `StretchMode`, `ButtonName`, `ContentBoundsMode`, `DoubleClickZoomMode`, `ResizeBehaviorMode`, `WheelBehaviorMode`, `ZoomIndicatorPosition`, `ZoomChangedEventArgs`, or `ZoomChangedEventHandler` directly needs `using PanAndZoom;` in C# and `xmlns:pz="using:PanAndZoom"` in XAML when the enum types are used as elements. See [Installation](getting-started/installation.md) for details.
