---
title: "Diagnostics and Testing"
---

# Diagnostics and Testing

This repository is already test-heavy, which makes the codebase a good reference for both usage and expected behavior.

## PanAndZoom Test Coverage Areas

The main test project covers:

- pointer, wheel, keyboard, and gesture interaction
- bounds modes and dynamic zoom limits
- view history, saved views, and state serialization
- rotation, scale indicator, viewport culling, and `ILogicalScrollable`
- Appium-style APIs and recording workflows

## Core Engine Tests

`tests/PanAndZoom.Core.UnitTests` (xunit v3) exercises `PanAndZoom.Core.PanAndZoomEngine` directly, without a UI framework:

```bash
dotnet test tests/PanAndZoom.Core.UnitTests -c Release
```

## Uno Platform Runtime Tests

`tests/PanAndZoom.Uno.RuntimeTests` runs inside a real Uno Skia desktop app using Uno.UI.RuntimeTests.Engine, so layout, templates, pointer capture, manipulations, and render transforms are exercised for real:

```bash
build/run-uno-runtime-tests.sh
build/run-uno-runtime-tests.sh Debug "ZoomBorderTests"
```

Results are written to `artifacts/test-results/uno-runtime-tests.xml`. On Linux without a display the script uses `xvfb-run`. The project README covers the helpers (`ZoomBorderTestHelper`, `InputHelper`) and how to port Avalonia tests.

## HeadlessTestingFramework Test Coverage Areas

The second test project focuses on:

- touch simulation and gesture recognizer helpers
- tree validation, tree comparison, and XPath helpers
- recording infrastructure and multi-touch helper construction

## Useful Debug Artifacts

- generated Lunet API docs under `site/.lunet/build/www/api`
- recording output from `HeadlessScreenRecorder` or `RecordingSession`
- tree and template comparison summaries from `TreeComparer` and `TemplateComparer`

## Documentation Validation

Validate docs locally with:

```bash
./check-docs.sh
```

That rebuilds the Lunet site and API reference from the same sources used by CI.
