// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder discrete zoom levels functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderDiscreteZoomLevelsTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public void EnableDiscreteZoomLevels_DefaultValue_IsFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.EnableDiscreteZoomLevels);
    }

    [TestMethod]
    public void DiscreteZoomLevels_DefaultValue_HasStandardLevels()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsNotNull(zoomBorder.DiscreteZoomLevels);
        CollectionAssert.Contains(zoomBorder.DiscreteZoomLevels, 1.0);
        CollectionAssert.Contains(zoomBorder.DiscreteZoomLevels, 2.0);
    }

    [TestMethod]
    public void DiscreteZoomLevels_CanBeSetToCustomLevels()
    {
        var zoomBorder = new ZoomBorder();
        var customLevels = new[] { 0.5, 1.0, 1.5, 2.0, 3.0 };

        zoomBorder.DiscreteZoomLevels = customLevels;

        CollectionAssert.AreEqual(customLevels, zoomBorder.DiscreteZoomLevels);
    }

    [TestMethod]
    public async Task GetNextDiscreteZoomLevel_ReturnsNextLevel()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableDiscreteZoomLevels = true;
            zb.DiscreteZoomLevels = new[] { 0.5, 1.0, 2.0, 4.0 };
            zb.Stretch = StretchMode.None;
        });

        var currentZoom = zoomBorder.ZoomX;

        var nextLevel = zoomBorder.GetNextDiscreteZoomLevel();

        // Next level should be the first level > currentZoom
        var sorted = new[] { 0.5, 1.0, 2.0, 4.0 }.OrderBy(z => z).ToArray();
        var expected = sorted.FirstOrDefault(z => z > currentZoom);
        if (expected == 0) expected = sorted.Last();

        Assert.AreEqual(expected, nextLevel);
    }

    [TestMethod]
    public async Task GetPreviousDiscreteZoomLevel_ReturnsPreviousLevel()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableDiscreteZoomLevels = true;
            zb.DiscreteZoomLevels = new[] { 0.5, 1.0, 2.0, 4.0 };
            zb.Stretch = StretchMode.None;
        });

        zoomBorder.Zoom(2.0, 100, 75);
        var currentZoom = zoomBorder.ZoomX;

        var previousLevel = zoomBorder.GetPreviousDiscreteZoomLevel();

        // Previous level should be the first level < currentZoom
        var sorted = new[] { 0.5, 1.0, 2.0, 4.0 }.OrderByDescending(z => z).ToArray();
        var expected = sorted.FirstOrDefault(z => z < currentZoom);
        if (expected == 0) expected = sorted.Last();

        Assert.AreEqual(expected, previousLevel);
    }

    [TestMethod]
    public async Task ZoomToLevel_ZoomsToSpecificLevel()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableDiscreteZoomLevels = true;
            zb.DiscreteZoomLevels = new[] { 0.5, 1.0, 2.0, 4.0 };
        });

        zoomBorder.ZoomToLevel(2.0, 100, 75);

        Assert.AreEqual(2.0, zoomBorder.ZoomX, 0.01);
    }
}
