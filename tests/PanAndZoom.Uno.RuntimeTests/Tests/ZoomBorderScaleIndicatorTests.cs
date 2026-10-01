// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder scale indicator functionality.
/// The zoom indicator has property support, helper methods, visibility state tracking with auto-hide timer,
/// and IsZoomIndicatorVisible property for UI binding. Visual rendering should be implemented in XAML
/// by binding to IsZoomIndicatorVisible and using GetZoomIndicatorText/GetZoomIndicatorPosition helpers.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderScaleIndicatorTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public void ShowZoomIndicator_DefaultValue_IsFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.ShowZoomIndicator);
    }

    [TestMethod]
    public void ZoomIndicatorPosition_DefaultValue_IsBottomRight()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(ZoomIndicatorPosition.BottomRight, zoomBorder.ZoomIndicatorPosition);
    }

    [TestMethod]
    public void ZoomIndicatorFormat_DefaultValue_IsPercentage()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual("{0:P0}", zoomBorder.ZoomIndicatorFormat);
    }

    [TestMethod]
    public void ZoomIndicatorAutoHideDuration_DefaultValue_Is2Seconds()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(TimeSpan.FromSeconds(2), zoomBorder.ZoomIndicatorAutoHideDuration);
    }

    [TestMethod]
    public void ShowZoomIndicator_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ShowZoomIndicator = true;

        Assert.IsTrue(zoomBorder.ShowZoomIndicator);
    }

    [TestMethod]
    public void ZoomIndicatorPosition_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ZoomIndicatorPosition = ZoomIndicatorPosition.TopLeft;

        Assert.AreEqual(ZoomIndicatorPosition.TopLeft, zoomBorder.ZoomIndicatorPosition);
    }

    [TestMethod]
    public void ZoomIndicatorFormat_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ZoomIndicatorFormat = "{0:F2}x";

        Assert.AreEqual("{0:F2}x", zoomBorder.ZoomIndicatorFormat);
    }

    [TestMethod]
    public async Task GetZoomIndicatorText_ReturnsFormattedZoomLevel()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb => zb.ZoomIndicatorFormat = "{0:P0}");

        var text = zoomBorder.GetZoomIndicatorText();

        Assert.IsNotNull(text);
        Assert.IsFalse(string.IsNullOrEmpty(text));
    }

    [TestMethod]
    public async Task GetZoomIndicatorText_UsesCustomFormat()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb => zb.ZoomIndicatorFormat = "{0:F1}x");

        zoomBorder.Zoom(2.0, 100, 75);

        var text = zoomBorder.GetZoomIndicatorText();

        StringAssert.Contains(text, "2");
        StringAssert.Contains(text, "x");
    }

    /// <summary>
    /// Tests that IsZoomIndicatorVisible property exists for UI binding.
    /// The indicator becomes visible when ShowZoomIndicatorTemporarily() is called during zoom operations.
    /// </summary>
    [TestMethod]
    public void IsZoomIndicatorVisible_DefaultValue_IsFalse()
    {
        var zoomBorder = new ZoomBorder();

        // Initially the indicator is not visible
        Assert.IsFalse(zoomBorder.IsZoomIndicatorVisible);
    }

    /// <summary>
    /// Documents that ShowZoomIndicator controls whether the indicator can be shown.
    /// When ShowZoomIndicator is true, the indicator becomes visible during zoom operations
    /// and auto-hides after ZoomIndicatorAutoHideDuration.
    /// </summary>
    [TestMethod]
    public async Task ShowZoomIndicator_EnablesIndicatorVisibility()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.ShowZoomIndicator = true;
            zb.ZoomIndicatorPosition = ZoomIndicatorPosition.TopLeft;
        });

        // The property is set correctly
        Assert.IsTrue(zoomBorder.ShowZoomIndicator);

        // The helper methods work for UI binding
        var text = zoomBorder.GetZoomIndicatorText();
        Assert.IsFalse(string.IsNullOrEmpty(text));

        // IsZoomIndicatorVisible can be bound in XAML to control visibility
        // It will be set to true when ShowZoomIndicatorTemporarily() is called during zoom
    }

    /// <summary>
    /// Tests that ZoomIndicatorAutoHideDuration controls the auto-hide timer.
    /// The implementation uses a DispatcherQueueTimer that fires after this duration.
    /// </summary>
    [TestMethod]
    public async Task ZoomIndicatorAutoHideDuration_ControlsAutoHideTimer()
    {
        var zoomBorder = new ZoomBorder
        {
            ZoomIndicatorAutoHideDuration = TimeSpan.FromSeconds(5)
        };

        // Property stores value and is used by ShowZoomIndicatorTemporarily()
        Assert.AreEqual(TimeSpan.FromSeconds(5), zoomBorder.ZoomIndicatorAutoHideDuration);

        // Uno: the runtime tests can also verify the timer: the indicator hides after the (short) duration.
        var (loaded, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.ShowZoomIndicator = true;
            zb.ZoomIndicatorAutoHideDuration = TimeSpan.FromMilliseconds(200);
        });
        loaded.Zoom(2.5, 100, 75);
        Assert.IsTrue(loaded.IsZoomIndicatorVisible, "The indicator should be visible after zooming");
        Assert.IsTrue(await ZoomBorderTestHelper.WaitForAsync(() => !loaded.IsZoomIndicatorVisible), "The indicator should auto-hide after ZoomIndicatorAutoHideDuration");
    }

    #region ZoomIndicatorPosition Tests

    [TestMethod]
    public void ZoomIndicatorPosition_AllPositions_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        // Test all enum values
        zoomBorder.ZoomIndicatorPosition = ZoomIndicatorPosition.TopLeft;
        Assert.AreEqual(ZoomIndicatorPosition.TopLeft, zoomBorder.ZoomIndicatorPosition);

        zoomBorder.ZoomIndicatorPosition = ZoomIndicatorPosition.TopRight;
        Assert.AreEqual(ZoomIndicatorPosition.TopRight, zoomBorder.ZoomIndicatorPosition);

        zoomBorder.ZoomIndicatorPosition = ZoomIndicatorPosition.BottomLeft;
        Assert.AreEqual(ZoomIndicatorPosition.BottomLeft, zoomBorder.ZoomIndicatorPosition);

        zoomBorder.ZoomIndicatorPosition = ZoomIndicatorPosition.BottomRight;
        Assert.AreEqual(ZoomIndicatorPosition.BottomRight, zoomBorder.ZoomIndicatorPosition);
    }

    #endregion

    #region ZoomIndicatorFormat Tests

    [TestMethod]
    public async Task GetZoomIndicatorText_WithPercentageFormat_ReturnsPercentage()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb => zb.ZoomIndicatorFormat = "{0:P0}");

        var text = zoomBorder.GetZoomIndicatorText();

        // Should contain "%"
        StringAssert.Contains(text, "%");
    }

    [TestMethod]
    public async Task GetZoomIndicatorText_WithMultiplierFormat_ReturnsMultiplier()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb => zb.ZoomIndicatorFormat = "{0:F2}x");

        zoomBorder.Zoom(2.5, 100, 75);

        var text = zoomBorder.GetZoomIndicatorText();

        StringAssert.Contains(text, "x");
        StringAssert.Contains(text, "2");
    }

    [TestMethod]
    public async Task GetZoomIndicatorText_AfterZoomChange_ReflectsNewZoomLevel()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb => zb.ZoomIndicatorFormat = "{0:F1}x");

        // Get text at initial zoom
        var textInitial = zoomBorder.GetZoomIndicatorText();

        // Zoom to 3x
        zoomBorder.Zoom(3.0, 100, 75);
        var textAt3x = zoomBorder.GetZoomIndicatorText();

        // The text values should be different after zooming
        Assert.AreNotEqual(textInitial, textAt3x);
        StringAssert.Contains(textAt3x, "3");
    }

    #endregion

    #region ZoomIndicatorAutoHideDuration Tests

    [TestMethod]
    public void ZoomIndicatorAutoHideDuration_ZeroDuration_AcceptsValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ZoomIndicatorAutoHideDuration = TimeSpan.Zero;

        Assert.AreEqual(TimeSpan.Zero, zoomBorder.ZoomIndicatorAutoHideDuration);
    }

    [TestMethod]
    public void ZoomIndicatorAutoHideDuration_LargeDuration_AcceptsValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ZoomIndicatorAutoHideDuration = TimeSpan.FromMinutes(5);

        Assert.AreEqual(TimeSpan.FromMinutes(5), zoomBorder.ZoomIndicatorAutoHideDuration);
    }

    #endregion

    #region IsZoomIndicatorVisible Tests

    [TestMethod]
    public void IsZoomIndicatorVisible_Initially_IsFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.IsZoomIndicatorVisible);
    }

    [TestMethod]
    public async Task IsZoomIndicatorVisible_WhenShowZoomIndicatorFalse_RemainsHidden()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb => zb.ShowZoomIndicator = false);

        // Zoom should not show indicator when ShowZoomIndicator is false
        zoomBorder.Zoom(2.0, 100, 75);

        Assert.IsFalse(zoomBorder.IsZoomIndicatorVisible);
    }

    [TestMethod]
    public async Task IsZoomIndicatorVisible_WhenShowZoomIndicatorTrue_BecomesVisibleOnZoom()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.ShowZoomIndicator = true;
            zb.ZoomIndicatorAutoHideDuration = TimeSpan.FromMinutes(5); // Long duration to prevent auto-hide during test
        });

        // Zoom should trigger indicator visibility when ShowZoomIndicator is true
        zoomBorder.Zoom(2.0, 100, 75);

        Assert.IsTrue(zoomBorder.IsZoomIndicatorVisible);
    }

    #endregion
}
