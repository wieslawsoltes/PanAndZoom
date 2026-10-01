// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Port of the Avalonia <c>ZoomBorderInteractionScenarioTests</c> using real injected mouse and touch input.
/// </summary>
/// <remarks>
/// Avalonia pinch and scroll gesture events are WinUI touch manipulations on Uno (two fingers pinch, one finger
/// pans), so they are injected as real touch input and "handled" is observed on the bubbling
/// <c>ManipulationDelta</c> event. The <c>:isPanning</c> pseudo class is the <see cref="ZoomBorder.IsPanning"/> property.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderInteractionScenarioTests
{
    private static readonly Point s_center = new(200, 150);

    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<RoutedEventRecorder> CreateAsync(Action<ZoomBorder> configure)
    {
        var recorder = await RoutedEventRecorder.CreateAndLoadAsync(400, 300, 200, 150, configure);
        InputHelper.Reset();
        return recorder;
    }

    [TestMethod]
    public async Task ZoomThenPan_CombinedInteraction_WorksCorrectly()
    {
        var recorder = await CreateAsync(zb =>
        {
            zb.EnableZoom = true;
            zb.EnablePan = true;
            zb.PanButton = ButtonName.Left;
        });
        var zoomBorder = recorder.ZoomBorder;

        var initialZoom = zoomBorder.ZoomX;

        // First zoom in with the mouse wheel.
        PointerTestHelpers.MouseWheelAt(zoomBorder, s_center, 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        var zoomAfterWheel = zoomBorder.ZoomX;

        // Then pan with the pointer.
        PointerTestHelpers.MouseDownAt(zoomBorder, s_center, ButtonName.Left);
        PointerTestHelpers.MouseMoveTo(zoomBorder, new Point(100, 100));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomAfterWheel > initialZoom, "Zoom should increase after wheel event");
        Assert.IsTrue(recorder.WheelHandled, "Wheel event should be handled");
        Assert.IsTrue(zoomBorder.IsPanning, "Panning should be active after mouse down and move");
    }

    [TestMethod]
    public async Task PinchGestureWhilePanning_HandlesCorrectly()
    {
        var recorder = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = true;
            zb.EnablePan = true;
            zb.PanButton = ButtonName.Left;
        });
        var zoomBorder = recorder.ZoomBorder;

        // Start panning with the mouse.
        PointerTestHelpers.MouseDownAt(zoomBorder, s_center, ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        var initialZoom = zoomBorder.ZoomX;

        var pinchGestures = 0;
        zoomBorder.GestureStarted += (_, e) =>
        {
            if (e.GestureType == "Pinch")
            {
                pinchGestures++;
            }
        };

        // Pinch (scale 1.5) while the real mouse pan is active.
        // Uno: a WinUI manipulation tracks a single device type, so while the mouse pointer is pressed on the control
        // injected touch contacts raise no Manipulation* events; the pinch goes through the control manipulation handlers.
        zoomBorder.HandleManipulationStarted();
        zoomBorder.HandleManipulationDelta(s_center, new Point(0, 0), 1.5, 0.0, 1.5);
        zoomBorder.HandleManipulationCompleted();
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Panning should be active after mouse down");
        Assert.AreEqual(1, pinchGestures, "Pinch gesture should be handled");
        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "Zoom should increase from pinch gesture");
    }

    [TestMethod]
    public async Task ScrollGestureAndPointerWheel_BothWork()
    {
        var recorder = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureTranslation = true;
            zb.EnableZoom = true;
        });
        var zoomBorder = recorder.ZoomBorder;

        var initialOffsetX = zoomBorder.OffsetX;
        var initialZoom = zoomBorder.ZoomX;

        // First use a (one finger) scroll gesture for panning.
        PointerTestHelpers.TouchDragAt(zoomBorder, s_center, new Point(s_center.X - 50, s_center.Y));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Then use the pointer wheel for zooming.
        PointerTestHelpers.MouseWheelAt(zoomBorder, s_center, 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.ManipulationDeltaHandled, "Scroll gesture should be handled");
        Assert.IsTrue(recorder.WheelHandled, "Wheel event should be handled");
        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "Zoom should increase");
    }

    [TestMethod]
    public async Task MultiplePointerInteractions_HandleCorrectly()
    {
        var recorder = await CreateAsync(zb =>
        {
            zb.EnablePan = true;
            zb.PanButton = ButtonName.Left;
        });
        var zoomBorder = recorder.ZoomBorder;

        // First pointer interaction: press, move, release.
        PointerTestHelpers.MouseDownAt(zoomBorder, s_center, ButtonName.Left);
        PointerTestHelpers.MouseMoveTo(zoomBorder, new Point(150, 125));
        InputHelper.MouseUp(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsFalse(zoomBorder.IsPanning, "Panning should end after the first release");

        // Second pointer interaction.
        PointerTestHelpers.MouseDownAt(zoomBorder, s_center, ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Panning should be active after mouse interactions");
    }

    [TestMethod]
    public async Task ZoomToLimitThenPan_RespectsBoundaries()
    {
        var recorder = await CreateAsync(zb =>
        {
            zb.EnableZoom = true;
            zb.EnablePan = true;
            zb.PanButton = ButtonName.Left;
            zb.MaxZoomX = 3.0;
            zb.MaxZoomY = 3.0;
        });
        var zoomBorder = recorder.ZoomBorder;

        // Zoom to maximum.
        zoomBorder.ZoomTo(3.0, 100, 75);

        var zoomAfterMax = zoomBorder.ZoomX;

        // Try to zoom further with the wheel (Avalonia Delta.Y = 5), should be limited.
        PointerTestHelpers.MouseWheelAt(zoomBorder, s_center, 5 * 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Then try to pan.
        PointerTestHelpers.MouseDownAt(zoomBorder, s_center, ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(3.0, zoomAfterMax);
        Assert.IsTrue(zoomBorder.ZoomX <= 3.0, "Zoom should not exceed maximum");
        Assert.IsTrue(zoomBorder.IsPanning, "Pan should still work at max zoom");
    }

    [TestMethod]
    public async Task DisableAllInteractions_IgnoresAllEvents()
    {
        var recorder = await CreateAsync(zb =>
        {
            zb.EnableZoom = false;
            zb.EnablePan = false;
            zb.EnableGestures = false;
            zb.Stretch = StretchMode.None;
            zb.EnableConstrains = false;
        });
        var zoomBorder = recorder.ZoomBorder;

        var initialZoom = zoomBorder.ZoomX;
        var initialOffsetX = zoomBorder.OffsetX;

        // Try various interactions: wheel, mouse press and a two finger pinch.
        PointerTestHelpers.MouseWheelAt(zoomBorder, s_center, 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        PointerTestHelpers.MouseDownAt(zoomBorder, s_center, ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        var isPanningAfterPress = zoomBorder.IsPanning;
        InputHelper.MouseUp(ButtonName.Left);

        PointerTestHelpers.PinchAt(zoomBorder, s_center, 100, 150);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialZoom, zoomBorder.ZoomX);
        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(1, recorder.WheelCount, "The wheel event should reach the control");
        Assert.IsFalse(recorder.WheelHandled, "Wheel event should not be handled");
        Assert.IsFalse(isPanningAfterPress, "Panning should not be active when all interactions are disabled");
        Assert.IsFalse(recorder.ManipulationDeltaHandled, "Pinch gesture should not be handled");
    }

    [TestMethod]
    public async Task RapidInteractionSequence_HandlesGracefully()
    {
        var recorder = await CreateAsync(zb =>
        {
            zb.EnableZoom = true;
            zb.EnablePan = true;
            zb.EnableGestures = true;
            zb.PanButton = ButtonName.Left;
        });
        var zoomBorder = recorder.ZoomBorder;

        // Rapid sequence of different interactions.
        for (var i = 0; i < 5; i++)
        {
            // Wheel zoom (Avalonia Delta.Y = 0.2).
            PointerTestHelpers.MouseWheelAt(zoomBorder, s_center, 24);

            // Pinch gesture (scale 1.1).
            PointerTestHelpers.PinchAt(zoomBorder, s_center, 100, 110);

            // Pointer pan.
            PointerTestHelpers.MouseDownAt(zoomBorder, s_center, ButtonName.Left);
            InputHelper.MouseUp(ButtonName.Left);
        }

        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Should not crash and should handle events.
        Assert.IsTrue(zoomBorder.ZoomX >= 1.0, "Zoom should be valid after rapid interactions");
        Assert.IsTrue(zoomBorder.ZoomY >= 1.0, "Zoom should be valid after rapid interactions");
        Assert.IsFalse(zoomBorder.IsPanning, "Panning should end after the last release");
    }
}
