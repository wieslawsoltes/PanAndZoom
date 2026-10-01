---
title: "Build and Package"
---

# Build and Package

The repository uses the .NET 10 SDK (`global.json`, which also pins `Uno.Sdk` 6.7.30) and two solutions:

| Solution | Contents |
|---|---|
| `PanAndZoom.slnx` | `PanAndZoom.Core`, `PanAndZoom` (Avalonia), `HeadlessTestingFramework`, the Avalonia sample, and the Avalonia and core unit tests |
| `PanAndZoom.Uno.slnx` | `PanAndZoom.Core`, `PanAndZoom.Uno`, the Uno sample (`samples/UnoDemo`), and the Uno runtime tests |

## Avalonia And Core

Build, test, and pack from repository root:

```bash
dotnet restore PanAndZoom.slnx
dotnet build PanAndZoom.slnx -c Release --no-restore
dotnet test PanAndZoom.slnx -c Release --no-build
dotnet pack PanAndZoom.slnx -c Release --no-build -o artifacts/packages
```

Packages (`PanAndZoom.Core`, `PanAndZoom`, `HeadlessTestingFramework`) are written to `artifacts/packages` as `.nupkg` and `.snupkg` files.

Run only the engine unit tests (xunit v3, no UI framework):

```bash
dotnet test tests/PanAndZoom.Core.UnitTests -c Release
```

## Uno Platform

`PanAndZoom.Uno` targets `net10.0`, `net10.0-desktop`, `net10.0-browserwasm`, `net10.0-android`, `net10.0-ios`, and, when building on Windows, `net10.0-windows10.0.26100`. Building all targets requires the .NET workloads:

```bash
dotnet workload install android ios wasm-tools
```

- `android`: `net10.0-android` (also needs an Android SDK and a Java JDK)
- `ios`: `net10.0-ios` (iOS apps are built on macOS with Xcode)
- `wasm-tools`: `net10.0-browserwasm`

Without workloads, restrict the library targets with the `PanAndZoomTargetFrameworks` property:

```bash
# Desktop only, no workloads required
dotnet build src/PanAndZoom.Uno -c Release -p:PanAndZoomTargetFrameworks=net10.0-desktop

# Several targets (escape ';' as %3B)
dotnet build src/PanAndZoom.Uno -c Release "-p:PanAndZoomTargetFrameworks=net10.0%3Bnet10.0-desktop%3Bnet10.0-browserwasm"
```

Full solution build and package (workloads installed):

```bash
dotnet build PanAndZoom.Uno.slnx -c Release
dotnet pack src/PanAndZoom.Uno -c Release -o artifacts/packages
```

### Uno Runtime Tests

`tests/PanAndZoom.Uno.RuntimeTests` uses Uno.UI.RuntimeTests.Engine to run MSTest-style tests inside a real Uno Skia desktop app (`net10.0-desktop`):

```bash
# All tests (headless through xvfb-run on Linux, a window briefly opens on macOS and Windows)
build/run-uno-runtime-tests.sh

# Debug configuration with a filter (test class or method name fragments, '|' separated)
build/run-uno-runtime-tests.sh Debug "ZoomBorderConstraintTests | ZoomBorderTests"
```

The script builds the test app, runs it with `UNO_RUNTIME_TESTS_RUN_TESTS` and `UNO_RUNTIME_TESTS_OUTPUT_PATH`, writes NUnit XML results to `artifacts/test-results/uno-runtime-tests.xml`, and exits with a non-zero code when a test fails. Running the app without those environment variables shows the interactive runtime test UI. On Linux CI it needs `xvfb` and the X11/OpenGL libraries.

## Build The Sample Apps

```bash
dotnet build samples/AvaloniaDemo.Desktop/AvaloniaDemo.Desktop.csproj -c Release
dotnet build samples/UnoDemo -c Release -f net10.0-desktop
dotnet publish samples/UnoDemo -c Release -f net10.0-browserwasm -o artifacts/unodemo-wasm
```

## Build Documentation

```bash
./build-docs.sh
./check-docs.sh
./serve-docs.sh
```

PowerShell:

```powershell
./build-docs.ps1
./serve-docs.ps1
```

Documentation output is written to `site/.lunet/build/www`. The API reference is generated from the `net8.0` targets of `PanAndZoom.Core`, `PanAndZoom`, and `HeadlessTestingFramework`.

## CI

- `build.yml` runs on pull requests and pushes to `master`/`main`/`release/**` (superseded runs are cancelled) with three jobs: `Test` on Linux, Windows, and macOS (builds and tests `PanAndZoom.slnx`, runs the Uno runtime tests, and on macOS builds the Uno library for iOS and Android), `Uno sample` on Linux (sample self-test and WebAssembly publish), and `Pack` on Windows (all NuGet packages; the WinAppSDK target of `PanAndZoom.Uno` needs msbuild)
- `docs.yml` builds the Lunet site and publishes it to GitHub Pages
- `release.yml` runs on `v*` tags (or manual dispatch): tests `PanAndZoom.slnx` and the Uno runtime tests, packs every package on Windows, and publishes them to NuGet
