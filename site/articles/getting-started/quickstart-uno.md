---
title: "Quickstart: Uno Platform"
---

# Quickstart: Uno Platform

Create a zoomable container in an Uno Platform app by wrapping your content in `PanAndZoom.ZoomBorder` from the `PanAndZoom.Uno` package.

`PanAndZoom.Uno` targets `net10.0`, `net10.0-desktop`, `net10.0-browserwasm`, `net10.0-android`, `net10.0-ios`, and `net10.0-windows10.0.26100`, and is built with `Uno.Sdk` 6.7.30 and the Skia renderer. The pan and zoom logic is the same `PanAndZoom.Core.PanAndZoomEngine` used by the Avalonia control, so every feature described in this documentation is available on Uno Platform too.

## Install

```bash
dotnet add package PanAndZoom.Uno
```

Your app must use the Skia renderer (`<UnoFeatures>SkiaRenderer;</UnoFeatures>`, the Uno 6 default). The control style is included in the package, so no resources need to be merged.

## Minimal XAML

```xml
<Page x:Class="UnoDemo.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:paz="using:PanAndZoom">
  <paz:ZoomBorder x:Name="ZoomBorder"
                  Stretch="None"
                  ZoomSpeed="1.2"
                  PanButton="Left"
                  Background="SlateBlue"
                  ClipToBounds="True"
                  IsTabStop="True">
    <Canvas Background="LightGray" Width="300" Height="300">
      <Rectangle Canvas.Left="100" Canvas.Top="100" Width="50" Height="50" Fill="Red" />
    </Canvas>
  </paz:ZoomBorder>
</Page>
```

`ZoomBorder` is a templated `Control` whose content property is `Child`, so the child element can be written directly inside the tag.

## Common Code-Behind Hooks

```csharp
using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PanAndZoom;
using Windows.System;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();

        ZoomBorder.ZoomChanged += (_, e) =>
        {
            Debug.WriteLine($"Zoom: {e.ZoomX}, {e.ZoomY} Offset: {e.OffsetX}, {e.OffsetY}");
        };

        ZoomBorder.KeyDown += ZoomBorder_KeyDown;
    }

    private void ZoomBorder_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.F: ZoomBorder.Fill(); break;
            case VirtualKey.U: ZoomBorder.Uniform(); break;
            case VirtualKey.R: ZoomBorder.ResetMatrix(); break;
            case VirtualKey.T: ZoomBorder.ToggleStretchMode(); ZoomBorder.AutoFit(); break;
        }
    }
}
```

## Binding To Control State

WinUI has no `#Name` binding shorthand. Use `ElementName` bindings, or `x:Bind` from the page:

```xml
<StackPanel Orientation="Horizontal" Spacing="4">
  <Slider Minimum="1.05" Maximum="3" StepFrequency="0.05" Width="120"
          Value="{Binding ZoomSpeed, ElementName=ZoomBorder, Mode=TwoWay}" />
  <CheckBox Content="EnablePan"
            IsChecked="{Binding EnablePan, ElementName=ZoomBorder, Mode=TwoWay}" />
  <TextBlock Text="{Binding ZoomX, ElementName=ZoomBorder}" />
</StackPanel>
```

`ZoomBorder.StretchModes` and `ZoomBorder.ButtonNames` are static arrays that can be assigned to `ComboBox.ItemsSource` in code-behind.

## MVVM Command Binding

The same `ICommand` properties as on Avalonia are available:

```xml
<StackPanel Orientation="Horizontal" Spacing="4">
  <Button Content="Zoom In" Command="{Binding ZoomInCommand, ElementName=ZoomBorder}" />
  <Button Content="Reset" Command="{Binding ResetCommand, ElementName=ZoomBorder}" />
  <Button Content="Fit" Command="{Binding FitCommand, ElementName=ZoomBorder}" />
  <Button Content="Back" Command="{Binding NavigateBackCommand, ElementName=ZoomBorder}" />
</StackPanel>
```

## Input On Uno Platform

- Mouse: drag with `PanButton` to pan (the pointer is captured while panning), wheel to zoom, `Ctrl` + wheel and `Shift` + wheel use `WheelWithCtrl` and `WheelWithShift`, double tap uses `DoubleClickZoomMode`.
- Keyboard: arrows, `+`, `-`, `Ctrl` + `0`, `Home`, and `Ctrl` + `Left` / `Right` work while the control has focus. `IsTabStop` is `true` by default and the control focuses itself on pointer press.
- Touch: WinUI manipulations, enabled by `EnableGestures`. One finger pans and two fingers pinch zoom and rotate, gated by `EnableGestureTranslation`, `EnableGestureZoom`, and `EnableGestureRotation`. Mouse and pen input are handled by the pointer path, not by manipulations.
- Visual states: the template can react to the `PanningStates` group (`Panning` / `NotPanning`), and the read-only `IsPanning` property reports the same state.

## Differences From The Avalonia Control

| Area | Avalonia | Uno Platform |
|---|---|---|
| Namespace | `Avalonia.Controls.PanAndZoom` (+ `PanAndZoom` for shared enums) | `PanAndZoom` (control and shared enums) |
| XAML namespace | `xmlns:paz="using:Avalonia.Controls.PanAndZoom"` | `xmlns:paz="using:PanAndZoom"` |
| Base type | `Border` with `Child` | Templated `Control` with `[ContentProperty]` `Child` and template part `PART_Border` |
| Clipping | `ClipToBounds` | `ClipToBounds` dependency property, default `true` |
| Focus | `Focusable="True"` | `IsTabStop="True"` (default) |
| Geometry | `Avalonia.Point`, `Rect`, `Size`, `Vector`, `Matrix` (`M31` / `M32`) | `Windows.Foundation.Point`, `Rect`, `Size`, `Microsoft.UI.Xaml.Media.Matrix` (`OffsetX` / `OffsetY`) |
| Vector conversion | `ScreenToContent(Vector)`, `ContentToScreen(Vector)` | `ScreenToContentVector(Point)`, `ContentToScreenVector(Point)` |
| Rotation | `Rotation` | `Rotation` hides `UIElement.Rotation`; rotation is applied through the render matrix |
| Read-only state | `ZoomX`, `ZoomY`, `OffsetX`, `OffsetY`, `IsZoomIndicatorVisible`, `:isPanning` | Read-only dependency properties `ZoomX`, `ZoomY`, `OffsetX`, `OffsetY`, `IsZoomIndicatorVisible`, `IsPanning` |
| Animation | Avalonia transitions | The control runs a `PanAndZoom.Core.MatrixTransition` on `CompositionTarget.Rendering`; `IsAnimating` and `RenderMatrix` expose its state |
| Scrolling | `ILogicalScrollable` | `Extent`, `Viewport`, `ScrollOffset`, `CanHorizontallyScroll`, `CanVerticallyScroll`, `ScrollInvalidated`, `BringIntoView(FrameworkElement, Rect)`; `BringIntoViewRequested` from descendants is handled by panning |
| Touch | Pinch and scroll gesture recognizers | WinUI manipulations |
| Matrix helpers | `Avalonia.Controls.PanAndZoom.MatrixHelper` | `PanAndZoom.MatrixHelper` (`Multiply(a, b)` applies `a` first) |

The virtual extension points (`GetContentBounds()`, `ValidateTransform(Matrix)`, `OnResized(Size, Size)`, `CalculateAutoZoomLimits()`, `GetZoomIndicatorPosition()`, and the `Raise*` event methods) are available on both controls with the native geometry types.

## Run The Sample

`samples/UnoDemo` ports the Avalonia demo to Uno Platform:

```bash
dotnet run --project samples/UnoDemo -f net10.0-desktop
```

## Next Steps

- [Architecture](../concepts/architecture.md)
- [Namespace PanAndZoom (Uno Platform)](../reference/namespace-panandzoom-uno.md)
- [Build and Package](../build-and-package.md) for workloads and Uno runtime tests
- [Commands and Keyboard](../guides/commands-and-keyboard.md)
