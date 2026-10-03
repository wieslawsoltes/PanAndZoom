// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Port of the Avalonia <c>ZoomBorderPointerWheelTests</c> using real injected mouse wheel input
/// (Avalonia <c>Delta.Y = 1</c> equals a WinUI <c>MouseWheelDelta</c> of 120).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderPointerWheelTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<ZoomBorder> CreateAsync(Action<ZoomBorder> configure)
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, configure);
        InputHelper.Reset();
        return zoomBorder;
    }

    [TestMethod]
    public async Task PointerWheel_ZoomIn_IncreasesZoom()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableZoom = true;
            zb.EnablePan = false;
        });

        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(200, 150), 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX, "ZoomX should increase after wheel zoom in");
        Assert.IsTrue(zoomBorder.ZoomY > initialZoomY, "ZoomY should increase after wheel zoom in");
    }

    [TestMethod]
    public async Task PointerWheel_ZoomOut_DecreasesZoom()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableZoom = true;
            zb.EnablePan = false;
        });

        // First zoom in to have something to zoom out from.
        zoomBorder.ZoomTo(2.0, 100, 75);

        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(200, 150), -120);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX < initialZoomX, "ZoomX should decrease after wheel zoom out");
        Assert.IsTrue(zoomBorder.ZoomY < initialZoomY, "ZoomY should decrease after wheel zoom out");
    }

    [TestMethod]
    public async Task PointerWheel_ZoomDisabled_NoZoomChange()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableZoom = false;
            zb.EnablePan = true;
        });

        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // Avalonia raises a single Delta(1, 1) event; a WinUI wheel event is either vertical or horizontal,
        // so both components are injected as separate wheel events.
        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(200, 150), 120);
        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(200, 150), 120, horizontal: true);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
        Assert.AreEqual(initialZoomY, zoomBorder.ZoomY);
        Assert.IsTrue(zoomBorder.OffsetX != initialOffsetX || zoomBorder.OffsetY != initialOffsetY, "Offset should change when panning with wheel");
    }

    [TestMethod]
    public async Task PointerWheel_PanWithWheel_ChangesOffset()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableZoom = false;
            zb.EnablePan = true;
        });

        var initialOffsetX = zoomBorder.OffsetX;

        // Horizontal wheel for horizontal pan.
        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(200, 150), 120, horizontal: true);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.OffsetX != initialOffsetX, "OffsetX should change after horizontal wheel pan");
    }

    [TestMethod]
    public async Task PointerWheel_ZoomToPoint_ZoomsAtCorrectLocation()
    {
        var zoomBorder = await CreateAsync(zb => zb.EnableZoom = true);

        var initialZoom = zoomBorder.ZoomX;

        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(100, 75), 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "Zoom should increase");
    }

    [TestMethod]
    public async Task PointerWheel_BothZoomAndPanDisabled_NoChange()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableZoom = false;
            zb.EnablePan = false;
        });

        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // Delta(1, 1) as a vertical and a horizontal wheel event (see PointerWheel_ZoomDisabled_NoZoomChange).
        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(200, 150), 120);
        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(200, 150), 120, horizontal: true);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
        Assert.AreEqual(initialZoomY, zoomBorder.ZoomY);
        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY);
    }
}
