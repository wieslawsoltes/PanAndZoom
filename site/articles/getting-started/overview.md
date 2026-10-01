---
title: "Overview"
---

# Overview

This repository exposes four packages with different responsibilities:

| Package | Use it when you need |
|---|---|
| `PanAndZoom` | An interactive Avalonia viewport control with zoom, pan, history, bounds, resize, rotation, and state persistence support |
| `PanAndZoom.Uno` | The same viewport control for Uno Platform (WinUI) apps on desktop, WebAssembly, Android, iOS, and Windows |
| `PanAndZoom.Core` | The UI framework independent engine shared by both controls (referenced automatically; use it directly to build your own adapter) |
| `HeadlessTestingFramework` | Automated Avalonia UI tests with touch, wheel, keyboard, tree inspection, screenshots, recordings, and Appium-like interaction APIs |

## Choose The Right Starting Point

- Start with [Quickstart: ZoomBorder](quickstart-zoom-border.md) if you are building an interactive canvas, editor, or viewer with Avalonia.
- Start with [Quickstart: Uno Platform](quickstart-uno.md) if your app is built with Uno Platform.
- Start with [Quickstart: Headless Testing](quickstart-headless-testing.md) if you already have Avalonia controls and need deterministic test coverage.
- Use both when you want to ship zoom-heavy interaction and verify it with headless gesture tests in CI.

## What `ZoomBorder` Adds

`Avalonia.Controls.PanAndZoom.ZoomBorder` (Avalonia) and `PanAndZoom.ZoomBorder` (Uno Platform) wrap a single child and manage a transform matrix over that content. Both are thin adapters over `PanAndZoom.Core.PanAndZoomEngine`, so behavior is identical across frameworks (see [Architecture](../concepts/architecture.md)). Common capabilities include:

- mouse-wheel zoom and pointer-based pan
- pinch and scroll gestures
- bounds policies such as `KeepContentVisible` and `FillViewport`
- view history, saved views, and import/export of serialized state
- keyboard navigation and `ICommand` bindings for MVVM surfaces

## What HeadlessTestingFramework Adds

The testing package provides several layers:

- low-level input simulators: `Avalonia.HeadlessTestingFramework.TouchInputSimulator`, `Avalonia.HeadlessTestingFramework.KeyboardInputSimulator`, `Avalonia.HeadlessTestingFramework.MouseInputSimulator`
- high-level gesture and helper APIs: `Avalonia.HeadlessTestingFramework.GestureSimulator`, `Avalonia.HeadlessTestingFramework.MultiTouchTestHelperFactory`
- inspection and assertion helpers: `Avalonia.HeadlessTestingFramework.ControlFinder`, `Avalonia.HeadlessTestingFramework.TreeXPath`, `Avalonia.HeadlessTestingFramework.TreeValidator`
- automation and recording layers: `Avalonia.HeadlessTestingFramework.Appium.AvaloniaDriver`, `Avalonia.HeadlessTestingFramework.Recording.HeadlessScreenRecorder`

## Related

- [Installation](installation.md)
- [Architecture](../concepts/architecture.md)
- [Concepts](../concepts/readme.md)
- [Headless Testing](../headless-testing/readme.md)
