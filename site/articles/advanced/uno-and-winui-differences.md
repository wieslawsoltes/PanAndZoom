---
title: "Uno Platform and WinUI 3 Differences"
---

# Uno Platform and WinUI 3 Differences

`PanAndZoom.WinUI` is the native WinUI 3 (Windows App SDK) build of the Uno Platform `ZoomBorder`. It has no sources of its own: `src/PanAndZoom.WinUI`, `samples/WinUIDemo` and `tests/PanAndZoom.WinUI.RuntimeTests` link the sources of `src/PanAndZoom.Uno`, `samples/UnoDemo` and `tests/PanAndZoom.Uno.RuntimeTests`. Uno implements the WinUI API surface, so the same code compiles for both. This page tracks where the two platforms behave differently, how the shared code deals with it, and the issues that are still open.

The sample and the tests define the `PANANDZOOM_WINUI` compilation symbol in their WinUI projects for the few places that need platform specific code. The library itself has no platform specific code.

## Which Package To Use

| App | Package |
|---|---|
| Uno Platform app (any target, including its Windows App SDK head) | `PanAndZoom.Uno` |
| Plain WinUI 3 / Windows App SDK app (no Uno) | `PanAndZoom.WinUI` |

## Control Differences

| Area | Uno Platform | WinUI 3 | How `ZoomBorder` handles it |
|---|---|---|---|
| Object identity | Property getters return the managed instance that was set. | Getters of projected WinRT objects are not guaranteed to return the same managed wrapper, so reference equality is unreliable. | The render transform is assigned on every update instead of being compared, and detaching a child clears the values the control owns instead of comparing them. |
| Cleared `RenderTransform` | Returns `null`. | Returns a default identity `MatrixTransform`. | Detaching uses `ClearValue` for `RenderTransform` and `RenderTransformOrigin`; code that checks whether a child is transformed should test for a non-identity matrix. |
| Manipulation start events | Depending on the Uno version, the distance travelled before the manipulation was recognized is reported in the first delta or only in `ManipulationStarted.Cumulative` (unoplatform/uno#20473). | Reported in `ManipulationStarted.Cumulative`, never in a delta. | `ZoomBorder` applies the difference between the cumulative manipulation and what it already applied, so both behaviors give the same result and nothing is applied twice. |
| Manipulation start distance | The whole finger travel is reported. | Part of the finger travel before the manipulation starts is never reported: a single finger drag pans about 4 to 10 pixels less than the finger moved (a 20 pixel drag pans about 10 pixels, a 100 pixel drag about 93 to 97 pixels). The loss does not depend on the speed or the number of pointer updates. | Platform behavior, the control follows the WinUI manipulation like any other WinUI element. |
| Manipulation tracking | Follows the touch points exactly. | The manipulation output trails a moving finger by a few pixels and does not catch up when the finger lifts while moving. | Platform behavior, not compensated by the control. |
| Pinch with a lifted finger | The manipulation completes when a finger lifts. | The manipulation continues until all fingers lifted, so lifting one finger of a pinch does not complete it. | `ZoomBorder` tracks the touch pointers and ends the pinch (`PinchEnded`, gesture ended events) when fewer than two fingers remain, like Avalonia and Uno. A later scale change starts a new pinch. |
| Touch pointer capture | Manipulations capture the touch pointer implicitly. | Touch pointers are not captured, so moves outside the control are not routed to it. | On WinUI (not Uno), `ZoomBorder` captures touch and pen pointers when they are pressed (gestures enabled), like the Avalonia control. An explicit capture on Uno would change its tap recognition. |
| Multiple taps | `DoubleTapped` is raised for every tap that follows a tap (taps 2 and 3 of a triple tap). | `DoubleTapped` is raised once for a triple tap. | Platform behavior: a triple tap zooms in on WinUI and zooms in and back out on Uno with the default `DoubleClickZoomMode`. |
| Stationary touch contacts | Delivered immediately. | Windows holds back `PointerPressed` of a contact that does not move (about 150 ms for one contact, until a contact moves or lifts for two contacts). | Platform behavior; only matters for code that reacts to presses before any movement. |
| Middle button on macOS | Uno desktop host workaround (unoplatform/uno#24852). | Not applicable. | The workaround is inert on WinUI. |
| Default style of subclasses | The `ZoomBorder` default style applies to any subclass. | The default style applies only to subclasses that have XAML type metadata. A subclass declared only in C# fails with "Cannot apply a Style with TargetType 'PanAndZoom.ZoomBorder'". | Use public subclasses that the app's XAML compiler knows about, for example by referencing them in a XAML resource dictionary (see `tests/PanAndZoom.WinUI.RuntimeTests/TestTypes.xaml`). |
| Package layout | Uno packs the compiled XAML resources itself. | The NuGet package must contain `PanAndZoom.WinUI.pri` and `PanAndZoom.WinUI/Themes/Generic.xbf`. | `PanAndZoom.WinUI.csproj` adds the XAML resource layout to the package. |
| Windows target | `PanAndZoom.Uno` targets `net10.0-windows10.0.26100` for its Windows App SDK head. | `PanAndZoom.WinUI` targets `net10.0-windows10.0.19041.0` (minimum 10.0.17763). | Separate packages. |

## Sample Differences

`samples/WinUIDemo` is an unpackaged, self-contained Windows App SDK app built from the `samples/UnoDemo` sources. Uno only features are excluded under `PANANDZOOM_WINUI`: `Uno.Resizetizer` window icons (`SetWindowIcon`) and the Uno logging setup. The sample self-test (`UNODEMO_SELFTEST=1`) runs every demo page on both platforms.

## Runtime Test Differences

The WinUI runtime tests run the same test classes as the Uno runtime tests in a native WinUI app, with real Windows input injection. The test infrastructure hides the differences, so the tests themselves are shared.

### Test Runner

| Area | Uno Platform | WinUI 3 |
|---|---|---|
| Runner | The embedded Uno.UI.RuntimeTests.Engine runner, started by `UNO_RUNTIME_TESTS_RUN_TESTS`. | The embedded runner needs `Window.Current`, which is always `null` in WinUI 3 desktop apps. `WinUITestRunner` starts the tests when `PANANDZOOM_WINUI_TESTS` is set. |
| Settings | `ApplicationData.Current.LocalSettings` | Throws in unpackaged apps (no package identity). A build step patches the engine's `UnitTestsControl` to use an in-memory store. |
| Window | Any size. | Injected input goes to the window under the pointer, so the runner maximizes the window, keeps it on top and in the foreground, and keeps the display on (input is not delivered while the display sleeps). The mouse must not be used while the tests run. |

### Input Injection

| Area | Uno Platform | WinUI 3 |
|---|---|---|
| Delivery | Synchronous: the event is raised before `InjectXxxInput` returns. | Asynchronous through the OS input queue, and bursts of pointer updates are coalesced. `WinUIInputPump` injects one frame per system timer tick (15.6 ms), and `ZoomBorderTestHelper.WaitForIdleAsync` waits until the queue drained plus a short settle delay. |
| Coordinates | Window coordinates (logical pixels). | Screen coordinates (physical pixels); `InjectionCoordinates` converts them. |
| Mouse moves | Relative moves are exact. | Relative moves are subject to pointer acceleration; absolute moves are used. |
| Mouse buttons | Several buttons can be released in one event. | Each button is released separately. |
| Mouse wheel | Delta read from `DeltaX` and `DeltaY`. | Delta read from `MouseData` only. |
| Keyboard modifiers | Passed with the pointer event. | Real key presses; the keys stay down until the wheel event was dispatched. |
| Touch pointer ids | Any id. | Small ids only (a contact slot per id); Windows assigns its own pointer ids, which the recorder maps back by press order. |
| Touch time offsets | Simulated timestamps. | Must be 0; a time offset longer than a frame becomes a real delay. |
| Touch frames | A frame can describe only the contacts that changed. | Every frame must describe all active contacts, and contacts that are not refreshed are cancelled. The pump merges frames with the active contacts and refreshes them while idle. |
| Stationary touch contacts | Delivered immediately. | Held back by Windows (see above); the pump moves each new contact by one pixel and back, like a real finger. |
| Moving touch contacts | Lifted exactly where the last frame placed them. | Manipulation output trails the finger (see above); the pump holds a contact that moved still for a few frames before lifting it, like a finger that stops and lifts. |
| Touch pan distances | Exact. | Windows drops part of the travel when a manipulation starts (see above). Offset assertions of touch drags use `ZoomBorderTestHelper.TouchPanTolerance`, which adds 10 pixels per drag on WinUI. |
| Two finger scale | Exact. | The scale of fingers moving one after the other is smoothed; a two finger pan ends within 3% of the initial zoom instead of exactly on it. |
| Injected timestamps | Pointer timestamps are the injected time. | Pointer timestamps are the times Windows received the frames; timing assertions use a tolerance. |
| Pen | No initialization. | `InitializePenInjection` is required first. |

## Validation

Last validated on 2026-10-03:

| Run | Result |
|---|---|
| WinUI runtime tests (`build/run-winui-runtime-tests.ps1`, Windows 11 ARM64) | 789 passed, 3 skipped (macOS host only tests), 0 failed |
| WinUI sample self-test (`samples/WinUIDemo`, `UNODEMO_SELFTEST=1`) | 25 demo pages, 0 failures |
| Uno runtime tests (`build/run-uno-runtime-tests.sh`, macOS desktop) | 792 passed, 0 failed |

## Open Issues

| Issue | Status |
|---|---|
| Windows drops part of the finger travel when a touch manipulation starts, so touch pans are a few pixels shorter than on Uno. | Platform behavior. Documented; the control follows the WinUI manipulation events like other WinUI controls. |
| Uno versions report the distance travelled before a manipulation is recognized differently (unoplatform/uno#20473). | Handled: `ZoomBorder` works from the cumulative manipulation. |
| No CI job runs the WinUI runtime tests: real input injection needs an interactive desktop where the test window stays in the foreground. CI builds the WinUI library, sample and test app, and packs `PanAndZoom.WinUI`. | Open |
| The test window can lose the foreground in an interactive session while the tests run (injected input then goes to another window). | Mitigated: the runner brings the window back before every test and logs it. |
