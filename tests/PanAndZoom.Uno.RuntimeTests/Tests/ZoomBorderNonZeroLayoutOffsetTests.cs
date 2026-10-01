// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder behavior when the child element is smaller than the ZoomBorder, resulting in
/// the layout centering the child with a non-zero layout offset (Avalonia <c>Bounds.Position</c>,
/// WinUI <see cref="UIElement.ActualOffset"/>).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderNonZeroLayoutOffsetTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static Task<(ZoomBorder ZoomBorder, Border Child)> CreateCenteredAsync(
        double width, double height, double childWidth, double childHeight, Action<ZoomBorder>? configure = null)
    {
        // The child Border keeps its default (Stretch) alignment, so a fixed size child is centered by the layout.
        return ZoomBorderTestHelper.CreateAndLoadAsync(width, height, childWidth, childHeight, zb =>
        {
            ((Border)zb.Child!).Background = new SolidColorBrush(Colors.Yellow);
            configure?.Invoke(zb);
        });
    }

    private static bool Intersects(Rect a, Rect b)
    {
        return a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;
    }

    #region Layout Offset Verification

    [TestMethod]
    public async Task ChildSmallerThanZoomBorder_IsCenteredByAvalonia()
    {
        var (zoomBorder, childElement) = await CreateCenteredAsync(600, 400, 200, 100);

        // WinUI centers the child as well, creating a non-zero layout offset
        var expectedOffsetX = (600 - 200) / 2.0; // 200
        var expectedOffsetY = (400 - 100) / 2.0; // 150
        var layoutOffset = LayoutTestHelpers.GetLayoutOffset(childElement, zoomBorder);

        Assert.AreEqual(expectedOffsetX, layoutOffset.X, 0.05);
        Assert.AreEqual(expectedOffsetY, layoutOffset.Y, 0.05);
    }

    [TestMethod]
    public async Task ChildLargerThanZoomBorder_HasNegativeLayoutOffset()
    {
        // WinUI arranges an oversized Stretch-aligned child at the top-left (Avalonia centers it), so the child
        // is explicitly Center aligned to get the centered (negative) layout offset.
        var (zoomBorder, childElement) = await CreateCenteredAsync(400, 300, 600, 500, zb =>
        {
            var child = (Border)zb.Child!;
            child.HorizontalAlignment = HorizontalAlignment.Center;
            child.VerticalAlignment = VerticalAlignment.Center;
        });

        // Child is centered, so the layout position is negative
        var expectedOffsetX = (400 - 600) / 2.0; // -100
        var expectedOffsetY = (300 - 500) / 2.0; // -100
        var layoutOffset = LayoutTestHelpers.GetLayoutOffset(childElement, zoomBorder);

        Assert.AreEqual(expectedOffsetX, layoutOffset.X, 0.05, $"Layout offset {layoutOffset}");
        Assert.AreEqual(expectedOffsetY, layoutOffset.Y, 0.05, $"Layout offset {layoutOffset}");
    }

    #endregion

    #region TransformContentToViewport Helper Tests

    [TestMethod]
    public void TransformContentToViewport_LayoutOffsetAddedAfterTransform()
    {
        var elementBounds = new Rect(100, 50, 200, 150);
        var matrix = Matrix.Identity;

        var result = ZoomBorder.TransformContentToViewport(elementBounds, matrix);

        // With identity matrix, content at (0,0) + layout offset (100,50) = (100, 50)
        Assert.AreEqual(100, result.X, 0.05);
        Assert.AreEqual(50, result.Y, 0.05);
        Assert.AreEqual(200, result.Width, 0.05);
        Assert.AreEqual(150, result.Height, 0.05);
    }

    [TestMethod]
    public void TransformContentToViewport_LayoutOffsetNotScaledByZoom()
    {
        var elementBounds = new Rect(100, 50, 200, 150);
        var matrix = new Matrix(2, 0, 0, 2, 0, 0);

        var result = ZoomBorder.TransformContentToViewport(elementBounds, matrix);

        // Content size scaled 2x, but layout offset NOT scaled
        Assert.AreEqual(100, result.X, 0.05);
        Assert.AreEqual(50, result.Y, 0.05);
        Assert.AreEqual(400, result.Width, 0.05);
        Assert.AreEqual(300, result.Height, 0.05);
    }

    [TestMethod]
    public void TransformContentToViewport_WithContentRectOverload_WorksCorrectly()
    {
        var contentRect = new Rect(50, 25, 100, 75);
        var layoutOffset = new Point(100, 50);
        var matrix = new Matrix(2, 0, 0, 2, 0, 0);

        var result = ZoomBorder.TransformContentToViewport(contentRect, layoutOffset, matrix);

        // Content point (50, 25) * 2 = (100, 50), then + layout offset (100, 50) = (200, 100)
        Assert.AreEqual(200, result.X, 0.05);
        Assert.AreEqual(100, result.Y, 0.05);
        Assert.AreEqual(200, result.Width, 0.05);
        Assert.AreEqual(150, result.Height, 0.05);
    }

    #endregion

    #region CalculateScrollable Tests with Non-Zero Layout Offset

    [TestMethod]
    public void CalculateScrollable_WithNonZeroLayoutOffset_CorrectExtent()
    {
        var borderSize = new Size(600, 400);
        var bounds = new Rect(200, 150, 200, 100);
        var matrix = Matrix.Identity;

        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var offset);

        Assert.AreEqual(new Size(600, 400), extent);
        Assert.AreEqual(new Size(600, 400), viewport);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_WithNonZeroLayoutOffset_Zoomed2x()
    {
        var borderSize = new Size(600, 400);
        var bounds = new Rect(200, 150, 200, 100);
        var matrix = new Matrix(2, 0, 0, 2, 0, 0);

        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var offset);

        // Content starts at (200, 150) and extends to (600, 350): still fits in the viewport
        Assert.AreEqual(new Size(600, 400), viewport);
        Assert.AreEqual(new Size(600, 400), extent);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_WithNonZeroLayoutOffset_PannedNegative()
    {
        var borderSize = new Size(600, 400);
        var bounds = new Rect(200, 150, 200, 100);
        var matrix = new Matrix(1, 0, 0, 1, -300, -200);

        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var offset);

        // Content visual position = layout offset + matrix offset = (-100, -50)
        Assert.AreEqual(new Size(600, 400), viewport);
        Assert.IsTrue(extent.Width >= viewport.Width, "Extent should accommodate scrollable range");
        Assert.IsTrue(offset.X > 0, "Should have positive X scroll offset when content is left of viewport");
        Assert.IsTrue(offset.Y > 0, "Should have positive Y scroll offset when content is above viewport");
    }

    #endregion

    #region Coordinate Conversion Tests with Centered Child

    [TestMethod]
    public async Task ViewportToContent_CenteredChild_CorrectConversion()
    {
        var (zoomBorder, _) = await CreateCenteredAsync(600, 400, 200, 100, zb => zb.Stretch = StretchMode.None);

        var layoutOffsetX = (600 - 200) / 2.0;
        var layoutOffsetY = (400 - 100) / 2.0;

        var contentPoint = zoomBorder.ViewportToContent(new Point(layoutOffsetX, layoutOffsetY));

        // Viewport point at layout offset should map to content origin (0, 0)
        Assert.AreEqual(0, contentPoint.X, 0.05);
        Assert.AreEqual(0, contentPoint.Y, 0.05);
    }

    [TestMethod]
    public async Task ContentToViewport_CenteredChild_CorrectConversion()
    {
        var (zoomBorder, _) = await CreateCenteredAsync(600, 400, 200, 100, zb => zb.Stretch = StretchMode.None);

        var expectedLayoutOffsetX = (600 - 200) / 2.0; // 200
        var expectedLayoutOffsetY = (400 - 100) / 2.0; // 150

        var viewportPoint = zoomBorder.ContentToViewport(new Point(0, 0));

        Assert.AreEqual(expectedLayoutOffsetX, viewportPoint.X, 0.05);
        Assert.AreEqual(expectedLayoutOffsetY, viewportPoint.Y, 0.05);
    }

    [TestMethod]
    public async Task ViewportToContent_RoundTrip_CenteredChild_PreservesPoint()
    {
        var (zoomBorder, _) = await CreateCenteredAsync(600, 400, 200, 100);

        zoomBorder.Zoom(1.5, 100, 50);

        var originalViewportPoint = new Point(350, 220);

        var contentPoint = zoomBorder.ViewportToContent(originalViewportPoint);
        var roundTrippedPoint = zoomBorder.ContentToViewport(contentPoint);

        Assert.AreEqual(originalViewportPoint.X, roundTrippedPoint.X, 0.05);
        Assert.AreEqual(originalViewportPoint.Y, roundTrippedPoint.Y, 0.05);
    }

    #endregion

    #region CenterOn Tests with Centered Child

    [TestMethod]
    public async Task CenterOn_Point_CenteredChild_PointIsCenteredInViewport()
    {
        var (zoomBorder, _) = await CreateCenteredAsync(600, 400, 200, 100);

        var targetContentPoint = new Point(100, 50);

        zoomBorder.CenterOn(targetContentPoint, animate: false);

        var viewportCenter = new Point(300, 200);
        var transformedPoint = zoomBorder.ContentToViewport(targetContentPoint);

        Assert.AreEqual(viewportCenter.X, transformedPoint.X, 0.05);
        Assert.AreEqual(viewportCenter.Y, transformedPoint.Y, 0.05);
    }

    [TestMethod]
    public async Task CenterOn_PointWithZoom_CenteredChild_CorrectPositionAndZoom()
    {
        var (zoomBorder, _) = await CreateCenteredAsync(600, 400, 200, 100);

        var targetContentPoint = new Point(50, 25);
        var targetZoom = 2.0;

        zoomBorder.CenterOn(targetContentPoint, targetZoom, animate: false);

        Assert.AreEqual(targetZoom, zoomBorder.ZoomX, 0.005);
        Assert.AreEqual(targetZoom, zoomBorder.ZoomY, 0.005);

        var viewportCenter = new Point(300, 200);
        var transformedPoint = zoomBorder.ContentToViewport(targetContentPoint);

        Assert.AreEqual(viewportCenter.X, transformedPoint.X, 0.05);
        Assert.AreEqual(viewportCenter.Y, transformedPoint.Y, 0.05);
    }

    #endregion

    #region ZoomToRectangle Tests with Centered Child

    [TestMethod]
    public async Task ZoomToRectangle_CenteredChild_RectangleCenteredInViewport()
    {
        var (zoomBorder, _) = await CreateCenteredAsync(600, 400, 200, 100);

        var targetRect = new Rect(25, 10, 100, 60);

        zoomBorder.ZoomToRectangle(targetRect);

        var rectCenter = new Point(targetRect.X + targetRect.Width / 2.0,
                                   targetRect.Y + targetRect.Height / 2.0);
        var viewportCenter = new Point(300, 200);
        var transformedCenter = zoomBorder.ContentToViewport(rectCenter);

        Assert.AreEqual(viewportCenter.X, transformedCenter.X, 0.05);
        Assert.AreEqual(viewportCenter.Y, transformedCenter.Y, 0.05);
    }

    #endregion

    #region Content Bounds Restriction Tests with Centered Child

    [TestMethod]
    public async Task KeepContentVisible_CenteredChild_ConstrainsCorrectly()
    {
        var (zoomBorder, _) = await CreateCenteredAsync(600, 400, 200, 100, zb =>
        {
            zb.BoundsMode = ContentBoundsMode.KeepContentVisible;
            zb.MinimumVisibleContentPercentage = 0.1;
            zb.EnableConstrains = true;
        });

        zoomBorder.Pan(5000, 5000);

        Assert.IsTrue(zoomBorder.OffsetX < 5000, "OffsetX should be constrained");
        Assert.IsTrue(zoomBorder.OffsetY < 5000, "OffsetY should be constrained");

        var contentVisualBounds = zoomBorder.ContentToViewport(new Rect(0, 0, 200, 100));

        Assert.IsTrue(contentVisualBounds.Right > 0 || contentVisualBounds.Bottom > 0 ||
                      contentVisualBounds.Left < 600 || contentVisualBounds.Top < 400,
                      "Content should still be at least partially visible");
    }

    [TestMethod]
    public async Task KeepCentered_CenteredChild_MaintainsCenter()
    {
        var (zoomBorder, _) = await CreateCenteredAsync(600, 400, 200, 100, zb =>
        {
            zb.BoundsMode = ContentBoundsMode.KeepCentered;
            zb.EnableConstrains = true;
        });

        zoomBorder.Pan(500, 500);

        // Content center (100, 50) should map to viewport center (300, 200)
        var contentCenter = new Point(100, 50);
        var transformedCenter = zoomBorder.ContentToViewport(contentCenter);

        Assert.AreEqual(300, transformedCenter.X, 0.05);
        Assert.AreEqual(200, transformedCenter.Y, 0.05);
    }

    #endregion

    #region BringIntoView Tests with Centered Child

    [TestMethod]
    public async Task BringIntoView_CenteredChild_AlreadyVisible_NoPositionChange()
    {
        var (zoomBorder, childElement) = await CreateCenteredAsync(600, 400, 200, 100);

        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        // Uno logical scroll API equivalent of ILogicalScrollable.BringIntoView
        var result = zoomBorder.BringIntoView(childElement, new Rect(0, 0, 200, 100));

        Assert.IsTrue(result);
        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX, 0.05);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY, 0.05);
        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX, 0.0005);
        Assert.AreEqual(initialZoomY, zoomBorder.ZoomY, 0.0005);
    }

    [TestMethod]
    public async Task BringIntoView_CenteredChild_AfterPanning_ScrollsToTarget()
    {
        var (zoomBorder, childElement) = await CreateCenteredAsync(600, 400, 200, 100,
            zb => zb.BoundsMode = ContentBoundsMode.Unrestricted);

        zoomBorder.Pan(-800, -600);

        // Uno logical scroll API equivalent of ILogicalScrollable.BringIntoView
        var result = zoomBorder.BringIntoView(childElement, new Rect(0, 0, 50, 30));

        Assert.IsTrue(result);

        var targetInViewport = zoomBorder.ContentToViewport(new Rect(0, 0, 50, 30));
        var viewportBounds = new Rect(0, 0, 600, 400);

        Assert.IsTrue(Intersects(viewportBounds, targetInViewport),
            "Target region should be visible in viewport after BringIntoView");
    }

    #endregion
}
