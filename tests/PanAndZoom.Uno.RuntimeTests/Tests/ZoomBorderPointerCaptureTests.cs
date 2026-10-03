// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Port of the Avalonia <c>ZoomBorderPointerCaptureTests</c> using real injected mouse input.
/// Unlike headless Avalonia, pointer capture is observable on Uno through <see cref="UIElement.PointerCaptures"/>,
/// so the capture itself is asserted in addition to <see cref="ZoomBorder.IsPanning"/> (the <c>:isPanning</c> pseudo class).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderPointerCaptureTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<RoutedEventRecorder> CreateAsync(bool enablePan = true, ButtonName panButton = ButtonName.Left)
    {
        var recorder = await RoutedEventRecorder.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnablePan = enablePan;
            zb.PanButton = panButton;
        });
        InputHelper.Reset();
        return recorder;
    }

    [TestMethod]
    public async Task PointerPressed_EnabledPan_CapturesPointer()
    {
        var zoomBorder = (await CreateAsync()).ZoomBorder;

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(200, 150), ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Panning should be active after left mouse button press");
        Assert.AreEqual(1, PointerTestHelpers.GetPointerCaptureCount(zoomBorder), "The mouse pointer should be captured");
    }

    [TestMethod]
    public async Task PointerPressed_DisabledPan_DoesNotCapturePointer()
    {
        var zoomBorder = (await CreateAsync(enablePan: false)).ZoomBorder;

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(200, 150), ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsFalse(zoomBorder.IsPanning, "Panning should not be active when pan is disabled");
        Assert.AreEqual(0, PointerTestHelpers.GetPointerCaptureCount(zoomBorder), "The pointer should not be captured");
    }

    [TestMethod]
    public async Task PointerPressed_WrongButton_DoesNotCapturePointer()
    {
        var zoomBorder = (await CreateAsync(panButton: ButtonName.Left)).ZoomBorder;

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(200, 150), ButtonName.Right);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsFalse(zoomBorder.IsPanning, "Panning should not be active with wrong button");
        Assert.AreEqual(0, PointerTestHelpers.GetPointerCaptureCount(zoomBorder), "The pointer should not be captured");
    }

    [TestMethod]
    public async Task PointerReleased_AfterCapture_ReleasesPointer()
    {
        var zoomBorder = (await CreateAsync()).ZoomBorder;

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(200, 150), ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Panning should be active after mouse down");
        Assert.AreEqual(1, PointerTestHelpers.GetPointerCaptureCount(zoomBorder));

        InputHelper.MouseUp(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsFalse(zoomBorder.IsPanning, "Panning should stop after mouse up");
        Assert.AreEqual(0, PointerTestHelpers.GetPointerCaptureCount(zoomBorder), "The pointer capture should be released");
    }

    [TestMethod]
    public async Task PointerCaptureLost_HandlesEvent()
    {
        var recorder = await CreateAsync();
        var zoomBorder = recorder.ZoomBorder;

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(200, 150), ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Panning should be active after mouse down");

        // Uno: the capture is lost for real (released externally while the button is still pressed)
        // instead of raising a synthetic PointerCaptureLost event.
        zoomBorder.ReleasePointerCaptures();
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.CaptureLostCount > 0, "Pointer capture lost event should be raised");
        Assert.IsTrue(recorder.CaptureLostHandled, "Pointer capture lost event should be handled");
        Assert.IsFalse(zoomBorder.IsPanning, "Panning should stop when the capture is lost");
    }

    [TestMethod]
    public async Task PointerMoved_WithoutCapture_DoesNotPan()
    {
        var zoomBorder = (await CreateAsync()).ZoomBorder;

        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        PointerTestHelpers.MouseMoveTo(zoomBorder, new Point(200, 150));
        PointerTestHelpers.MouseMoveTo(zoomBorder, new Point(100, 100));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY);
        Assert.IsFalse(zoomBorder.IsPanning, "Panning should not be active without prior mouse down");
    }

    [TestMethod]
    public async Task PointerMoved_WithCapture_UpdatesOffset()
    {
        var zoomBorder = (await CreateAsync()).ZoomBorder;

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(200, 150), ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Panning should be active after mouse down");

        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        PointerTestHelpers.MouseMoveTo(zoomBorder, new Point(150, 125));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Panning should still be active during mouse move");
        Assert.AreEqual(1, PointerTestHelpers.GetPointerCaptureCount(zoomBorder), "The pointer should stay captured during the move");
        // With real input the offset follows the pointer delta (-50, -25).
        Assert.AreEqual(initialOffsetX - 50, zoomBorder.OffsetX, 1.0);
        Assert.AreEqual(initialOffsetY - 25, zoomBorder.OffsetY, 1.0);
    }

    [TestMethod]
    public async Task MultiplePointers_OnlyFirstCaptured()
    {
        var zoomBorder = (await CreateAsync()).ZoomBorder;

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(200, 150), ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "First pointer should start panning");
        Assert.AreEqual(1, PointerTestHelpers.GetPointerCaptureCount(zoomBorder), "Only the first pointer should be captured");
    }
}
