using Avalonia.Controls;
using Avalonia.Media.Transformation;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class ZoomBorderTests
{
    [AvaloniaFact]
    public void ZoomBorder_Ctor()
    {
        var target = new ZoomBorder();
        Assert.NotNull(target);
        Assert.Equal(ButtonName.Middle, target.PanButton);
        Assert.Equal(1.2, target.ZoomSpeed);
        Assert.Equal(StretchMode.Uniform, target.Stretch);
        Assert.Equal(1.0, target.ZoomX);
        Assert.Equal(1.0, target.ZoomY);
        Assert.Equal(0.0, target.OffsetX);
        Assert.Equal(0.0, target.OffsetY);
        Assert.True(target.EnableConstrains);
        Assert.Equal(double.NegativeInfinity, target.MinZoomX);
        Assert.Equal(double.PositiveInfinity, target.MaxZoomX);
        Assert.Equal(double.NegativeInfinity, target.MinZoomY);
        Assert.Equal(double.PositiveInfinity, target.MaxZoomY);
        Assert.Equal(double.NegativeInfinity, target.MinOffsetX);
        Assert.Equal(double.PositiveInfinity, target.MaxOffsetX);
        Assert.Equal(double.NegativeInfinity, target.MinOffsetY);
        Assert.Equal(double.PositiveInfinity, target.MaxOffsetY);
        Assert.True(target.EnablePan);
        Assert.True(target.EnableZoom);
        Assert.True(target.EnableGestureZoom); 
        Assert.True(target.EnableGestureRotation);
        Assert.True(target.EnableGestureTranslation);
    }

    [Fact]
    public void CalculateMatrix_StretchMode_None()
    {
        var panelBounds = new Rect(0, 0, 300, 200);
        var elementBounds = new Rect(0, 0, 100, 100);
        var target = ZoomBorder.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.None);
        Assert.Equal(1.0, target.M11);
        Assert.Equal(0.0, target.M12);
        Assert.Equal(0.0, target.M21);
        Assert.Equal(1.0, target.M22);
        Assert.Equal(0.0, target.M31);
        Assert.Equal(0.0, target.M32);
    }

    [Fact]
    public void CalculateMatrix_StretchMode_Fill()
    {
        var panelBounds = new Rect(0, 0, 300, 200);
        var elementBounds = new Rect(0, 0, 100, 100);
        var target = ZoomBorder.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.Fill);
        Assert.Equal(3.0, target.M11);
        Assert.Equal(0.0, target.M12);
        Assert.Equal(0.0, target.M21);
        Assert.Equal(2.0, target.M22);
        Assert.Equal(-100.0, target.M31);
        Assert.Equal(-50.0, target.M32);
    }

    [Fact]
    public void CalculateMatrix_StretchMode_Uniform()
    {
        var panelBounds = new Rect(0, 0, 300, 200);
        var elementBounds = new Rect(0, 0, 100, 100);
        var target = ZoomBorder.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.Uniform);
        Assert.Equal(2.0, target.M11);
        Assert.Equal(0.0, target.M12);
        Assert.Equal(0.0, target.M21);
        Assert.Equal(2.0, target.M22);
        Assert.Equal(-50.0, target.M31);
        Assert.Equal(-50.0, target.M32);
    }

    [Fact]
    public void CalculateMatrix_StretchMode_UniformToFill()
    {
        var panelBounds = new Rect(0, 0, 300, 200);
        var elementBounds = new Rect(0, 0, 100, 100);
        var target = ZoomBorder.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.UniformToFill);
        Assert.Equal(3.0, target.M11);
        Assert.Equal(0.0, target.M12);
        Assert.Equal(0.0, target.M21);
        Assert.Equal(3.0, target.M22);
        Assert.Equal(-100.0, target.M31);
        Assert.Equal(-100.0, target.M32);
    }

    [AvaloniaFact]
    public void AutoFit_WithStretchNone_Resets_Matrix_ToIdentity()
    {
        var zoomBorder = new ZoomBorder();
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;

        zoomBorder.Stretch = StretchMode.Fill;
        zoomBorder.AutoFit(200, 200, childElement.Width, childElement.Height);

        var matrixAfterFill = zoomBorder.Matrix;
        Assert.NotEqual(Matrix.Identity, matrixAfterFill);

        zoomBorder.Stretch = StretchMode.None;
        zoomBorder.AutoFit(200, 200, childElement.Width, childElement.Height);

        var matrixAfterNone = zoomBorder.Matrix;
        Assert.Equal(Matrix.Identity, matrixAfterNone);
    }

    [Fact]
    public void CalculateScrollable_Default()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 100, 100);
        var matrix = CreateMatrix();
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(300, 300), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 0), offset);
    }

    [Fact]
    public void CalculateScrollable_ViewportSmallerThenContent()
    {
        var borderSize = new Size(100, 100);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix();
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var offset);
        Assert.Equal(new Size(300, 300), extent);
        Assert.Equal(new Size(100, 100), viewport);
        Assert.Equal(new Vector(0, 0), offset);
    }

    [Fact]
    public void CalculateScrollable_OffsetX_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(offsetX: -100);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(400, 300), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(100, 0), offset);
    }
        
    [Fact]
    public void CalculateScrollable_OffsetX_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(offsetX: 100);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(400, 300), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 0), offset);
    }
        
    [Fact]
    public void CalculateScrollable_OffsetY_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(offsetY: -100);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(300, 400), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 100), offset);
    }
        
    [Fact]
    public void CalculateScrollable_OffsetY_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(offsetY: 100);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(300, 400), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 0), offset);
    }
        
    [Fact]
    public void CalculateScrollable_ZoomIn_2x()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 2, scaleY: 2);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(600, 600), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 0), offset);
    }

    [Fact]
    public void CalculateScrollable_ZoomIn_2x_OffsetX_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetX: -150);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(600, 600), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(150, 0), offset);
    }

    [Fact]
    public void CalculateScrollable_ZoomIn_2x_OffsetX_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetX: 150);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(600, 600), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 0), offset);
    }

    [Fact]
    public void CalculateScrollable_ZoomIn_2x_OffsetY_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetY: -150);
        ZoomBorder.CalculateScrollable(bounds,borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(600, 600), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 150), offset);
    }

    [Fact]
    public void CalculateScrollable_ZoomIn_2x_OffsetY_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetY: 150);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(600, 600), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 0), offset);
    }

    [Fact]
    public void CalculateScrollable_ZoomOut_0_5x()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(300, 300), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 0), offset);
    }

    [Fact]
    public void CalculateScrollable_ZoomOut_0_5x_OffsetX_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetX: -200);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(500, 300), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(200, 0), offset);
    }

    [Fact]
    public void CalculateScrollable_ZoomOut_0_5x_OffsetX_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetX: 200);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(350, 300), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 0), offset);
    }

    [Fact]
    public void CalculateScrollable_ZoomOut_0_5x_OffsetY_Negative()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetY: -200);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(300, 500), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 200), offset);
    }

    [Fact]
    public void CalculateScrollable_ZoomOut_0_5x_OffsetY_Positive()
    {
        var borderSize = new Size(300, 300);
        var bounds = new Rect(0, 0, 300, 300);
        var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetY: 200);
        ZoomBorder.CalculateScrollable(bounds, borderSize, matrix, out var extent, out var viewport, out var  offset);
        Assert.Equal(new Size(300, 350), extent);
        Assert.Equal(new Size(300, 300), viewport);
        Assert.Equal(new Vector(0, 0), offset);
    }

    private static Matrix CreateMatrix(double scaleX = 1.0, double scaleY = 1.0, double offsetX = 0.0, double offsetY = 0.0)
    {
        return new Matrix(scaleX, 0, 0, scaleY, offsetX, offsetY);
    }
}
