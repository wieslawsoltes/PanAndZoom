// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Port of the Avalonia <c>ZoomBorderPointerPanTests</c> using real injected mouse input.
/// The Avalonia <c>:isPanning</c> pseudo class is the <see cref="ZoomBorder.IsPanning"/> property on Uno.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderPointerPanTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<ZoomBorder> CreateAsync(bool enablePan, ButtonName panButton)
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnablePan = enablePan;
            zb.PanButton = panButton;
        });
        InputHelper.Reset();
        return zoomBorder;
    }

    [TestMethod]
    public async Task PointerPressed_LeftButton_StartsPanning()
    {
        var zoomBorder = await CreateAsync(true, ButtonName.Left);

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(100, 75), ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Should be panning when panning starts");
    }

    [TestMethod]
    public async Task PointerPressed_RightButton_StartsPanning_WhenConfigured()
    {
        var zoomBorder = await CreateAsync(true, ButtonName.Right);

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(100, 75), ButtonName.Right);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Should be panning when right button panning starts");
    }

    [TestMethod]
    public async Task PointerPressed_MiddleButton_StartsPanning_WhenConfigured()
    {
        var zoomBorder = await CreateAsync(true, ButtonName.Middle);

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(100, 75), ButtonName.Middle);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Should be panning when middle button panning starts");
    }

    [TestMethod]
    public async Task PointerPressed_WrongButton_DoesNotStartPanning()
    {
        var zoomBorder = await CreateAsync(true, ButtonName.Left);

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(100, 75), ButtonName.Right);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsFalse(zoomBorder.IsPanning, "Should not be panning when wrong button is pressed");
    }

    [TestMethod]
    public async Task PointerMoved_WhilePanning_ChangesOffset()
    {
        var zoomBorder = await CreateAsync(true, ButtonName.Left);

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(100, 75), ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // Move 50 pixels right and down while the button is pressed.
        PointerTestHelpers.MouseMoveTo(zoomBorder, new Point(150, 125));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.OffsetX != initialOffsetX || zoomBorder.OffsetY != initialOffsetY,
            "Offset should change when pointer moves during panning");
    }

    [TestMethod]
    public async Task PointerReleased_EndsPanning()
    {
        var zoomBorder = await CreateAsync(true, ButtonName.Left);

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(100, 75), ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Should be panning after press");

        InputHelper.MouseUp(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsFalse(zoomBorder.IsPanning, "Should not be panning after release");
    }

    [TestMethod]
    public async Task PointerPressed_PanDisabled_DoesNotStartPanning()
    {
        var zoomBorder = await CreateAsync(false, ButtonName.Left);

        PointerTestHelpers.MouseDownAt(zoomBorder, new Point(100, 75), ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsFalse(zoomBorder.IsPanning, "Should not start panning when EnablePan is false");
    }

    [TestMethod]
    public async Task PointerMoved_NotPanning_DoesNotChangeOffset()
    {
        var zoomBorder = await CreateAsync(true, ButtonName.Left);

        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // Move the pointer over the control without pressing a button.
        PointerTestHelpers.MouseMoveTo(zoomBorder, new Point(100, 75));
        PointerTestHelpers.MouseMoveTo(zoomBorder, new Point(150, 125));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY);
    }
}
