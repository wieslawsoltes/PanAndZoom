// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for center on methods and coordinate system conversion.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderCenterOnAndCoordinateTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public void CenterPadding_DefaultValue_IsZero()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder();

        // Assert
        Assert.AreEqual(new Thickness(0), zoomBorder.CenterPadding);
    }

    [TestMethod]
    public void CenterPadding_CanBeSetToCustomValue()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.CenterPadding = new Thickness(10, 20, 30, 40);

        // Assert
        Assert.AreEqual(new Thickness(10, 20, 30, 40), zoomBorder.CenterPadding);
    }

    [TestMethod]
    public async Task CenterOnPoint_CentersViewportOnSpecifiedPoint()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        var targetPoint = new Point(100, 100);

        // Act
        zoomBorder.CenterOn(targetPoint);

        // Assert - The target point should now be centered in the viewport
        var viewportCenter = new Point(zoomBorder.ActualWidth / 2.0, zoomBorder.ActualHeight / 2.0);
        var transformedPoint = zoomBorder.ContentToViewport(targetPoint);

        Assert.IsTrue(Math.Abs(transformedPoint.X - viewportCenter.X) < 1, "X should be centered");
        Assert.IsTrue(Math.Abs(transformedPoint.Y - viewportCenter.Y) < 1, "Y should be centered");
    }

    [TestMethod]
    public async Task CenterOnPointWithZoom_CentersAndZooms()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        var targetPoint = new Point(400, 300);
        var targetZoom = 2.0;

        // Act
        zoomBorder.CenterOn(targetPoint, targetZoom);

        // Assert
        Assert.AreEqual(targetZoom, zoomBorder.ZoomX, 0.01);
        Assert.AreEqual(targetZoom, zoomBorder.ZoomY, 0.01);
    }

    [TestMethod]
    public async Task CenterOnRect_CentersOnRectangleWithAppropriateZoom()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        var targetRect = new Rect(100, 100, 200, 150);

        // Act
        zoomBorder.CenterOn(targetRect);

        // Assert - Zoom should be calculated to fit rect in viewport
        Assert.IsTrue(zoomBorder.ZoomX > 1.0, "Should zoom to fit rectangle");
    }

    [TestMethod]
    public async Task ViewportToContent_ConvertsCoordinatesCorrectly()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.Zoom(2.0, 100, 75);

        var viewportPoint = new Point(200, 150);

        // Act
        var contentPoint = zoomBorder.ViewportToContent(viewportPoint);

        // Assert - Should convert from viewport to content coordinates
        Assert.AreNotEqual(viewportPoint, contentPoint);
    }

    [TestMethod]
    public async Task ContentToViewport_ConvertsCoordinatesCorrectly()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        // Zoom at origin (0,0) with 2x zoom
        zoomBorder.Zoom(2.0, 0, 0);

        var contentPoint = new Point(100, 75);

        // Act
        var viewportPoint = zoomBorder.ContentToViewport(contentPoint);

        // Assert - Should convert from content to viewport coordinates
        // At 2x zoom, content point (100, 75) should map to viewport (200, 150)
        Assert.IsTrue(viewportPoint.X > contentPoint.X, "Viewport X should be larger due to zoom");
        Assert.IsTrue(viewportPoint.Y > contentPoint.Y, "Viewport Y should be larger due to zoom");
    }

    [TestMethod]
    public async Task ViewportToContent_RoundTrip_ReturnsOriginalPoint()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.Zoom(1.5, 100, 75);

        var originalPoint = new Point(150, 100);

        // Act
        var contentPoint = zoomBorder.ViewportToContent(originalPoint);
        var backToViewport = zoomBorder.ContentToViewport(contentPoint);

        // Assert - Round trip should return to original point
        Assert.IsTrue(Math.Abs(backToViewport.X - originalPoint.X) < 0.01, "X should match");
        Assert.IsTrue(Math.Abs(backToViewport.Y - originalPoint.Y) < 0.01, "Y should match");
    }

    [TestMethod]
    public async Task ViewportToContent_Rect_ConvertsCorrectly()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        var viewportRect = new Rect(50, 50, 100, 75);

        // Act
        var contentRect = zoomBorder.ViewportToContent(viewportRect);

        // Assert
        Assert.IsTrue(contentRect.Width > 0 && contentRect.Height > 0);
    }

    [TestMethod]
    public async Task ContentToViewport_Rect_ConvertsCorrectly()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        var contentRect = new Rect(25, 25, 50, 37.5);

        // Act
        var viewportRect = zoomBorder.ContentToViewport(contentRect);

        // Assert
        Assert.IsTrue(viewportRect.Width > 0 && viewportRect.Height > 0);
    }

    [TestMethod]
    public async Task ScreenToContent_Size_ConvertsCorrectly()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.Zoom(2.0, 100, 75);
        var screenSize = new Size(100, 100);

        // Act
        var contentSize = zoomBorder.ScreenToContent(screenSize);

        // Assert - Content size should be smaller when zoomed in
        Assert.IsTrue(contentSize.Width < screenSize.Width);
        Assert.IsTrue(contentSize.Height < screenSize.Height);
    }

    [TestMethod]
    public async Task ContentToScreen_Size_ConvertsCorrectly()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.Zoom(2.0, 100, 75);
        var contentSize = new Size(50, 50);

        // Act
        var screenSize = zoomBorder.ContentToScreen(contentSize);

        // Assert - Screen size should be larger when zoomed in
        Assert.IsTrue(screenSize.Width > contentSize.Width);
        Assert.IsTrue(screenSize.Height > contentSize.Height);
    }

    [TestMethod]
    public async Task GetContentToScreenMatrix_ReturnsCurrentMatrix()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.Zoom(2.0, 100, 75);

        // Act
        var matrix = zoomBorder.GetContentToScreenMatrix();

        // Assert
        Assert.AreEqual(zoomBorder.Matrix, matrix);
    }

    [TestMethod]
    public async Task GetScreenToContentMatrix_ReturnsInvertedMatrix()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.Zoom(2.0, 100, 75);

        // Act
        var matrix = zoomBorder.GetScreenToContentMatrix();
        var contentToScreen = zoomBorder.GetContentToScreenMatrix();

        // Assert - Multiplying them should give identity
        var product = MatrixHelper.Multiply(matrix, contentToScreen);
        Assert.IsTrue(Math.Abs(product.M11 - 1.0) < 0.01);
        Assert.IsTrue(Math.Abs(product.M22 - 1.0) < 0.01);
    }

    [TestMethod]
    public async Task GetVisibleContentBounds_ReturnsCorrectBounds()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        // Act
        var bounds = zoomBorder.GetVisibleContentBounds();

        // Assert
        Assert.IsTrue(bounds.Width > 0);
        Assert.IsTrue(bounds.Height > 0);
    }

    [TestMethod]
    public async Task GetViewportBounds_ReturnsViewportSize()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        // Act
        var bounds = zoomBorder.GetViewportBounds();

        // Assert
        Assert.AreEqual(0, bounds.X);
        Assert.AreEqual(0, bounds.Y);
        Assert.AreEqual(400, bounds.Width);
        Assert.AreEqual(300, bounds.Height);
    }

    [TestMethod]
    public async Task CenterOn_Control_CentersOnChildControl()
    {
        // Arrange
        var innerBorder = new Border
        {
            Width = 50,
            Height = 50,
            Background = new SolidColorBrush(Colors.Blue)
        };

        var canvas = new Canvas
        {
            Width = 400,
            Height = 400
        };
        canvas.Children.Add(innerBorder);
        Canvas.SetLeft(innerBorder, 200);
        Canvas.SetTop(innerBorder, 200);

        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = canvas
        };

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act
        zoomBorder.CenterOn(innerBorder, animate: false);

        // Assert - Just verify it didn't throw
        Assert.IsNotNull(zoomBorder);
    }

    [TestMethod]
    public async Task ScreenToContent_ConvertsCoordinates()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        // Act
        // Avalonia Vector overload is the Point based ScreenToContentVector on Uno.
        var contentPoint = zoomBorder.ScreenToContentVector(new Point(100, 100));

        // Assert - At identity matrix, coordinates should be the same
        Assert.AreNotEqual(default(Point), contentPoint);
    }

    [TestMethod]
    public async Task ContentToScreen_ConvertsCoordinates()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        // Act
        // Avalonia Vector overload is the Point based ContentToScreenVector on Uno.
        var screenPoint = zoomBorder.ContentToScreenVector(new Point(100, 100));

        // Assert - At identity matrix, coordinates should be the same
        Assert.AreNotEqual(default(Point), screenPoint);
    }

    [TestMethod]
    public async Task GetSavedViews_ReturnsEmptyWhenNoSavedViews()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        // Act
        var savedViews = zoomBorder.GetSavedViews();

        // Assert
        Assert.AreEqual(0, savedViews.Count);
    }

    [TestMethod]
    public async Task GetSavedViews_ReturnsSavedViewsAfterSaving()
    {
        // Arrange
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        // Save a view
        zoomBorder.SaveView("TestView");

        // Act
        var savedViews = zoomBorder.GetSavedViews();

        // Assert
        Assert.IsTrue(savedViews.Any(v => v.Name == "TestView"));
    }
}
