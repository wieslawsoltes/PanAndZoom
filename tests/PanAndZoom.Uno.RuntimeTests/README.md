# PanAndZoom.Uno.RuntimeTests

Runtime tests for the Uno Platform `ZoomBorder` (`src/PanAndZoom.Uno`). The tests run inside a real
Uno Skia desktop application using [Uno.UI.RuntimeTests.Engine](https://github.com/unoplatform/uno.ui.runtimetests.engine),
so layout, templates, pointer capture, manipulations and render transforms are exercised for real.

## Running

```bash
# All tests (headless on Linux through xvfb-run, a window briefly opens on macOS/Windows)
build/run-uno-runtime-tests.sh

# Debug configuration and a filter (class or method name fragments, '|' separated)
build/run-uno-runtime-tests.sh Debug "ZoomBorderConstraintTests|ZoomBorderTests"
```

The script writes NUnit XML results to `artifacts/test-results/uno-runtime-tests.xml` and exits with a
non-zero code when a test fails. Running the app without the `UNO_RUNTIME_TESTS_*` environment variables
shows the interactive test runner UI.

## Writing tests

Tests use MSTest attributes and always run on the UI thread:

```csharp
[TestClass]
[RunsOnUIThread]
public class ZoomBorderSomethingTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public async Task Zoom_Works()
    {
        var (zoomBorder, child) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 200, 200,
            zb => zb.Stretch = StretchMode.None);

        zoomBorder.ZoomTo(2.0, 100, 100);

        Assert.AreEqual(2.0, zoomBorder.ZoomX, 1e-9);
    }
}
```

### Helpers (`Infrastructure/`)

| Helper | Purpose |
| --- | --- |
| `ZoomBorderTestHelper.Create/CreateAndLoadAsync` | Creates a fixed size `ZoomBorder` (top/left aligned) with a fixed size `Border` child and loads it. |
| `ZoomBorderTestHelper.LoadAsync(element)` / `UnloadAsync()` | Loads any element into the test window / clears it. |
| `ZoomBorderTestHelper.WaitForIdleAsync()` / `WaitForAsync(condition)` | Waits for layout idle or a condition. |
| `ZoomBorderTestHelper.ToWindow(element, point)` | Converts element coordinates to window coordinates for input injection. |
| `ZoomBorderTestHelper.GetChildRenderMatrix(zb)` | Matrix of the render transform applied to the child. |
| `ZoomBorderTestHelper.PressKey(zb, VirtualKey, modifiers)` | Routes a key through the control keyboard handler (Uno cannot inject keys on Skia). |
| `ZoomBorderTestHelper.Wheel(zb, delta, pointInChild, modifiers)` | Routes a wheel change through the control wheel handler. |
| `InputHelper.MouseDrag/MouseDown/MouseUp/MouseMoveTo/MouseClick/MouseDoubleClick` | Real injected mouse input (window coordinates). |
| `InputHelper.MouseWheel(position, delta[, VirtualKeyModifiers])` | Real injected wheel input (120 per notch), optionally with modifiers. |
| `InputHelper.TouchDrag/TwoFingerGesture/Pinch` | Real injected touch input (manipulations). |

### Porting Avalonia tests

The Avalonia tests (`tests/Avalonia.Controls.PanAndZoom.UnitTests`) are the reference. When porting:

* `[AvaloniaFact]` becomes `[TestMethod]` (async `Task` when the control is loaded); `[AvaloniaTheory]` + `[InlineData]` becomes `[TestMethod]` + `[DataRow]`.
* `new Window { Content = ... }; window.Show();` becomes `await ZoomBorderTestHelper.LoadAsync(...)`.
* xUnit `Assert.Equal(expected, actual, precision)` (decimal places) becomes `Assert.AreEqual(expected, actual, delta)`.
* Avalonia `Matrix.M31/M32` are `Matrix.OffsetX/OffsetY`; `a * b` is `MatrixHelper.Multiply(a, b)`.
* Avalonia `Vector` arguments are WinUI `Point`s (`ScreenToContentVector`, `ContentToScreenVector`, `ScrollOffset`).
* WinUI `Rect` cannot have a negative size; `Rect.Empty` is not `default(Rect)`.
* The `:isPanning` pseudo class is the `IsPanning` property.
* `ILogicalScrollable` members are `Extent`, `Viewport`, `ScrollOffset`, `ScrollInvalidated` and `BringIntoView(FrameworkElement, Rect)`.
* Avalonia transitions are replaced by `IsAnimating` and `RenderMatrix` (the render transform is animated by the control).
* `ClipToBounds` defaults to `true` on Uno.
* Avalonia gesture recognizers (pinch, scroll) are WinUI manipulations on Uno: one finger pans, two fingers pinch and rotate.
* When the Uno behavior legitimately differs (layout rounding, WinUI APIs), adapt the expectation and leave a short comment explaining why.
