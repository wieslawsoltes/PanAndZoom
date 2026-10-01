---
title: "Samples"
---

# Samples

The repository ships two sample applications with the same feature tabs:

| Sample | Framework | Platforms |
|---|---|---|
| `samples/AvaloniaDemo.Base` and `samples/AvaloniaDemo.Desktop` | Avalonia | Desktop |
| `samples/UnoDemo` | Uno Platform (Skia renderer) | Desktop (`net10.0-desktop`), WebAssembly (`net10.0-browserwasm`), Android (`net10.0-android`), iOS (`net10.0-ios`), Windows (`net10.0-windows10.0.26100`) |

Both expose the main control capabilities as tabs, which makes them the fastest way to validate behavior before writing application code. `UnoDemo` is a full port of the Avalonia demo, so the areas below apply to both.

## Run The Samples

```bash
# Avalonia
dotnet run --project samples/AvaloniaDemo.Desktop -c Release

# Uno Platform desktop
dotnet run --project samples/UnoDemo -f net10.0-desktop

# Uno Platform WebAssembly (needs the wasm-tools workload)
dotnet publish samples/UnoDemo -c Release -f net10.0-browserwasm -o artifacts/unodemo-wasm
```

Android and iOS builds need the `android` and `ios` workloads (`dotnet workload install android ios`); iOS apps are built on macOS with Xcode. See [Build and Package](build-and-package.md).

## PanAndZoom Sample Areas

- Commands: built-in `ICommand` bindings and common toolbar actions
- Coordinate Conversion: `ViewportToContent`, `ContentToViewport`, and matrix-derived helpers
- Bounds and Callbacks: visible bounds, viewport bounds, and content-bound callbacks
- Keyboard and Reset: keyboard shortcuts, reset behavior, and focus requirements
- Zoom-to-Rectangle: fitting or exactly matching content rectangles
- Inertia and Gestures: gesture recognizers (Avalonia) or manipulations (Uno Platform), pinch, and scroll interactions
- View History: back/forward navigation across saved viewport states
- Dynamic Zoom Limits: auto-calculated min and max zoom behavior
- Grid and Snap: grid parameters and `SnapToGrid(...)` helpers
- Rotation and Accessibility: rotation APIs, descriptions, and accessibility text
- Saved Views and State Serialization: named views plus `ExportState()` and `ImportState(...)`
- Resize Behavior and Gesture Fine Control: custom resize policies and gesture toggles

## Why The Sample Matters

- It shows the intended composition pattern for `ZoomBorder` in real Avalonia and WinUI XAML.
- It demonstrates feature combinations that are easy to miss by reading only API docs.
- It provides concrete scenarios that already map to unit tests in the repository.

## Related

- [Quickstart: ZoomBorder](getting-started/quickstart-zoom-border.md)
- [Quickstart: Uno Platform](getting-started/quickstart-uno.md)
- [Guides](guides/readme.md)
- [Advanced](advanced/readme.md)
