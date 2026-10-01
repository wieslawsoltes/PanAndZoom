// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for view history and undo/redo functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderViewHistoryTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public void EnableViewHistory_DefaultValue_IsTrue()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsTrue(zoomBorder.EnableViewHistory);
    }

    [TestMethod]
    public void EnableViewHistory_CanBeSetToFalse()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.EnableViewHistory = false;

        Assert.IsFalse(zoomBorder.EnableViewHistory);
    }

    [TestMethod]
    public void ViewHistorySize_DefaultValue_Is50()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(50, zoomBorder.ViewHistorySize);
    }

    [TestMethod]
    public void ViewHistorySize_CanBeSetToCustomValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ViewHistorySize = 100;

        Assert.AreEqual(100, zoomBorder.ViewHistorySize);
    }

    [TestMethod]
    public void CanNavigateBack_InitiallyFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.CanNavigateBack);
    }

    [TestMethod]
    public void CanNavigateForward_InitiallyFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.CanNavigateForward);
    }

    [TestMethod]
    public void ViewHistory_AllPropertiesCanBeSetTogether()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableViewHistory = false,
            ViewHistorySize = 25
        };

        Assert.IsFalse(zoomBorder.EnableViewHistory);
        Assert.AreEqual(25, zoomBorder.ViewHistorySize);
    }

    [TestMethod]
    public async Task NavigateBack_AfterZoom_RestoresPreviousState()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableViewHistory = true;
            zb.EnableZoom = true;
            zb.Stretch = StretchMode.None;
        });

        var initialZoom = zoomBorder.ZoomX;

        // Zoom in
        zoomBorder.ZoomIn();
        var zoomedInLevel = zoomBorder.ZoomX;

        Assert.IsTrue(zoomBorder.CanNavigateBack, "Should be able to navigate back after zoom");

        zoomBorder.NavigateBack();

        Assert.AreEqual(initialZoom, zoomBorder.ZoomX, 0.01);
        Assert.IsTrue(zoomBorder.ZoomX < zoomedInLevel, "Should restore previous zoom level");
        Assert.IsTrue(zoomBorder.CanNavigateForward, "Should be able to navigate forward after back");
    }

    [TestMethod]
    public async Task NavigateForward_AfterBackNavigation_RestoresLaterState()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableViewHistory = true;
            zb.EnableZoom = true;
            zb.Stretch = StretchMode.None;
        });

        // Zoom in
        zoomBorder.ZoomIn();
        var zoomedInLevel = zoomBorder.ZoomX;

        // Navigate back
        zoomBorder.NavigateBack();

        zoomBorder.NavigateForward();

        Assert.AreEqual(zoomedInLevel, zoomBorder.ZoomX, 0.01);
        Assert.IsFalse(zoomBorder.CanNavigateForward, "Should not be able to navigate forward at latest state");
    }

    [TestMethod]
    public async Task ViewHistory_MultipleNavigations_MaintainsCorrectHistory()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableViewHistory = true;
            zb.EnableZoom = true;
            zb.EnablePan = true;
            zb.Stretch = StretchMode.None;
        });

        // Perform multiple operations
        var state1Zoom = zoomBorder.ZoomX;
        zoomBorder.ZoomIn();
        var state2Zoom = zoomBorder.ZoomX;
        zoomBorder.ZoomIn();
        var state3Zoom = zoomBorder.ZoomX;

        // Navigate back twice
        zoomBorder.NavigateBack();
        Assert.AreEqual(state2Zoom, zoomBorder.ZoomX, 0.01);

        zoomBorder.NavigateBack();
        Assert.AreEqual(state1Zoom, zoomBorder.ZoomX, 0.01);

        // At the initial state (state1Zoom), cannot navigate back further
        Assert.IsFalse(zoomBorder.CanNavigateBack, "Should not be able to navigate back past initial state");
        Assert.IsTrue(zoomBorder.CanNavigateForward, "Should be able to navigate forward");

        // Navigate forward twice
        zoomBorder.NavigateForward();
        Assert.AreEqual(state2Zoom, zoomBorder.ZoomX, 0.01);

        zoomBorder.NavigateForward();
        Assert.AreEqual(state3Zoom, zoomBorder.ZoomX, 0.01);

        Assert.IsTrue(zoomBorder.CanNavigateBack, "Should be able to navigate back");
        Assert.IsFalse(zoomBorder.CanNavigateForward, "Should not be able to navigate forward beyond latest state");
    }

    [TestMethod]
    public async Task ClearViewHistory_RemovesAllHistory()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableViewHistory = true;
            zb.EnableZoom = true;
            zb.Stretch = StretchMode.None;
        });

        // Add some history
        zoomBorder.ZoomIn();
        zoomBorder.ZoomIn();
        Assert.IsTrue(zoomBorder.CanNavigateBack, "Should have history before clear");

        zoomBorder.ClearViewHistory();

        Assert.IsFalse(zoomBorder.CanNavigateBack, "Should not be able to navigate back after clear");
        Assert.IsFalse(zoomBorder.CanNavigateForward, "Should not be able to navigate forward after clear");
    }

    [TestMethod]
    public async Task ViewHistory_NewOperationAfterBack_ClearsForwardHistory()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableViewHistory = true;
            zb.EnableZoom = true;
            zb.EnablePan = true;
            zb.Stretch = StretchMode.None;
        });

        // Create history
        zoomBorder.ZoomIn();
        zoomBorder.ZoomIn();

        // Navigate back
        zoomBorder.NavigateBack();
        Assert.IsTrue(zoomBorder.CanNavigateForward, "Should be able to navigate forward after back");

        // Perform new operation
        zoomBorder.Pan(10, 10);

        // Forward history should be cleared
        Assert.IsFalse(zoomBorder.CanNavigateForward, "Forward history should be cleared after new operation");
        Assert.IsTrue(zoomBorder.CanNavigateBack, "Should still be able to navigate back");
    }

    [TestMethod]
    public async Task ViewHistory_DisabledWhenEnableViewHistoryIsFalse()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableViewHistory = false;
            zb.EnableZoom = true;
        });

        // Perform operations
        zoomBorder.ZoomIn();
        zoomBorder.ZoomIn();

        // No history should be recorded
        Assert.IsFalse(zoomBorder.CanNavigateBack, "Should not record history when disabled");
    }

    [TestMethod]
    public async Task ViewHistoryChanged_EventRaisedOnNavigateBack()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableViewHistory = true;
            zb.EnableZoom = true;
        });

        zoomBorder.ZoomIn();

        var eventRaised = false;
        zoomBorder.ViewHistoryChanged += (sender, args) => eventRaised = true;

        zoomBorder.NavigateBack();

        Assert.IsTrue(eventRaised, "ViewHistoryChanged event should be raised on NavigateBack");
    }

    [TestMethod]
    public async Task ViewHistoryChanged_EventRaisedOnNavigateForward()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableViewHistory = true;
            zb.EnableZoom = true;
        });

        zoomBorder.ZoomIn();
        zoomBorder.NavigateBack();

        var eventRaised = false;
        zoomBorder.ViewHistoryChanged += (sender, args) => eventRaised = true;

        zoomBorder.NavigateForward();

        Assert.IsTrue(eventRaised, "ViewHistoryChanged event should be raised on NavigateForward");
    }

    [TestMethod]
    public async Task ViewHistoryChanged_EventRaisedOnClearHistory()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableViewHistory = true;
            zb.EnableZoom = true;
        });

        zoomBorder.ZoomIn();

        var eventRaised = false;
        zoomBorder.ViewHistoryChanged += (sender, args) => eventRaised = true;

        zoomBorder.ClearViewHistory();

        Assert.IsTrue(eventRaised, "ViewHistoryChanged event should be raised on ClearViewHistory");
    }
}
