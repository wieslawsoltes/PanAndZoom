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

Namespace:

```csharp
using Avalonia.Controls.PanAndZoom;
```

XAML namespace:

```xml
xmlns:paz="using:Avalonia.Controls.PanAndZoom"
```

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

## Sample App

The repository sample references the package directly from source:

- `samples/AvaloniaDemo.Base`
- `samples/AvaloniaDemo.Desktop`

That is the quickest way to confirm local environment setup before consuming the NuGet packages in another solution.

## Related

- [Quickstart: ZoomBorder](quickstart-zoom-border.md)
- [Quickstart: Headless Testing](quickstart-headless-testing.md)
