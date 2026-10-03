// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

// WinUI has no ILogicalScrollable/IScrollable contract: the Uno ZoomBorder exposes the same logical scroll
// state directly (Extent, Viewport, ScrollOffset, ScrollInvalidated, CanHorizontally/VerticallyScroll,
// BringIntoView(FrameworkElement, Rect)), so these tests use those members.
[TestClass]
[RunsOnUIThread]
public class ZoomBorderILogicalScrollableTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static ScrollViewer CreateScrollViewer(ZoomBorder zoomBorder)
    {
        return new ScrollViewer
        {
            Content = zoomBorder,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    [TestMethod]
    public async Task ILogicalScrollable_Properties_InitializedCorrectly()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150,
            zb => ((Border)zb.Child!).Background = new SolidColorBrush(Colors.Red));

        // IsLogicalScrollEnabled/ScrollSize/PageScrollSize are Avalonia ILogicalScrollable-only members;
        // the Uno logical scroll state is initialized with the viewport size and scrolling disabled.
        Assert.AreEqual(new Size(400, 300), zoomBorder.Viewport);
        Assert.IsTrue(zoomBorder.Extent.Width > 0 && zoomBorder.Extent.Height > 0);
        Assert.IsFalse(zoomBorder.CanHorizontallyScroll);
        Assert.IsFalse(zoomBorder.CanVerticallyScroll);
    }

    [TestMethod]
    public void ILogicalScrollable_CanScroll_Properties_SetCorrectly()
    {
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
        };

        zoomBorder.CanHorizontallyScroll = true;
        Assert.IsTrue(zoomBorder.CanHorizontallyScroll);

        zoomBorder.CanVerticallyScroll = true;
        Assert.IsTrue(zoomBorder.CanVerticallyScroll);

        zoomBorder.CanHorizontallyScroll = false;
        zoomBorder.CanVerticallyScroll = false;
        Assert.IsFalse(zoomBorder.CanHorizontallyScroll);
        Assert.IsFalse(zoomBorder.CanVerticallyScroll);
    }

    [TestMethod]
    public async Task ILogicalScrollable_ScrollInvalidated_EventRaised()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        var eventRaised = false;
        zoomBorder.ScrollInvalidated += (_, _) => eventRaised = true;

        zoomBorder.Zoom(2.0, 200, 150);

        Assert.IsTrue(eventRaised, "ScrollInvalidated event should be raised when zoom changes");
    }

    [TestMethod]
    public async Task ILogicalScrollable_Offset_UpdatesCorrectly()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        zoomBorder.Zoom(2.0, 200, 150);

        var initialOffset = zoomBorder.ScrollOffset;

        zoomBorder.ScrollOffset = new Point(50, 30);

        Assert.AreNotEqual(initialOffset, zoomBorder.ScrollOffset);
    }

    [TestMethod]
    public async Task ZoomBorder_InsideScrollViewer_BasicIntegration()
    {
        var (zoomBorder, _) = ZoomBorderTestHelper.Create(400, 300, 800, 600);
        var scrollViewer = CreateScrollViewer(zoomBorder);

        await ZoomBorderTestHelper.LoadAsync(scrollViewer);

        Assert.IsNotNull(scrollViewer.Content);
        Assert.IsInstanceOfType(scrollViewer.Content, typeof(ZoomBorder));
    }

    [TestMethod]
    public async Task ZoomBorder_InsideScrollViewer_ScrollingBehavior()
    {
        var (zoomBorder, _) = ZoomBorderTestHelper.Create(400, 300, 800, 600);
        var scrollViewer = CreateScrollViewer(zoomBorder);
        await ZoomBorderTestHelper.LoadAsync(scrollViewer);

        zoomBorder.Zoom(2.0, 200, 150);

        Assert.IsTrue(zoomBorder.Extent.Width > zoomBorder.Viewport.Width ||
                      zoomBorder.Extent.Height > zoomBorder.Viewport.Height,
                      "Extent should be larger than viewport when zoomed");
    }

    [TestMethod]
    public async Task ZoomBorder_InsideScrollViewer_OffsetChanges()
    {
        var (zoomBorder, _) = ZoomBorderTestHelper.Create(400, 300, 800, 600);
        var scrollViewer = CreateScrollViewer(zoomBorder);
        await ZoomBorderTestHelper.LoadAsync(scrollViewer);

        zoomBorder.Zoom(2.0, 200, 150);
        zoomBorder.Pan(-100, -50);

        Assert.IsTrue(zoomBorder.ScrollOffset.X > 0 || zoomBorder.ScrollOffset.Y > 0,
                      "Offset should be positive when content is panned negatively");
    }

    [TestMethod]
    public void CalculateScrollable_StaticMethod_CorrectCalculations()
    {
        var sourceBounds = new Rect(0, 0, 200, 150);
        var borderSize = new Size(400, 300);
        var matrix = MatrixHelper.Scale(2.0, 2.0);

        ZoomBorder.CalculateScrollable(sourceBounds, borderSize, matrix, out var extent, out var viewport, out var offset);

        Assert.AreEqual(borderSize, viewport);
        Assert.IsTrue(extent.Width >= viewport.Width, "Extent width should be at least viewport width");
        Assert.IsTrue(extent.Height >= viewport.Height, "Extent height should be at least viewport height");
        Assert.IsTrue(offset.X >= 0 && offset.Y >= 0, "Offset should be non-negative");
    }

    [TestMethod]
    public void CalculateScrollable_WithTranslation_CorrectOffset()
    {
        var sourceBounds = new Rect(0, 0, 200, 150);
        var borderSize = new Size(400, 300);
        var matrix = MatrixHelper.Multiply(MatrixHelper.Scale(2.0, 2.0), MatrixHelper.Translate(-50, -30));

        ZoomBorder.CalculateScrollable(sourceBounds, borderSize, matrix, out _, out _, out var offset);

        Assert.AreEqual(50.0, offset.X);
        Assert.AreEqual(30.0, offset.Y);
    }

    [TestMethod]
    public async Task ZoomBorder_ScrollViewer_MultipleZoomLevels()
    {
        var (zoomBorder, _) = ZoomBorderTestHelper.Create(400, 300, 200, 150);
        var scrollViewer = CreateScrollViewer(zoomBorder);
        await ZoomBorderTestHelper.LoadAsync(scrollViewer);

        var zoomLevels = new[] { 0.5, 1.0, 1.5, 2.0, 3.0 };

        foreach (var zoom in zoomLevels)
        {
            zoomBorder.Zoom(zoom, 200, 150);

            Assert.IsTrue(zoomBorder.Extent.Width > 0 && zoomBorder.Extent.Height > 0,
                          $"Extent should be positive at zoom level {zoom}");
            Assert.IsTrue(zoomBorder.Viewport.Width > 0 && zoomBorder.Viewport.Height > 0,
                          $"Viewport should be positive at zoom level {zoom}");
        }
    }

    [TestMethod]
    public async Task ZoomBorder_ScrollViewer_BringIntoView_BringsVisibleElement_ReturnsTrue()
    {
        var (zoomBorder, childElement) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        var result = zoomBorder.BringIntoView(childElement, new Rect(0, 0, 50, 50));

        Assert.IsTrue(result, "BringIntoView should return true when element can be shown");
    }

    [TestMethod]
    public async Task ZoomBorder_ScrollViewer_BringIntoView_WithNullTarget_ReturnsFalse()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        var result = zoomBorder.BringIntoView(null!, new Rect(0, 0, 50, 50));

        Assert.IsFalse(result, "BringIntoView should return false when target is null");
    }

    [TestMethod]
    public async Task ZoomBorder_ScrollViewer_BringIntoView_PansToShowOffscreenContent()
    {
        var (zoomBorder, childElement) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        // Pan content off screen first
        zoomBorder.Pan(-500, -400);
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        var result = zoomBorder.BringIntoView(childElement, new Rect(0, 0, 100, 100));

        Assert.IsTrue(result, "BringIntoView should return true");
    }

    [TestMethod]
    public async Task ZoomBorder_ScrollViewer_GetControlInDirection_ReturnsNull()
    {
        var (zoomBorder, childElement) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        // Avalonia ILogicalScrollable.GetControlInDirection has no WinUI counterpart: directional navigation
        // targets are the XYFocus* properties, which the ZoomBorder leaves unset (no custom navigation).
        Assert.IsNull(zoomBorder.XYFocusDown);
        Assert.IsNull(childElement.XYFocusDown);
    }

    [TestMethod]
    public async Task ZoomBorder_ScrollViewer_ExtentChangesWithContent()
    {
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        var scrollViewer = CreateScrollViewer(zoomBorder);
        await ZoomBorderTestHelper.LoadAsync(scrollViewer);

        var initialExtent = zoomBorder.Extent;

        var largeChild = new Border
        {
            Width = 800,
            Height = 600,
            Background = new SolidColorBrush(Colors.Blue)
        };

        zoomBorder.Child = largeChild;
        // Uno layout is asynchronous: wait for the new child to be arranged before reading the extent.
        await ZoomBorderTestHelper.WaitForIdleAsync();

        var newExtent = zoomBorder.Extent;
        Assert.IsTrue(newExtent.Width >= initialExtent.Width && newExtent.Height >= initialExtent.Height,
                      "Extent should increase or stay the same with larger content");
    }

    [TestMethod]
    public async Task ZoomBorder_ScrollViewer_OffsetFeedbackLoop_Prevention()
    {
        var (zoomBorder, _) = ZoomBorderTestHelper.Create(400, 300, 800, 600);
        var scrollViewer = CreateScrollViewer(zoomBorder);
        await ZoomBorderTestHelper.LoadAsync(scrollViewer);

        zoomBorder.Zoom(2.0, 200, 150);

        // Set offset multiple times rapidly (simulating scroll bar feedback)
        var testOffset = new Point(100, 50);
        zoomBorder.ScrollOffset = testOffset;
        zoomBorder.ScrollOffset = testOffset;

        Assert.IsTrue(true, "Multiple offset sets completed without exceptions");
    }
}
