// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder dynamic zoom limits functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderDynamicZoomLimitsTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public void AutoCalculateMinZoom_DefaultValue_IsFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.AutoCalculateMinZoom);
    }

    [TestMethod]
    public void AutoCalculateMaxZoom_DefaultValue_IsFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.AutoCalculateMaxZoom);
    }

    [TestMethod]
    public void MaxZoomPixelSize_DefaultValue_Is4()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(4.0, zoomBorder.MaxZoomPixelSize);
    }

    [TestMethod]
    public void AutoCalculateMinZoom_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.AutoCalculateMinZoom = true;

        Assert.IsTrue(zoomBorder.AutoCalculateMinZoom);
    }

    [TestMethod]
    public void AutoCalculateMaxZoom_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.AutoCalculateMaxZoom = true;

        Assert.IsTrue(zoomBorder.AutoCalculateMaxZoom);
    }

    [TestMethod]
    public void MaxZoomPixelSize_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.MaxZoomPixelSize = 8.0;

        Assert.AreEqual(8.0, zoomBorder.MaxZoomPixelSize);
    }

    [TestMethod]
    public async Task AutoCalculateMinZoom_PreventsZoomOutBeyondContentFit()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600, zb =>
        {
            zb.AutoCalculateMinZoom = true;
            zb.Stretch = StretchMode.None;
        });

        // Try to zoom out very far
        for (int i = 0; i < 20; i++)
        {
            zoomBorder.ZoomOut();
        }

        // Should not zoom below the minimum (content fit)
        Assert.IsTrue(zoomBorder.ZoomX >= 0.4, $"Zoom should be limited by auto-calculated minimum (ZoomX={zoomBorder.ZoomX})");
    }

    [TestMethod]
    public async Task AutoCalculateMaxZoom_PreventsZoomInBeyondPixelSize()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.AutoCalculateMaxZoom = true;
            zb.MaxZoomPixelSize = 2.0;
            zb.Stretch = StretchMode.None;
        });

        // Try to zoom in very far
        for (int i = 0; i < 20; i++)
        {
            zoomBorder.ZoomIn();
        }

        // Should not zoom above the maximum pixel size
        Assert.IsTrue(zoomBorder.ZoomX <= 2.1, $"Zoom should be limited by MaxZoomPixelSize (ZoomX={zoomBorder.ZoomX})");
    }
}
