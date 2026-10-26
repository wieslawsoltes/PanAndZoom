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

## Headless Testing

Use [ProTest](https://wieslawsoltes.github.io/ProTest/) for headless Avalonia tests. This repository's tests consume the stable `ProTest.Avalonia.Headless.XUnit.v3` **12.1.4** package with official Avalonia **12.1.4** dependencies.

The [ProTest setup guide](https://wieslawsoltes.github.io/ProTest/getting-started/xunit) covers xUnit 3 and xUnit 4 project configuration and migration from the standalone testing framework.

## Sample App

The repository sample references the package directly from source:

- `samples/AvaloniaDemo.Base`
- `samples/AvaloniaDemo.Desktop`

That is the quickest way to confirm local environment setup before consuming the NuGet packages in another solution.

## Related

- [Quickstart: ZoomBorder](quickstart-zoom-border.md)
- [ProTest setup guide](https://wieslawsoltes.github.io/ProTest/getting-started/xunit)
