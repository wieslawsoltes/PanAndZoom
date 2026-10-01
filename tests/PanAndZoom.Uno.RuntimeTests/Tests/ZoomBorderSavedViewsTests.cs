// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder saved views functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderSavedViewsTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public async Task SaveView_SavesCurrentViewState()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.ZoomIn();

        zoomBorder.SaveView("TestView", "Test description");

        var view = zoomBorder.GetSavedView("TestView");
        Assert.IsNotNull(view);
        Assert.AreEqual("TestView", view!.Value.Name);
        Assert.AreEqual("Test description", view.Value.Description);
    }

    [TestMethod]
    public async Task RestoreView_RestoresSavedView()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.ZoomIn();
        var savedZoom = zoomBorder.ZoomX;
        zoomBorder.SaveView("TestView");

        // Change the view
        zoomBorder.ResetMatrix();

        var result = zoomBorder.RestoreView("TestView");

        Assert.IsTrue(result);
        Assert.AreEqual(savedZoom, zoomBorder.ZoomX, 0.01);
    }

    [TestMethod]
    public void RestoreView_ReturnsFalseForNonExistentView()
    {
        var zoomBorder = new ZoomBorder();

        var result = zoomBorder.RestoreView("NonExistent");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task GetSavedViewNames_ReturnsAllSavedViewNames()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.SaveView("View1");
        zoomBorder.SaveView("View2");
        zoomBorder.SaveView("View3");

        var names = zoomBorder.GetSavedViewNames();

        Assert.AreEqual(3, names.Length);
        CollectionAssert.Contains(names, "View1");
        CollectionAssert.Contains(names, "View2");
        CollectionAssert.Contains(names, "View3");
    }

    [TestMethod]
    public async Task DeleteSavedView_RemovesView()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.SaveView("TestView");

        var result = zoomBorder.DeleteSavedView("TestView");

        Assert.IsTrue(result);
        Assert.IsNull(zoomBorder.GetSavedView("TestView"));
    }

    [TestMethod]
    public async Task ClearSavedViews_RemovesAllViews()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.SaveView("View1");
        zoomBorder.SaveView("View2");

        zoomBorder.ClearSavedViews();

        Assert.AreEqual(0, zoomBorder.GetSavedViewNames().Length);
    }
}
