// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder viewport culling functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderViewportCullingTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public async Task IsRectangleVisible_ReturnsTrueForVisibleRectangle()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        var visibleRect = new Rect(50, 50, 100, 100);

        var result = zoomBorder.IsRectangleVisible(visibleRect);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task IsRectangleVisible_ReturnsFalseForNonVisibleRectangle()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        var nonVisibleRect = new Rect(10000, 10000, 100, 100);

        var result = zoomBorder.IsRectangleVisible(nonVisibleRect);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task IsPointVisible_ReturnsTrueForVisiblePoint()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        var visiblePoint = new Point(100, 100);

        var result = zoomBorder.IsPointVisible(visiblePoint);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task GetVisiblePortion_ReturnsIntersectionWithViewport()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        var rect = new Rect(0, 0, 500, 400);

        var visiblePortion = zoomBorder.GetVisiblePortion(rect);

        Assert.IsTrue(visiblePortion.Width > 0);
        Assert.IsTrue(visiblePortion.Height > 0);
        Assert.IsTrue(visiblePortion.Width <= rect.Width);
        Assert.IsTrue(visiblePortion.Height <= rect.Height);
    }
}
