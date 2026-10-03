---
title: "Quickstart: ZoomBorder"
---

# Quickstart: ZoomBorder

Create a zoomable container by wrapping your content in `Avalonia.Controls.PanAndZoom.ZoomBorder`.

## Minimal XAML

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:paz="using:Avalonia.Controls.PanAndZoom">
  <paz:ZoomBorder x:Name="ZoomBorder"
                  Stretch="None"
                  ZoomSpeed="1.2"
                  Background="SlateBlue"
                  ClipToBounds="True"
                  Focusable="True">
    <Canvas Background="LightGray" Width="300" Height="300">
      <Rectangle Canvas.Left="100" Canvas.Top="100" Width="50" Height="50" Fill="Red" />
    </Canvas>
  </paz:ZoomBorder>
</Window>
```

## Common Code-Behind Hooks

`ZoomChangedEventArgs` and the other shared model types live in the `PanAndZoom` namespace (`PanAndZoom.Core` assembly), so add `using PanAndZoom;` next to `using Avalonia.Controls.PanAndZoom;`.

```csharp
ZoomBorder.ZoomChanged += (_, e) =>
{
    Debug.WriteLine($"Zoom: {e.ZoomX}, {e.ZoomY} Offset: {e.OffsetX}, {e.OffsetY}");
};

ZoomBorder.Fill();
ZoomBorder.Uniform();
ZoomBorder.ResetMatrix();
```

## Recommended First Properties

- `PanButton`: choose which mouse button starts panning
- `EnablePan` and `EnableZoom`: feature gates for interaction
- `EnableGestures`: attach pinch and scroll recognizers
- `Stretch`: initial fit policy
- `BoundsMode`: how content should be constrained when panning

## MVVM Command Binding

`ZoomBorder` exposes built-in commands that can be bound directly in XAML:

```xml
<StackPanel>
  <Button Content="Zoom In" Command="{Binding #ZoomBorder.ZoomInCommand}" />
  <Button Content="Reset" Command="{Binding #ZoomBorder.ResetCommand}" />
  <Button Content="Fit" Command="{Binding #ZoomBorder.FitCommand}" />
</StackPanel>
```

## Next Steps

- [Transform and Coordinate Spaces](../concepts/transform-and-coordinate-spaces.md)
- [Quickstart: Uno Platform](quickstart-uno.md)
- [Commands and Keyboard](../guides/commands-and-keyboard.md)
- [Bounds, Wheel, and Resize](../guides/bounds-wheel-and-resize.md)
