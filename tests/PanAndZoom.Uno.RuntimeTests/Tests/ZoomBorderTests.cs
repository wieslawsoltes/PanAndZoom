// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

[TestClass]
[RunsOnUIThread]
public class ZoomBorderTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public void ZoomBorder_Ctor()
    {
        var target = new ZoomBorder();
        Assert.IsNotNull(target);
        Assert.AreEqual(ButtonName.Middle, target.PanButton);
        Assert.AreEqual(1.2, target.ZoomSpeed);
        Assert.AreEqual(StretchMode.Uniform, target.Stretch);
        Assert.AreEqual(1.0, target.ZoomX);
        Assert.AreEqual(1.0, target.ZoomY);
        Assert.AreEqual(0.0, target.OffsetX);
        Assert.AreEqual(0.0, target.OffsetY);
        Assert.IsTrue(target.EnableConstrains);
        Assert.AreEqual(double.NegativeInfinity, target.MinZoomX);
        Assert.AreEqual(double.PositiveInfinity, target.MaxZoomX);
        Assert.AreEqual(double.NegativeInfinity, target.MinZoomY);
        Assert.AreEqual(double.PositiveInfinity, target.MaxZoomY);
        Assert.AreEqual(double.NegativeInfinity, target.MinOffsetX);
        Assert.AreEqual(double.PositiveInfinity, target.MaxOffsetX);
        Assert.AreEqual(double.NegativeInfinity, target.MinOffsetY);
        Assert.AreEqual(double.PositiveInfinity, target.MaxOffsetY);
        Assert.IsTrue(target.EnablePan);
        Assert.IsTrue(target.EnableZoom);
        Assert.IsTrue(target.EnableGestureZoom);
        Assert.IsTrue(target.EnableGestureRotation);
        Assert.IsTrue(target.EnableGestureTranslation);
    }

    [TestMethod]
    public void CalculateMatrix_StretchMode_None()
    {
        var panelBounds = new Rect(0, 0, 300, 200);
        var elementBounds = new Rect(0, 0, 100, 100);
        var target = ZoomBorder.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.None);
        Assert.AreEqual(1.0, target.M11);
        Assert.AreEqual(0.0, target.M12);
        Assert.AreEqual(0.0, target.M21);
        Assert.AreEqual(1.0, target.M22);
        Assert.AreEqual(0.0, target.OffsetX);
        Assert.AreEqual(0.0, target.OffsetY);
    }

    [TestMethod]
    public void CalculateMatrix_StretchMode_Fill()
    {
        var panelBounds = new Rect(0, 0, 300, 200);
        var elementBounds = new Rect(0, 0, 100, 100);
        var target = ZoomBorder.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.Fill);
        Assert.AreEqual(3.0, target.M11);
        Assert.AreEqual(0.0, target.M12);
        Assert.AreEqual(0.0, target.M21);
        Assert.AreEqual(2.0, target.M22);
        Assert.AreEqual(-100.0, target.OffsetX);
        Assert.AreEqual(-50.0, target.OffsetY);
    }

    [TestMethod]
    public void CalculateMatrix_StretchMode_Uniform()
    {
        var panelBounds = new Rect(0, 0, 300, 200);
        var elementBounds = new Rect(0, 0, 100, 100);
        var target = ZoomBorder.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.Uniform);
        Assert.AreEqual(2.0, target.M11);
        Assert.AreEqual(0.0, target.M12);
        Assert.AreEqual(0.0, target.M21);
        Assert.AreEqual(2.0, target.M22);
        Assert.AreEqual(-50.0, target.OffsetX);
        Assert.AreEqual(-50.0, target.OffsetY);
    }

    [TestMethod]
    public void CalculateMatrix_StretchMode_UniformToFill()
    {
        var panelBounds = new Rect(0, 0, 300, 200);
        var elementBounds = new Rect(0, 0, 100, 100);
        var target = ZoomBorder.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.UniformToFill);
        Assert.AreEqual(3.0, target.M11);
        Assert.AreEqual(0.0, target.M12);
        Assert.AreEqual(0.0, target.M21);
        Assert.AreEqual(3.0, target.M22);
        Assert.AreEqual(-100.0, target.OffsetX);
        Assert.AreEqual(-100.0, target.OffsetY);
    }

    [TestMethod]
    public void CalculateScrollable_Default()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 100, 100);
        var matrix = CreateMatrix();
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(300, 300), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_ViewportSmallerThenContent()
    {
        var borderSize = new Size(100, 100);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix();
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var offset);
        Assert.AreEqual(new Size(300, 300), extent);
        Assert.AreEqual(new Size(100, 100), viewport);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_OffsetX_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(offsetX: -100);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(400, 300), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(100, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_OffsetX_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(offsetX: 100);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(400, 300), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_OffsetY_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(offsetY: -100);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(300, 400), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 100), offset);
    }

    [TestMethod]
    public void CalculateScrollable_OffsetY_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(offsetY: 100);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(300, 400), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_ZoomIn_2x()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 2, scaleY: 2);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(600, 600), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_ZoomIn_2x_OffsetX_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetX: -150);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(600, 600), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(150, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_ZoomIn_2x_OffsetX_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetX: 150);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(600, 600), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_ZoomIn_2x_OffsetY_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetY: -150);
        ZoomBorder.CalculateScrollable(bounds,borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(600, 600), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 150), offset);
    }

    [TestMethod]
    public void CalculateScrollable_ZoomIn_2x_OffsetY_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetY: 150);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(600, 600), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_ZoomOut_0_5x()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(300, 300), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_ZoomOut_0_5x_OffsetX_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetX: -200);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(500, 300), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(200, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_ZoomOut_0_5x_OffsetX_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetX: 200);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(350, 300), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    [TestMethod]
    public void CalculateScrollable_ZoomOut_0_5x_OffsetY_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetY: -200);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(300, 500), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 200), offset);
    }

    [TestMethod]
    public void CalculateScrollable_ZoomOut_0_5x_OffsetY_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetY: 200);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.AreEqual(new Size(300, 350), extent);
        Assert.AreEqual(new Size(300, 300), viewport);
        Assert.AreEqual(new Point(0, 0), offset);
    }

    private static Matrix CreateMatrix(double scaleX = 1.0, double scaleY = 1.0, double offsetX = 0.0, double offsetY = 0.0)
    {
        return new Matrix(scaleX, 0, 0, scaleY, offsetX, offsetY);
    }
}
