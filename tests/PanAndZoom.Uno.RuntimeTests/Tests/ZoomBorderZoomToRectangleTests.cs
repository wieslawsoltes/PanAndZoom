// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder zoom to rectangle functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderZoomToRectangleTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    /// <summary>
    /// Gets the layout offset (render transforms excluded) of <paramref name="child"/> relative to <paramref name="zoomBorder"/>.
    /// </summary>
    private static Point GetLayoutOffset(ZoomBorder zoomBorder, UIElement child)
    {
        double x = 0;
        double y = 0;
        DependencyObject? current = child;
        while (current is UIElement element && !ReferenceEquals(element, zoomBorder))
        {
            x += element.ActualOffset.X;
            y += element.ActualOffset.Y;
            current = VisualTreeHelper.GetParent(current);
        }

        return new Point(x, y);
    }

    [TestMethod]
    public async Task ZoomToRectangle_ZoomsToFitRectangle()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        var rect = new Rect(100, 100, 200, 150);

        zoomBorder.ZoomToRectangle(rect);

        // Should zoom to fit the rectangle
        Assert.IsTrue(zoomBorder.ZoomX >= 1.0, $"Should zoom in to fit rectangle (ZoomX={zoomBorder.ZoomX})");
    }

    [TestMethod]
    public async Task ZoomToRectangleExact_ZoomsToSpecificViewportRect()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 800, 600);

        var contentRect = new Rect(100, 100, 200, 150);
        var viewportRect = new Rect(50, 50, 300, 200);

        zoomBorder.ZoomToRectangleExact(contentRect, viewportRect);

        Assert.IsTrue(zoomBorder.ZoomX > 1.0, $"ZoomX={zoomBorder.ZoomX}");
    }

    [TestMethod]
    public async Task ZoomToRectangle_CentersRectangleInViewport()
    {
        var canvas = new Canvas
        {
            Width = 800,
            Height = 600,
            Background = new SolidColorBrush(Colors.LightGray)
        };
        var zoomBorder = new ZoomBorder
        {
            Width = 600,
            Height = 400,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Stretch = StretchMode.None,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Red region coordinates
        var rect = new Rect(50, 100, 200, 150);

        zoomBorder.ZoomToRectangle(rect);

        // Verify the rectangle center maps to viewport center
        var expectedZoom = Math.Min(600.0 / 200.0, 400.0 / 150.0);
        var rectCenterX = rect.X + rect.Width / 2.0;
        var rectCenterY = rect.Y + rect.Height / 2.0;

        // Uno: Avalonia centers an oversized child ((600-800)/2, (400-600)/2); the WinUI layout offset of the
        // oversized child is read from the actual layout instead of being hard coded.
        var layoutOffset = GetLayoutOffset(zoomBorder, canvas);
        var layoutOffsetX = layoutOffset.X;
        var layoutOffsetY = layoutOffset.Y;

        // Matrix offset = viewportCenter - layoutOffset - contentCenter * zoom
        var expectedOffsetX = 300.0 - layoutOffsetX - rectCenterX * expectedZoom;
        var expectedOffsetY = 200.0 - layoutOffsetY - rectCenterY * expectedZoom;

        Assert.AreEqual(expectedZoom, zoomBorder.ZoomX, 0.01);
        Assert.AreEqual(expectedOffsetX, zoomBorder.OffsetX, 0.01);
        Assert.AreEqual(expectedOffsetY, zoomBorder.OffsetY, 0.01);

        // Verify that the rectangle center point transforms to viewport center
        // Visual position = layoutOffset + matrixOffset + contentPoint * zoom
        var transformedCenterX = layoutOffsetX + rectCenterX * zoomBorder.ZoomX + zoomBorder.OffsetX;
        var transformedCenterY = layoutOffsetY + rectCenterY * zoomBorder.ZoomY + zoomBorder.OffsetY;

        Assert.AreEqual(300.0, transformedCenterX, 0.1);
        Assert.AreEqual(200.0, transformedCenterY, 0.1);

        // Uno: also verify the real visual transform (layout + render transform) maps the center to the viewport center.
        await ZoomBorderTestHelper.WaitForIdleAsync();
        var visualCenter = canvas.TransformToVisual(zoomBorder).TransformPoint(new Point(rectCenterX, rectCenterY));
        Assert.AreEqual(300.0, visualCenter.X, 0.5);
        Assert.AreEqual(200.0, visualCenter.Y, 0.5);
    }
}
