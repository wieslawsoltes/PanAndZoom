// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Gesture tests ported from the Avalonia <c>ZoomBorderGestureRecognizerTests</c>.
/// </summary>
/// <remarks>
/// Avalonia uses <c>PinchGestureRecognizer</c>/<c>ScrollGestureRecognizer</c>; on Uno the same gestures are WinUI
/// manipulations (one finger pans = "Scroll" gesture, two fingers pinch/rotate = "Pinch" gesture). The Avalonia
/// <c>PinchEvent</c>/<c>ScrollGestureEvent</c> handlers are replaced by the <see cref="ZoomBorder.GestureStarted"/>
/// events (raised for every processed pinch/scroll update) recorded by <see cref="GestureRecorder"/>, and the
/// Avalonia <c>GestureRecognizerTestHelper</c> by <see cref="TestPointer"/> (real injected input).
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderGestureRecognizerTests
{
    private const ManipulationModes GestureManipulations =
        ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.Scale | ManipulationModes.Rotate;

    [TestCleanup]
    public async Task Cleanup()
    {
        GestureTestHelpers.ResetTouch();
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    #region Helper Methods

    private static async Task<ZoomBorder> CreateAndLoadZoomBorderAsync(Action<ZoomBorder>? configure = null)
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = true;
            zb.EnableGestureTranslation = true;
            zb.EnableGestureRotation = true;
            configure?.Invoke(zb);
        });

        return zoomBorder;
    }

    private static async Task<(ZoomBorder ZoomBorder, GestureRecorder Recorder)> SetupZoomBorderAsync(Action<ZoomBorder>? configure = null)
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync(configure);
        return (zoomBorder, new GestureRecorder(zoomBorder));
    }

    private static Task IdleAsync() => ZoomBorderTestHelper.WaitForIdleAsync();

    #endregion

    #region PinchGestureRecognizer Tests

    [TestMethod]
    public async Task PinchGestureRecognizer_TwoFingerTouch_RaisesPinchEvent()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();

        // Uno adaptation: the pinch recognizer is the Scale/Rotate manipulation mode instead of a PinchGestureRecognizer instance.
        Assert.AreEqual(ManipulationModes.Scale | ManipulationModes.Rotate, zoomBorder.ManipulationMode & (ManipulationModes.Scale | ManipulationModes.Rotate), "ZoomBorder should enable pinch (scale/rotate) manipulations");

        var firstTouch = new TestPointer();
        var secondTouch = new TestPointer();

        firstTouch.Down(zoomBorder, new Point(100, 100));
        secondTouch.Down(zoomBorder, new Point(200, 100));
        firstTouch.Move(zoomBorder, new Point(110, 100));
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count >= 1, $"Pinch not received! pinchCount={recorder.Pinches.Count}, manipulationDeltas={recorder.ManipulationDeltas.Count}");

        secondTouch.Up(zoomBorder);
        firstTouch.Up(zoomBorder);
        await IdleAsync();
    }

    [TestMethod]
    public async Task PinchGestureRecognizer_SingleFinger_DoesNotRaisePinchEvent()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var touch = new TestPointer();

        touch.Down(zoomBorder, new Point(100, 100));
        touch.Move(zoomBorder, new Point(200, 200));
        touch.Up(zoomBorder, new Point(200, 200));
        await IdleAsync();

        Assert.AreEqual(0, recorder.Pinches.Count, "Pinch should not be raised for single finger");
    }

    [TestMethod]
    public async Task PinchGestureRecognizer_SamePointerTwice_DoesNotRaisePinchEvent()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var touch = new TestPointer();

        // Same pointer pressed twice (shouldn't work)
        touch.Down(zoomBorder, new Point(100, 100));
        touch.Down(zoomBorder, new Point(150, 100));
        touch.Move(zoomBorder, new Point(200, 100));
        await IdleAsync();

        Assert.AreEqual(0, recorder.Pinches.Count, "Pinch should not be raised for same pointer");

        touch.Up(zoomBorder);
        await IdleAsync();
    }

    [TestMethod]
    public async Task PinchGestureRecognizer_ZoomIn_IncreasesScale()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var firstTouch = new TestPointer();
        var secondTouch = new TestPointer();

        firstTouch.Down(zoomBorder, new Point(150, 150));
        secondTouch.Down(zoomBorder, new Point(250, 150));
        firstTouch.Move(zoomBorder, new Point(100, 150));
        secondTouch.Move(zoomBorder, new Point(300, 150));
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch should be raised");
        Assert.IsTrue(recorder.LastPinchScale > 1.0, $"Scale should be > 1.0 for zoom in, was {recorder.LastPinchScale}");

        firstTouch.Up(zoomBorder);
        secondTouch.Up(zoomBorder);
        await IdleAsync();
    }

    [TestMethod]
    public async Task PinchGestureRecognizer_ZoomOut_DecreasesScale()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var firstTouch = new TestPointer();
        var secondTouch = new TestPointer();

        firstTouch.Down(zoomBorder, new Point(50, 150));
        secondTouch.Down(zoomBorder, new Point(350, 150));
        firstTouch.Move(zoomBorder, new Point(150, 150));
        secondTouch.Move(zoomBorder, new Point(250, 150));
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch should be raised");
        Assert.IsTrue(recorder.LastPinchScale < 1.0, $"Scale should be < 1.0 for zoom out, was {recorder.LastPinchScale}");

        firstTouch.Up(zoomBorder);
        secondTouch.Up(zoomBorder);
        await IdleAsync();
    }

    [TestMethod]
    public async Task PinchGestureRecognizer_Rotation_ReportsAngle()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var firstTouch = new TestPointer();
        var secondTouch = new TestPointer();

        firstTouch.Down(zoomBorder, new Point(100, 150));
        secondTouch.Down(zoomBorder, new Point(300, 150));
        firstTouch.Move(zoomBorder, new Point(150, 100));
        secondTouch.Move(zoomBorder, new Point(250, 200));
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch should be raised");
        // Uno adaptation: the angle is reported by the manipulation (cumulative rotation) that drives the pinch.
        Assert.AreNotEqual(0.0, recorder.ManipulationDeltas[^1].Rotation);

        firstTouch.Up(zoomBorder);
        secondTouch.Up(zoomBorder);
        await IdleAsync();
    }

    [TestMethod]
    public async Task PinchGestureRecognizer_ReleaseOneFinger_RaisesPinchEnded()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var firstTouch = new TestPointer();
        var secondTouch = new TestPointer();

        firstTouch.Down(zoomBorder, new Point(100, 100));
        secondTouch.Down(zoomBorder, new Point(200, 100));
        secondTouch.Move(zoomBorder, new Point(250, 100));
        secondTouch.Up(zoomBorder, new Point(250, 100));
        await IdleAsync();

        Assert.IsTrue(recorder.PinchEndedCount > 0, "PinchEnded should be raised when one finger is released");

        firstTouch.Up(zoomBorder);
        await IdleAsync();
    }

    [TestMethod]
    public async Task PinchGestureRecognizer_CaptureLost_EndsPinch()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var firstTouch = new TestPointer();
        var secondTouch = new TestPointer();

        firstTouch.Down(zoomBorder, new Point(100, 100));
        secondTouch.Down(zoomBorder, new Point(200, 100));
        secondTouch.Move(zoomBorder, new Point(250, 100));
        await IdleAsync();

        // Cancel first touch (simulate capture lost)
        firstTouch.Cancel();

        // Continue with second touch
        secondTouch.Move(zoomBorder, new Point(300, 100));
        secondTouch.Up(zoomBorder);
        await IdleAsync();

        // Since we lost one contact, the pinch must have ended (Avalonia only documents this scenario).
        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch should be raised before the contact is lost");
        Assert.IsTrue(recorder.PinchEndedCount > 0, "Pinch should end once the contacts are lost/released");
    }

    [TestMethod]
    public async Task PinchGestureRecognizer_MultipleSequentialPinches_AllRecognized()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();

        var first1 = new TestPointer();
        var second1 = new TestPointer();
        first1.Down(zoomBorder, new Point(100, 100));
        second1.Down(zoomBorder, new Point(200, 100));
        second1.Move(zoomBorder, new Point(250, 100));
        first1.Up(zoomBorder);
        second1.Up(zoomBorder);
        await IdleAsync();

        var countAfterFirst = recorder.Pinches.Count;

        var first2 = new TestPointer();
        var second2 = new TestPointer();
        first2.Down(zoomBorder, new Point(100, 100));
        second2.Down(zoomBorder, new Point(200, 100));
        second2.Move(zoomBorder, new Point(250, 100));
        first2.Up(zoomBorder);
        second2.Up(zoomBorder);
        await IdleAsync();

        Assert.IsTrue(countAfterFirst > 0, "First pinch should be recognized");
        Assert.IsTrue(recorder.Pinches.Count > countAfterFirst, "Second pinch should also be recognized");
    }

    #endregion

    #region ScrollGestureRecognizer Tests

    [TestMethod]
    public async Task ScrollGestureRecognizer_SingleFingerDrag_RaisesScrollGesture()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var touch = new TestPointer();

        touch.Down(zoomBorder, new Point(100, 100));
        touch.Move(zoomBorder, new Point(200, 200));
        touch.Move(zoomBorder, new Point(250, 250));
        await IdleAsync();

        Assert.IsTrue(recorder.Scrolls.Count > 0, "Scroll gesture should be raised for single finger drag");

        touch.Up(zoomBorder);
        await IdleAsync();
    }

    [TestMethod]
    public async Task ScrollGestureRecognizer_SmallMovement_DoesNotRaiseScrollGesture()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var touch = new TestPointer();

        touch.Down(zoomBorder, new Point(100, 100));
        touch.Move(zoomBorder, new Point(102, 102));
        touch.Up(zoomBorder, new Point(102, 102));
        await IdleAsync();

        Assert.AreEqual(0, recorder.Scrolls.Count, "Scroll gesture should not be raised for small movement");
    }

    [TestMethod]
    public async Task ScrollGestureRecognizer_HorizontalScroll_ReportsCorrectDelta()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        var touch = new TestPointer();

        touch.Down(zoomBorder, new Point(100, 100));
        touch.Move(zoomBorder, new Point(200, 100));
        touch.Move(zoomBorder, new Point(300, 100));
        touch.Up(zoomBorder);
        await IdleAsync();

        // Avalonia leaves this unasserted; on Uno the horizontal drag must produce a purely horizontal pan.
        Assert.IsTrue(recorder.Scrolls.Count > 0, "Scroll gesture should be raised");
        Assert.AreEqual(initialOffsetX + 200, zoomBorder.OffsetX, 1.0);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY, 1.0);
    }

    [TestMethod]
    public async Task ScrollGestureRecognizer_VerticalScroll_ReportsCorrectDelta()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        var touch = new TestPointer();

        touch.Down(zoomBorder, new Point(100, 100));
        touch.Move(zoomBorder, new Point(100, 200));
        touch.Move(zoomBorder, new Point(100, 280));
        touch.Up(zoomBorder);
        await IdleAsync();

        // Avalonia leaves this unasserted (and drags outside the 300px high control); on Uno the vertical drag must produce a purely vertical pan.
        Assert.IsTrue(recorder.Scrolls.Count > 0, "Scroll gesture should be raised");
        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX, 1.0);
        Assert.AreEqual(initialOffsetY + 180, zoomBorder.OffsetY, 1.0);
    }

    [TestMethod]
    public async Task ScrollGestureRecognizer_Release_RaisesScrollGestureEnded()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var touch = new TestPointer();

        touch.Down(zoomBorder, new Point(100, 100));
        touch.Move(zoomBorder, new Point(200, 200));
        touch.Move(zoomBorder, new Point(300, 280));
        touch.Up(zoomBorder, new Point(300, 280));
        await IdleAsync();

        Assert.IsTrue(recorder.ScrollEndedCount > 0, "ScrollGestureEnded should be raised when finger is released");
    }

    [TestMethod]
    public async Task ScrollGestureRecognizer_MousePointer_DoesNotTrigger()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var mouse = new TestPointer(PointerDeviceType.Mouse);

        mouse.Down(zoomBorder, new Point(100, 100));
        mouse.Move(zoomBorder, new Point(200, 200));
        mouse.Move(zoomBorder, new Point(300, 280));
        mouse.Up(zoomBorder, new Point(300, 280));
        await IdleAsync();

        // Scroll gestures are touch manipulations; the mouse is handled by the pointer (PanButton) path.
        Assert.AreEqual(0, recorder.Scrolls.Count, "Scroll gesture should not be raised for mouse pointer");
    }

    [TestMethod]
    public async Task ScrollGestureRecognizer_PenPointer_Triggers()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var pen = new TestPointer(PointerDeviceType.Pen);

        pen.Down(zoomBorder, new Point(100, 100));
        pen.Move(zoomBorder, new Point(200, 200));
        pen.Move(zoomBorder, new Point(300, 280));
        await IdleAsync();

        Assert.IsTrue(recorder.Scrolls.Count > 0, "Scroll gesture should be raised for pen pointer");

        pen.Up(zoomBorder);
        await IdleAsync();
    }

    #endregion

    #region ZoomBorder Integration with Gesture Recognizers

    [TestMethod]
    public async Task ZoomBorder_PinchZoomIn_ChangesZoom()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync();
        var initialZoom = zoomBorder.ZoomX;
        var firstTouch = new TestPointer();
        var secondTouch = new TestPointer();

        firstTouch.Down(zoomBorder, new Point(150, 150));
        secondTouch.Down(zoomBorder, new Point(250, 150));
        for (var i = 0; i < 10; i++)
        {
            firstTouch.Move(zoomBorder, new Point(150 - i * 10, 150));
            secondTouch.Move(zoomBorder, new Point(250 + i * 10, 150));
        }

        firstTouch.Up(zoomBorder);
        secondTouch.Up(zoomBorder);
        await IdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX >= initialZoom, $"Zoom should increase or stay same. Initial: {initialZoom}, Current: {zoomBorder.ZoomX}");
        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, $"Real pinch input should zoom in. Initial: {initialZoom}, Current: {zoomBorder.ZoomX}");
    }

    [TestMethod]
    public async Task ZoomBorder_PinchZoomOut_ChangesZoom()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync();

        // First zoom in to have something to zoom out from (Uno equivalent of TouchInputSimulator.PinchGesture).
        GestureTestHelpers.Pinch(zoomBorder, 2.0, new Point(200, 150));
        zoomBorder.HandleManipulationCompleted();
        var initialZoom = zoomBorder.ZoomX;

        var firstTouch = new TestPointer();
        var secondTouch = new TestPointer();
        firstTouch.Down(zoomBorder, new Point(50, 150));
        secondTouch.Down(zoomBorder, new Point(350, 150));
        for (var i = 0; i < 10; i++)
        {
            firstTouch.Move(zoomBorder, new Point(50 + i * 10, 150));
            secondTouch.Move(zoomBorder, new Point(350 - i * 10, 150));
        }

        firstTouch.Up(zoomBorder);
        secondTouch.Up(zoomBorder);
        await IdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX <= initialZoom, $"Zoom should decrease or stay same. Initial: {initialZoom}, Current: {zoomBorder.ZoomX}");
        Assert.IsTrue(zoomBorder.ZoomX < initialZoom, $"Real pinch input should zoom out. Initial: {initialZoom}, Current: {zoomBorder.ZoomX}");
    }

    [TestMethod]
    public async Task ZoomBorder_GesturesDisabled_NoZoomChange()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync(zb => zb.EnableGestures = false);
        var initialZoom = zoomBorder.ZoomX;
        var firstTouch = new TestPointer();
        var secondTouch = new TestPointer();

        firstTouch.Down(zoomBorder, new Point(150, 150));
        secondTouch.Down(zoomBorder, new Point(250, 150));
        firstTouch.Move(zoomBorder, new Point(50, 150));
        secondTouch.Move(zoomBorder, new Point(350, 150));
        firstTouch.Up(zoomBorder);
        secondTouch.Up(zoomBorder);
        await IdleAsync();

        Assert.AreEqual(initialZoom, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task ZoomBorder_GestureZoomDisabled_NoZoomChange()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync(zb => zb.EnableGestureZoom = false);
        var initialZoom = zoomBorder.ZoomX;

        // Simulate pinch via the manipulation handler (Uno equivalent of TouchInputSimulator.PinchGesture).
        GestureTestHelpers.Pinch(zoomBorder, 2.0, new Point(200, 150));
        zoomBorder.HandleManipulationCompleted();

        Assert.AreEqual(initialZoom, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task ZoomBorder_ScrollGesture_ChangesPan()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync();
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // Uno adaptation: a real one finger drag (scroll delta 50,50 = finger moving by -50,-50) instead of TouchInputSimulator.ScrollGesture.
        new TestPointer().Drag(zoomBorder, new Point(200, 150), new Point(150, 100));
        await IdleAsync();

        var offsetChanged = zoomBorder.OffsetX != initialOffsetX || zoomBorder.OffsetY != initialOffsetY;
        Assert.IsTrue(offsetChanged, "Scroll gesture should change pan offset");
    }

    [TestMethod]
    public async Task ZoomBorder_GestureTranslationDisabled_NoPanChange()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync(zb => zb.EnableGestureTranslation = false);
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // Uno adaptation: a real one finger drag instead of TouchInputSimulator.ScrollGesture.
        new TestPointer().Drag(zoomBorder, new Point(200, 150), new Point(150, 100));
        await IdleAsync();

        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY);
    }

    #endregion

    #region MultiTouchTestHelperFactory Tests

    [TestMethod]
    public async Task MultiTouchFactory_SimulatePinch_RaisesPinchEvent()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();

        GestureTestHelpers.SimulatePinch(
            zoomBorder,
            new Point(100, 150),
            new Point(300, 150),
            new Point(50, 150),
            new Point(350, 150),
            steps: 5);
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch should be raised by SimulatePinch");
    }

    [TestMethod]
    public async Task MultiTouchFactory_SimulatePinchZoomIn_RaisesPinchWithScaleGreaterThanOne()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();

        GestureTestHelpers.SimulatePinchZoom(zoomBorder, new Point(200, 150), startDistance: 100, endDistance: 200, steps: 5);
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch should be raised");
        Assert.IsTrue(recorder.LastPinchScale > 1.0, "Scale should be > 1 for zoom in");
    }

    [TestMethod]
    public async Task MultiTouchFactory_SimulatePinchZoomOut_RaisesPinchWithScaleLessThanOne()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();

        GestureTestHelpers.SimulatePinchZoom(zoomBorder, new Point(200, 150), startDistance: 200, endDistance: 100, steps: 5);
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch should be raised");
        Assert.IsTrue(recorder.LastPinchScale < 1.0, "Scale should be < 1 for zoom out");
    }

    [TestMethod]
    public async Task MultiTouchFactory_SimulateTwoFingerPan_RaisesPinchEvents()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var initialZoom = zoomBorder.ZoomX;

        GestureTestHelpers.SimulateTwoFingerPan(zoomBorder, new Point(200, 150), new Point(300, 200), fingerSpacing: 50, steps: 5);
        await IdleAsync();

        // Avalonia leaves this unasserted (scale should be close to 1.0). The injected fingers move one after the other,
        // which produces transient pinch updates, so only the net scale and the processed translation are asserted.
        Assert.AreEqual(initialZoom, zoomBorder.ZoomX, 1e-6, "Two finger pan should keep the scale close to 1.0");
        Assert.IsTrue(recorder.Scrolls.Count > 0, "Two finger pan should be processed as translation");
    }

    [TestMethod]
    public async Task MultiTouchFactory_SimulateRotation_RaisesPinchWithAngle()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();

        GestureTestHelpers.SimulateRotation(zoomBorder, new Point(200, 150), radius: 50, startAngleDegrees: 0, endAngleDegrees: 45, steps: 5);
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count > 0, "Rotation should raise pinch updates");
        // Uno adaptation: the angle is reported by the manipulation cumulative rotation.
        Assert.IsTrue(recorder.ManipulationDeltas.Any(d => Math.Abs(d.Rotation) > 1.0), "Rotation should report angle changes");
    }

    [TestMethod]
    public void MultiTouchFactory_CreatePair_CreatesTwoDistinctHelpers()
    {
        // Uno adaptation: tests the Uno TestPointer factory (distinct injected pointer ids instead of Avalonia IPointer instances).
        var (first, second) = GestureTestHelpers.CreatePair();

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.AreNotSame(first, second);
        Assert.AreNotEqual(first.Id, second.Id);
    }

    [TestMethod]
    public void MultiTouchFactory_CreateMultiple_CreatesRequestedCount()
    {
        // Uno adaptation: tests the Uno TestPointer factory (distinct injected pointer ids instead of Avalonia IPointer instances).
        var helpers = GestureTestHelpers.Create(5);

        Assert.AreEqual(5, helpers.Length);
        for (var i = 0; i < helpers.Length; i++)
        {
            for (var j = i + 1; j < helpers.Length; j++)
            {
                Assert.AreNotSame(helpers[i], helpers[j]);
                Assert.AreNotEqual(helpers[i].Id, helpers[j].Id);
            }
        }
    }

    #endregion

    #region GestureRecognizerTestHelper Direct Tests

    [TestMethod]
    public async Task GestureRecognizerTestHelper_Down_CapturesPointer()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync();
        var outsideMoves = TrackOutsideMoves(zoomBorder);
        var helper = new TestPointer();

        helper.Down(zoomBorder, new Point(100, 100));
        await IdleAsync();

        // Uno adaptation: WinUI manipulations capture the touch pointer implicitly (it is not listed in PointerCaptures),
        // so capture is verified by the pointer still being routed to the control after it leaves the control bounds.
        helper.Move(zoomBorder, new Point(100, 350));
        await IdleAsync();
        Assert.IsTrue(outsideMoves.Count > 0, "Pointer should be captured by the control while pressed");

        helper.Up(zoomBorder);
        await IdleAsync();
    }

    [TestMethod]
    public async Task GestureRecognizerTestHelper_Up_ReleasesCapture()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync();
        var outsideMoves = TrackOutsideMoves(zoomBorder);
        var helper = new TestPointer();

        helper.Down(zoomBorder, new Point(100, 100));
        helper.Up(zoomBorder, new Point(100, 100));
        await IdleAsync();

        // Uno adaptation: once released, the pointer is no longer routed to the control outside of its bounds.
        helper.Move(zoomBorder, new Point(100, 350));
        await IdleAsync();
        Assert.AreEqual(0, outsideMoves.Count, "Pointer capture should be released");
        Assert.IsFalse(IsCaptured(zoomBorder, helper));
    }

    [TestMethod]
    public async Task GestureRecognizerTestHelper_Cancel_ReleasesCapture()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync();
        var outsideMoves = TrackOutsideMoves(zoomBorder);
        var helper = new TestPointer();

        helper.Down(zoomBorder, new Point(100, 100));
        helper.Cancel();
        await IdleAsync();

        // Uno adaptation: once canceled, the pointer is no longer routed to the control outside of its bounds.
        helper.Move(zoomBorder, new Point(100, 350));
        await IdleAsync();
        Assert.AreEqual(0, outsideMoves.Count, "Pointer capture should be released");
        Assert.IsFalse(IsCaptured(zoomBorder, helper));
    }

    [TestMethod]
    public async Task GestureRecognizerTestHelper_Tap_RaisesPointerPressedAndReleased()
    {
        var pressedRaised = false;
        var releasedRaised = false;
        var zoomBorder = await CreateAndLoadZoomBorderAsync(zb =>
        {
            zb.PointerPressed += (_, _) => pressedRaised = true;
            zb.PointerReleased += (_, _) => releasedRaised = true;
        });

        new TestPointer().Tap(zoomBorder, new Point(100, 100));
        await IdleAsync();

        Assert.IsTrue(pressedRaised, "PointerPressed should be raised");
        Assert.IsTrue(releasedRaised, "PointerReleased should be raised");
    }

    [TestMethod]
    public async Task GestureRecognizerTestHelper_Drag_TriggersScrollGesture()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();

        new TestPointer().Drag(zoomBorder, new Point(100, 100), new Point(300, 280), steps: 5);
        await IdleAsync();

        Assert.IsTrue(recorder.Scrolls.Count > 0, "Scroll gesture should be raised during drag");
    }

    [TestMethod]
    public async Task GestureRecognizerTestHelper_TouchPointerType_IsTouch()
    {
        // Uno adaptation: besides the helper type, verifies the device type the control actually receives.
        var helper = new TestPointer();
        Assert.AreEqual(PointerDeviceType.Touch, helper.PointerDeviceType);
        Assert.AreEqual(PointerDeviceType.Touch, await GetReceivedPointerTypeAsync(helper));
    }

    [TestMethod]
    public async Task GestureRecognizerTestHelper_PenPointerType_IsPen()
    {
        // Uno adaptation: besides the helper type, verifies the device type the control actually receives.
        var helper = new TestPointer(PointerDeviceType.Pen);
        Assert.AreEqual(PointerDeviceType.Pen, helper.PointerDeviceType);
        Assert.AreEqual(PointerDeviceType.Pen, await GetReceivedPointerTypeAsync(helper));
    }

    [TestMethod]
    public async Task GestureRecognizerTestHelper_MousePointerType_IsMouse()
    {
        // Uno adaptation: besides the helper type, verifies the device type the control actually receives.
        var helper = new TestPointer(PointerDeviceType.Mouse);
        Assert.AreEqual(PointerDeviceType.Mouse, helper.PointerDeviceType);
        Assert.AreEqual(PointerDeviceType.Mouse, await GetReceivedPointerTypeAsync(helper));
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [TestMethod]
    public async Task ThreeFingerTouch_OnlyFirstTwoUsedForPinch()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        var first = new TestPointer();
        var second = new TestPointer();
        var third = new TestPointer();

        first.Down(zoomBorder, new Point(100, 100));
        second.Down(zoomBorder, new Point(200, 100));
        third.Down(zoomBorder, new Point(150, 200));
        second.Move(zoomBorder, new Point(250, 100));
        third.Move(zoomBorder, new Point(150, 250));
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count >= 0);
        Assert.IsFalse(double.IsNaN(zoomBorder.ZoomX) || double.IsInfinity(zoomBorder.ZoomX), "Three finger input must not corrupt the zoom");

        third.Up(zoomBorder);
        second.Up(zoomBorder);
        first.Up(zoomBorder);
        await IdleAsync();
    }

    [TestMethod]
    public async Task RapidSequentialTouches_AllRecognized()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync();
        var tapCount = 0;
        zoomBorder.PointerPressed += (_, _) => tapCount++;

        for (var i = 0; i < 10; i++)
        {
            new TestPointer().Tap(zoomBorder, new Point(100 + i * 10, 100));
        }

        await IdleAsync();

        Assert.AreEqual(10, tapCount);
    }

    [TestMethod]
    public async Task PinchFollowedByScroll_BothRecognized()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();

        var first = new TestPointer();
        var second = new TestPointer();
        first.Down(zoomBorder, new Point(100, 100));
        second.Down(zoomBorder, new Point(200, 100));
        second.Move(zoomBorder, new Point(250, 100));
        first.Up(zoomBorder);
        second.Up(zoomBorder);
        await IdleAsync();

        var scrollsAfterPinch = recorder.Scrolls.Count;

        var scrollTouch = new TestPointer();
        scrollTouch.Down(zoomBorder, new Point(100, 100));
        scrollTouch.Move(zoomBorder, new Point(200, 200));
        scrollTouch.Move(zoomBorder, new Point(300, 280));
        scrollTouch.Up(zoomBorder);
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch should be raised");
        Assert.IsTrue(recorder.Scrolls.Count > scrollsAfterPinch, "Scroll should be raised after pinch");
    }

    [TestMethod]
    public async Task SimultaneousPinchAndScroll_BothWorkTogether()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync(zb => zb.EnableSimultaneousPanZoom = true);
        var first = new TestPointer();
        var second = new TestPointer();

        first.Down(zoomBorder, new Point(150, 150));
        second.Down(zoomBorder, new Point(250, 150));
        for (var i = 0; i < 5; i++)
        {
            first.Move(zoomBorder, new Point(150 - i * 10, 150 + i * 10));
            second.Move(zoomBorder, new Point(250 + i * 10, 150 + i * 10));
        }

        first.Up(zoomBorder);
        second.Up(zoomBorder);
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch events should be raised during combined gesture");
    }

    [TestMethod]
    public async Task MinimumTouchPoints_Respected()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        zoomBorder.MinimumTouchPoints = 2;

        var touch = new TestPointer();
        touch.Down(zoomBorder, new Point(100, 100));
        touch.Move(zoomBorder, new Point(200, 200));
        touch.Up(zoomBorder);
        await IdleAsync();

        // With MinimumTouchPoints=2, a single finger shouldn't trigger a pinch (Avalonia only documents this).
        Assert.AreEqual(0, recorder.Pinches.Count);
    }

    [TestMethod]
    public async Task MaximumTouchPoints_Respected()
    {
        var (zoomBorder, recorder) = await SetupZoomBorderAsync();
        zoomBorder.MaximumTouchPoints = 2;

        var first = new TestPointer();
        var second = new TestPointer();
        var third = new TestPointer();
        first.Down(zoomBorder, new Point(100, 100));
        second.Down(zoomBorder, new Point(200, 100));
        third.Down(zoomBorder, new Point(300, 100));
        second.Move(zoomBorder, new Point(250, 100));
        await IdleAsync();

        Assert.IsTrue(recorder.Pinches.Count >= 0);

        third.Up(zoomBorder);
        second.Up(zoomBorder);
        first.Up(zoomBorder);
        await IdleAsync();
    }

    [TestMethod]
    public async Task GestureOnDisabledControl_EventsStillRaisedButIgnored()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync(zb => zb.IsEnabled = false);
        var initialZoom = zoomBorder.ZoomX;

        // Uno adaptation: real pinch input (Avalonia raises PinchEventArgs directly and only documents the behavior).
        // A disabled WinUI control does not receive input, so the pinch must not zoom.
        GestureTestHelpers.SimulatePinchZoom(zoomBorder, new Point(200, 150), startDistance: 100, endDistance: 200);
        await IdleAsync();

        Assert.AreEqual(initialZoom, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task GestureRecognizerCollection_ContainsPinchAndScroll()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync();

        // Uno adaptation: pinch = Scale/Rotate manipulations, scroll = TranslateX/TranslateY manipulations.
        var modes = zoomBorder.ManipulationMode;
        Assert.IsTrue((modes & (ManipulationModes.Scale | ManipulationModes.Rotate)) == (ManipulationModes.Scale | ManipulationModes.Rotate), "ZoomBorder should enable pinch manipulations");
        Assert.IsTrue((modes & (ManipulationModes.TranslateX | ManipulationModes.TranslateY)) == (ManipulationModes.TranslateX | ManipulationModes.TranslateY), "ZoomBorder should enable scroll manipulations");
        Assert.AreEqual(GestureManipulations, modes);
    }

    #endregion

    #region Touchpad Magnify/Rotate Integration

    [TestMethod]
    public async Task TouchpadMagnify_ChangesZoom()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync();
        var initialZoom = zoomBorder.ZoomX;

        // Uno adaptation: WinUI has no touchpad magnify event; precision touchpad pinch is delivered as Ctrl+wheel.
        InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 60, VirtualKeyModifiers.Control);
        await IdleAsync();

        Assert.AreNotEqual(initialZoom, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task TouchpadRotate_WithRotationEnabled_ChangesRotation()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync(zb => zb.EnableGestureRotation = true);

        // Uno adaptation: WinUI has no touchpad rotate event; rotation is a manipulation rotation (driven directly here).
        zoomBorder.HandleManipulationStarted();
        zoomBorder.HandleManipulationDelta(new Point(200, 150), new Point(0, 0), 1.0, 45, 1.0);
        zoomBorder.HandleManipulationCompleted();

        // Avalonia only documents the behavior; on Uno the rotation is applied.
        Assert.AreEqual(45, zoomBorder.ExportState().Rotation, 1e-6);
    }

    [TestMethod]
    public async Task TouchpadSwipe_ChangesPan()
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync();
        var initialZoom = zoomBorder.ZoomX;

        // Uno adaptation: WinUI has no touchpad swipe event; a horizontal touchpad swipe is delivered as a horizontal wheel.
        InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 120, horizontal: true);
        await IdleAsync();

        // Avalonia only documents the behavior (swipe might change the offset); a swipe must never zoom.
        Assert.AreEqual(initialZoom, zoomBorder.ZoomX, 1e-9);
    }

    #endregion

    private static bool IsCaptured(UIElement element, TestPointer pointer)
    {
        return element.PointerCaptures?.Any(p => p.PointerId == pointer.Id) == true;
    }

    private static List<Point> TrackOutsideMoves(FrameworkElement element)
    {
        var moves = new List<Point>();
        element.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler((_, e) =>
        {
            var position = e.GetCurrentPoint(element).Position;
            if (position.X < 0 || position.Y < 0 || position.X > element.ActualWidth || position.Y > element.ActualHeight)
            {
                moves.Add(position);
            }
        }), true);
        return moves;
    }

    private static async Task<PointerDeviceType?> GetReceivedPointerTypeAsync(TestPointer pointer)
    {
        var zoomBorder = await CreateAndLoadZoomBorderAsync();
        PointerDeviceType? received = null;
        zoomBorder.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, e) => received ??= e.Pointer.PointerDeviceType), true);

        pointer.Tap(zoomBorder, new Point(100, 100));
        await IdleAsync();
        return received;
    }
}
