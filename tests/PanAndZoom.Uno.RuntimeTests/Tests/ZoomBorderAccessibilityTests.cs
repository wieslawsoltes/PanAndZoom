// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder accessibility functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderAccessibilityTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public void ZoomLevelDescription_DefaultValue_IsEmpty()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(string.Empty, zoomBorder.ZoomLevelDescription);
    }

    [TestMethod]
    public void PanPositionDescription_DefaultValue_IsEmpty()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(string.Empty, zoomBorder.PanPositionDescription);
    }

    [TestMethod]
    public void UseHighContrastMode_DefaultValue_IsFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.UseHighContrastMode);
    }

    [TestMethod]
    public void ZoomLevelDescription_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ZoomLevelDescription = "Zoom level: 100%";

        Assert.AreEqual("Zoom level: 100%", zoomBorder.ZoomLevelDescription);
    }

    [TestMethod]
    public void PanPositionDescription_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.PanPositionDescription = "Pan position: X=0, Y=0";

        Assert.AreEqual("Pan position: X=0, Y=0", zoomBorder.PanPositionDescription);
    }

    [TestMethod]
    public void UseHighContrastMode_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.UseHighContrastMode = true;

        Assert.IsTrue(zoomBorder.UseHighContrastMode);
    }

    [TestMethod]
    public async Task UpdateAccessibilityDescriptions_UpdatesZoomLevelDescription()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.Zoom(2.0, 100, 75);

        zoomBorder.UpdateAccessibilityDescriptions();

        Assert.IsFalse(string.IsNullOrEmpty(zoomBorder.ZoomLevelDescription));
        StringAssert.Contains(zoomBorder.ZoomLevelDescription, "Zoom level");
        StringAssert.Contains(zoomBorder.ZoomLevelDescription, "200"); // 2.0 * 100 = 200%
    }

    [TestMethod]
    public async Task UpdateAccessibilityDescriptions_UpdatesPanPositionDescription()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.UpdateAccessibilityDescriptions();

        Assert.IsFalse(string.IsNullOrEmpty(zoomBorder.PanPositionDescription));
        StringAssert.Contains(zoomBorder.PanPositionDescription, "Pan position");
    }

    [TestMethod]
    public async Task GetAccessibilityDescription_ReturnsCombinedDescription()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        var description = zoomBorder.GetAccessibilityDescription();

        Assert.IsFalse(string.IsNullOrEmpty(description));
        StringAssert.Contains(description, "Zoom level");
        StringAssert.Contains(description, "Pan position");
    }
}
