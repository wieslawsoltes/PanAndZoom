// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Windows.System;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder driven by touch input.
/// </summary>
/// <remarks>
/// The Avalonia tests use the headless <c>TouchInputSimulator</c>. On Uno the touch input is real injected
/// touch (<see cref="TouchInjectionSession"/>, <see cref="TouchTestHelpers"/>). Assertions on the simulator
/// bookkeeping (active touch points, timestamps) become assertions on the pointer events that the control
/// actually receives (<see cref="TouchPointerRecorder"/>).
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderTouchSimulatorTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<ZoomBorder> CreateAsync(Action<ZoomBorder>? configure = null)
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, configure);
        return zoomBorder;
    }

    private static async Task<ZoomBorder> CreateWithoutChildAsync()
    {
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        return zoomBorder;
    }

    private static void AssertPoint(Point expected, Point actual)
    {
        Assert.AreEqual(expected.X, actual.X, 1.0, $"X expected {expected} actual {actual}");
        Assert.AreEqual(expected.Y, actual.Y, 1.0, $"Y expected {expected} actual {actual}");
    }

    [TestMethod]
    public async Task TouchInputSimulator_TouchDown_RaisesPointerPressed()
    {
        var zoomBorder = await CreateAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);

        session.TouchDown(new Point(100, 75));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.PressedCount > 0, "PointerPressed event should be raised");
    }

    [TestMethod]
    public async Task TouchInputSimulator_TouchMove_RaisesPointerMoved()
    {
        var zoomBorder = await CreateAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);

        var touchId = session.TouchDown(new Point(100, 75));
        session.TouchMove(touchId, new Point(150, 100));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.MovedCount > 0, "PointerMoved event should be raised");
    }

    [TestMethod]
    public async Task TouchInputSimulator_TouchUp_RaisesPointerReleased()
    {
        var zoomBorder = await CreateAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);

        var touchId = session.TouchDown(new Point(100, 75));
        session.TouchUp(touchId);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.ReleasedCount > 0, "PointerReleased event should be raised");
    }

    [TestMethod]
    public async Task TouchInputSimulator_Tap_CompletesSuccessfully()
    {
        var zoomBorder = await CreateAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);

        TouchTestHelpers.Tap(zoomBorder, new Point(100, 75));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(1, recorder.PressedCount);
        Assert.AreEqual(1, recorder.ReleasedCount);
    }

    [TestMethod]
    public async Task TouchInputSimulator_DoubleTap_RaisesTwoTapSequences()
    {
        var zoomBorder = await CreateAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);

        TouchTestHelpers.DoubleTap(zoomBorder, new Point(100, 75));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(2, recorder.PressedCount);
        Assert.AreEqual(2, recorder.ReleasedCount);
    }

    [TestMethod]
    public async Task TouchInputSimulator_PinchGesture_ZoomsIn()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestureZoom = true;
            zb.EnableGestures = true;
        });
        var initialZoom = zoomBorder.ZoomX;

        // Uno: real two finger pinch (finger distance x1.5) instead of a synthetic PinchEvent
        TouchTestHelpers.PinchScale(zoomBorder, 1.5, new Point(200, 150));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "ZoomX should increase after pinch zoom in");
    }

    [TestMethod]
    public async Task TouchInputSimulator_PinchGesture_ZoomsOut()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestureZoom = true;
            zb.EnableGestures = true;
        });

        // Zoom in first
        zoomBorder.ZoomTo(2.0, 100, 75);
        var initialZoom = zoomBorder.ZoomX;

        // Uno: real two finger pinch (finger distance x0.7) instead of a synthetic PinchEvent
        TouchTestHelpers.PinchScale(zoomBorder, 0.7, new Point(200, 150));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX < initialZoom, "ZoomX should decrease after pinch zoom out");
    }

    [TestMethod]
    public async Task TouchInputSimulator_ScrollGesture_Pans()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestureTranslation = true;
            zb.EnableGestures = true;
        });
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // Uno: real one finger drag by (-50, -30) instead of a synthetic ScrollGestureEvent with delta (50, 30)
        TouchTestHelpers.ScrollGesture(zoomBorder, 50, 30);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.OffsetX != initialOffsetX || zoomBorder.OffsetY != initialOffsetY,
            "Offset should change after scroll gesture");
    }

    [TestMethod]
    public async Task TouchInputSimulator_TouchpadMagnify_RaisesEvent()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestureZoom = true;
            zb.EnableGestures = true;
        });

        // Uno adaptation: WinUI has no touchpad magnify event; a touchpad pinch is delivered as Ctrl + wheel
        var exception = TouchTestHelpers.Record(() =>
            InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 60, VirtualKeyModifiers.Control));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // The important thing is it doesn't crash
        Assert.IsNull(exception, "TouchpadMagnify should not throw");
    }

    [TestMethod]
    public async Task TouchInputSimulator_TouchpadSwipe_RaisesEvent()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestureTranslation = true;
            zb.EnableGestures = true;
        });

        // Uno adaptation: WinUI has no touchpad swipe event; touchpad scrolling is delivered as horizontal wheel input
        var exception = TouchTestHelpers.Record(() =>
            InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 100, horizontal: true));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception, "TouchpadSwipe should not throw");
    }

    [TestMethod]
    public async Task TouchInputSimulator_SimulatePinchZoom_PerformsMultiStepZoom()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestureZoom = true;
            zb.EnableGestures = true;
        });
        var initialZoom = zoomBorder.ZoomX;

        TouchTestHelpers.Pinch(zoomBorder, new Point(200, 150), 50, 150, 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "ZoomX should increase after pinch zoom in simulation");
    }

    [TestMethod]
    public async Task TouchInputSimulator_SimulateTwoFingerPan_PerformsMultiStepPan()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestureTranslation = true;
            zb.EnableGestures = true;
        });
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        var exception = TouchTestHelpers.Record(() =>
            TouchTestHelpers.TwoFingerPan(zoomBorder, new Point(100, 100), new Point(200, 150), 50, 5));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception, "Two finger pan simulation should complete without errors");
        // Uno: the real two finger pan translates the manipulation centroid, so the offset is also verified
        Assert.IsTrue(zoomBorder.OffsetX != initialOffsetX || zoomBorder.OffsetY != initialOffsetY, "Offset should change after two finger pan");
    }

    [TestMethod]
    public async Task TouchInputSimulator_SimulateRotation_PerformsMultiStepRotation()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestureRotation = true;
            zb.EnableGestures = true;
        });

        var exception = TouchTestHelpers.Record(() =>
            TouchTestHelpers.Rotate(zoomBorder, new Point(200, 150), 50, 0, 45, 5));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception, "Rotation simulation should complete without errors");
    }

    [TestMethod]
    public async Task TouchInputSimulator_SimulateDrag_PerformsSingleFingerDrag()
    {
        var zoomBorder = await CreateAsync(zb => zb.EnablePan = true);
        using var recorder = new TouchPointerRecorder(zoomBorder);

        TouchTestHelpers.Drag(zoomBorder, new Point(100, 100), new Point(200, 150), 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.MovedCount >= 5, $"PointerMoved should be raised multiple times, was {recorder.MovedCount}");
    }

    [TestMethod]
    public async Task TouchInputSimulator_Swipe_PerformsSwipeInDirection()
    {
        var zoomBorder = await CreateAsync(zb => zb.EnablePan = true);
        using var recorder = new TouchPointerRecorder(zoomBorder);

        TouchTestHelpers.Swipe(zoomBorder, new Point(200, 150), TouchSwipeDirection.Left, 100, 100);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.PressedCount > 0, "PointerPressed should be raised");
        Assert.IsTrue(recorder.ReleasedCount > 0, "PointerReleased should be raised");
    }

    [TestMethod]
    public async Task TouchInputSimulator_Swipe_AllDirections()
    {
        var zoomBorder = await CreateAsync(zb => zb.EnablePan = true);
        using var recorder = new TouchPointerRecorder(zoomBorder);

        // All directions should work without errors
        var exception = TouchTestHelpers.Record(() =>
        {
            TouchTestHelpers.Swipe(zoomBorder, new Point(200, 150), TouchSwipeDirection.Left);
            TouchTestHelpers.Swipe(zoomBorder, new Point(200, 150), TouchSwipeDirection.Right);
            TouchTestHelpers.Swipe(zoomBorder, new Point(200, 150), TouchSwipeDirection.Up);
            TouchTestHelpers.Swipe(zoomBorder, new Point(200, 150), TouchSwipeDirection.Down);
        });
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception, "All swipe directions should complete without errors");
        // Uno: each swipe is a real press/release sequence received by the control
        Assert.AreEqual(4, recorder.PressedCount);
        Assert.AreEqual(4, recorder.ReleasedCount);
    }

    [TestMethod]
    public async Task TouchInputSimulator_AdvanceTime_UpdatesTimestamp()
    {
        var zoomBorder = await CreateWithoutChildAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);
        var initialTimestamp = session.Timestamp;

        session.AdvanceTime(100);

        Assert.AreEqual(initialTimestamp + 100UL, session.Timestamp);

        // Uno: the advanced time is carried by the next injected frame, so the pointer timestamps
        // (microseconds) received by the control are 100ms apart.
        var touchId = session.TouchDown(new Point(100, 100));
        session.AdvanceTime(100);
        session.TouchUp(touchId);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        var pressed = recorder.Events.First(e => e.Kind == "Pressed" && e.Id == touchId);
        var released = recorder.Events.First(e => e.Kind == "Released" && e.Id == touchId);
#if PANANDZOOM_WINUI
        // WinUI: Windows touch injection requires a zero time offset, so the advanced time is a real delay
        // and the timestamps are the times Windows received the frames (at least 100ms apart).
        var elapsed = released.Timestamp - pressed.Timestamp;
        Assert.IsTrue(elapsed >= 90_000UL && elapsed < 300_000UL, $"Expected about 100ms between press and release, was {elapsed / 1000}ms");
#else
        Assert.AreEqual(100_000UL, released.Timestamp - pressed.Timestamp);
#endif
    }

    [TestMethod]
    public async Task TouchInputSimulator_Reset_ClearsState()
    {
        var zoomBorder = await CreateWithoutChildAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);

        session.TouchDown(new Point(100, 100));
        session.AdvanceTime(100);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.AreEqual(1, recorder.ActivePointers.Count);

        session.Reset();
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(0UL, session.Timestamp);
        Assert.AreEqual(0, session.ActiveTouchPoints.Count);
        // Uno: resetting lifts the active fingers, so the control has no pressed touch pointer left
        Assert.AreEqual(0, recorder.ActivePointers.Count);
    }

    [TestMethod]
    public async Task TouchInputSimulator_ActiveTouchPoints_TracksActiveTouches()
    {
        var zoomBorder = await CreateWithoutChildAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);

        var touch1 = session.TouchDown(new Point(100, 100));
        var touch2 = session.TouchDown(new Point(200, 200));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Uno: active touches are verified on the pointers the control received
        Assert.AreEqual(2, recorder.ActivePointers.Count);
        Assert.IsTrue(recorder.ActivePointers.ContainsKey(touch1));
        Assert.IsTrue(recorder.ActivePointers.ContainsKey(touch2));
        AssertPoint(new Point(100, 100), recorder.ActivePointers[touch1]);
        AssertPoint(new Point(200, 200), recorder.ActivePointers[touch2]);
    }

    [TestMethod]
    public async Task TouchInputSimulator_TouchUp_RemovesFromActiveTouchPoints()
    {
        var zoomBorder = await CreateWithoutChildAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);
        var touchId = session.TouchDown(new Point(100, 100));

        session.TouchUp(touchId);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Uno: verified on the pointers the control received
        Assert.AreEqual(0, recorder.ActivePointers.Count);
        Assert.AreEqual(0, session.ActiveTouchPoints.Count);
    }

    [TestMethod]
    public async Task TouchInputSimulator_TouchMove_UpdatesPosition()
    {
        var zoomBorder = await CreateWithoutChildAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);
        var touchId = session.TouchDown(new Point(100, 100));

        session.TouchMove(touchId, new Point(150, 150));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Uno: the position reported to the control (whole pixel injection, so within 1px)
        AssertPoint(new Point(150, 150), recorder.ActivePointers[touchId]);
    }

    [TestMethod]
    public async Task TouchInputSimulator_TouchMove_WithInvalidId_Throws()
    {
        var zoomBorder = await CreateWithoutChildAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);

        var exception = TouchTestHelpers.Record(() => session.TouchMove(999, new Point(150, 150)));
        Assert.IsInstanceOfType(exception, typeof(InvalidOperationException));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Uno: the unknown contact must not reach the control (the raw injector would accept it)
        Assert.AreEqual(0, recorder.TotalCount);
    }

    [TestMethod]
    public async Task TouchInputSimulator_TouchUp_WithInvalidId_Throws()
    {
        var zoomBorder = await CreateWithoutChildAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);

        var exception = TouchTestHelpers.Record(() => session.TouchUp(999));
        Assert.IsInstanceOfType(exception, typeof(InvalidOperationException));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Uno: the unknown contact must not reach the control (the raw injector would accept it)
        Assert.AreEqual(0, recorder.TotalCount);
    }

    [TestMethod]
    public async Task TouchInputSimulator_PinchGestureEnded_RaisesEvent()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestureZoom = true;
            zb.EnableGestures = true;
        });
        var ended = new List<string>();
        zoomBorder.GestureEnded += (_, e) => ended.Add(e.GestureType);

        // Uno: the pinch ends when the fingers of the real pinch are lifted
        var exception = TouchTestHelpers.Record(() =>
            TouchTestHelpers.PinchScale(zoomBorder, 1.5, new Point(200, 150)));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception, "PinchGestureEnded should not throw");
        CollectionAssert.Contains(ended, "Pinch");
    }

    [TestMethod]
    public async Task TouchInputSimulator_ScrollGestureEnded_RaisesEvent()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestureTranslation = true;
            zb.EnableGestures = true;
        });
        var ended = new List<string>();
        zoomBorder.GestureEnded += (_, e) => ended.Add(e.GestureType);

        // Uno: the scroll ends when the finger of the real drag is lifted
        var exception = TouchTestHelpers.Record(() =>
            TouchTestHelpers.ScrollGesture(zoomBorder, 50, 30));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception, "ScrollGestureEnded should not throw");
        CollectionAssert.Contains(ended, "Scroll");
    }

    [TestMethod]
    public async Task TouchInputSimulator_SimulateDrag_ActuallyPans()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnablePan = true;
            zb.PanButton = ButtonName.Left;
        });

        // First zoom in so we have something to pan
        zoomBorder.ZoomTo(2.0, 200, 150, skipTransitions: true);

        var initialOffsetX = zoomBorder.OffsetX;
        var isPanning = false;
        zoomBorder.PanContinued += (_, _) => isPanning = true;

        // Uno: one finger touch is handled by the manipulation (scroll gesture) which pans through PanDelta
        TouchTestHelpers.Drag(zoomBorder, new Point(100, 100), new Point(200, 150), 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(isPanning, "PanContinued should be raised");
        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);
    }
}
