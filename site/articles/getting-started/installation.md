---
title: "Installation"
---

# Installation

## PanAndZoom

Install the control package:

```bash
dotnet add package PanAndZoom
```

Or add a package reference:

```xml
<ItemGroup>
  <PackageReference Include="PanAndZoom" Version="x.y.z" />
</ItemGroup>
```

`ZoomBorder` is a control, not a separate theme package, so there is no required `StyleInclude` step beyond your normal Avalonia application setup.

Namespaces:

```csharp
using Avalonia.Controls.PanAndZoom; // ZoomBorder, MatrixHelper, ZoomBorderState, event args
using PanAndZoom;                   // StretchMode, ButtonName, ZoomChangedEventArgs, ... (PanAndZoom.Core)
```

XAML namespaces:

```xml
xmlns:paz="using:Avalonia.Controls.PanAndZoom"
xmlns:pz="using:PanAndZoom"
```

The `pz` namespace is only needed when XAML references the shared enum types directly (for example `<pz:DoubleClickZoomMode>ZoomIn</pz:DoubleClickZoomMode>`). Property values such as `Stretch="Uniform"` work without it.

`PanAndZoom` depends on `PanAndZoom.Core`, which NuGet restores automatically.

## PanAndZoom.Uno

Install the Uno Platform control package into an Uno Platform 6 app (single project, `Uno.Sdk`):

```bash
dotnet add package PanAndZoom.Uno
```

Or add a package reference:

```xml
<ItemGroup>
  <PackageReference Include="PanAndZoom.Uno" Version="x.y.z" />
</ItemGroup>
```

Requirements:

- `Uno.Sdk` 6.x with the Skia renderer (`<UnoFeatures>SkiaRenderer;</UnoFeatures>`). Libraries and apps must use the same renderer, and Skia is the default for new Uno 6 apps.
- One of the package targets: `net10.0`, `net10.0-desktop`, `net10.0-browserwasm`, `net10.0-android`, `net10.0-ios`, or `net10.0-windows10.0.26100`.

The default control style ships in the package (`Themes/Generic.xaml`) and is picked up automatically, so there is no resource dictionary to merge.

Namespace (the control and the shared enums share the same namespace):

```csharp
using PanAndZoom;
```

XAML namespace:

```xml
xmlns:paz="using:PanAndZoom"
```

## PanAndZoom.Core

`PanAndZoom.Core` (`net8.0`, `net10.0`) is referenced transitively by both controls. Install it directly only when you want to drive the engine yourself, for example to write an adapter for another UI framework or to unit test pan and zoom math without a UI:

```bash
dotnet add package PanAndZoom.Core
```

Namespaces:

```csharp
using PanAndZoom;      // shared enums and ZoomChanged event types
using PanAndZoom.Core; // PanAndZoomEngine, IPanAndZoomHost, IPanAndZoomSettings, Core* primitives
```

See [Architecture](../concepts/architecture.md) for how the engine, host, and settings fit together.

## Migrating From Earlier Versions

The Avalonia `ZoomBorder` API is unchanged, but these types moved from `Avalonia.Controls.PanAndZoom` to the `PanAndZoom` namespace in the `PanAndZoom.Core` assembly:

- `StretchMode`, `ButtonName`, `ContentBoundsMode`, `DoubleClickZoomMode`, `ResizeBehaviorMode`, `WheelBehaviorMode`, `ZoomIndicatorPosition`
- `ZoomChangedEventArgs`, `ZoomChangedEventHandler`

Update existing code as follows:

- C#: add `using PanAndZoom;` (or `global using PanAndZoom;`) where these types are referenced.
- XAML: add `xmlns:pz="using:PanAndZoom"` and use `pz:` for enum elements, as in `samples/AvaloniaDemo.Base/Views/DoubleClickZoomView.axaml`:

  ```xml
  <ComboBox SelectedItem="{Binding DoubleClickZoomMode}">
    <pz:DoubleClickZoomMode>ZoomIn</pz:DoubleClickZoomMode>
    <pz:DoubleClickZoomMode>ZoomOut</pz:DoubleClickZoomMode>
  </ComboBox>
  ```

- `ZoomBorderCommand` is now the public `PanAndZoom.Core.ZoomBorderCommand`.
- The new `ZoomBorder.Engine` property exposes the underlying `PanAndZoom.Core.PanAndZoomEngine`.

## HeadlessTestingFramework

Install the testing package:

```bash
dotnet add package HeadlessTestingFramework
```

For xUnit headless tests, you also need the standard Avalonia headless test packages used in this repository:

```xml
<ItemGroup>
  <PackageReference Include="Avalonia.Headless.XUnit" Version="12.1.1" />
  <PackageReference Include="Avalonia.Skia" Version="12.1.1" />
</ItemGroup>
```

Namespaces:

```csharp
using Avalonia.HeadlessTestingFramework;
using Avalonia.HeadlessTestingFramework.Appium;
using Avalonia.HeadlessTestingFramework.Recording;
```

## Sample Apps

The repository samples reference the packages directly from source:

- `samples/AvaloniaDemo.Base` and `samples/AvaloniaDemo.Desktop` (Avalonia)
- `samples/UnoDemo` (Uno Platform: desktop, WebAssembly, Android, iOS, Windows)

That is the quickest way to confirm local environment setup before consuming the NuGet packages in another solution.

## Related

- [Quickstart: ZoomBorder](quickstart-zoom-border.md)
- [Quickstart: Uno Platform](quickstart-uno.md)
- [Quickstart: Headless Testing](quickstart-headless-testing.md)
