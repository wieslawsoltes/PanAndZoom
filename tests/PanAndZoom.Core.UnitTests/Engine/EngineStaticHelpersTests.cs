// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineStaticHelpersTests
{
    [Fact]
    public void Constructor_Throws_For_Null_Arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new PanAndZoomEngine(null!, new PanAndZoomSettings()));
        Assert.Throws<ArgumentNullException>(() => new PanAndZoomEngine(new TestHost(), null!));
    }

    [Fact]
    public void Constructor_Exposes_Host_And_Settings_And_Initial_State()
    {
        var host = new TestHost();
        var settings = new PanAndZoomSettings();

        var engine = new PanAndZoomEngine(host, settings);

        Assert.Same(host, engine.Host);
        Assert.Same(settings, engine.Settings);
        Assert.Equal(CoreMatrix.Identity, engine.Matrix);
        Assert.Equal(1.0, engine.ZoomX);
        Assert.Equal(1.0, engine.ZoomY);
        Assert.Equal(0.0, engine.OffsetX);
        Assert.Equal(0.0, engine.OffsetY);
        Assert.False(engine.IsPanning);
        Assert.False(engine.IsUpdating);
        Assert.False(engine.IsZoomIndicatorVisible);
        Assert.True(engine.IsAutoFitPending);
        Assert.Empty(engine.ViewHistory);
        Assert.Equal(-1, engine.ViewHistoryIndex);
        Assert.False(engine.CanNavigateBack);
        Assert.False(engine.CanNavigateForward);
    }

    [Fact]
    public void CalculateMatrix_None_Returns_Identity()
    {
        Assert.Equal(CoreMatrix.Identity, PanAndZoomEngine.CalculateMatrix(400, 300, 200, 100, StretchMode.None));
    }

    [Fact]
    public void CalculateMatrix_Fill_Scales_Axes_Independently_Around_Element_Center()
    {
        var matrix = PanAndZoomEngine.CalculateMatrix(400, 300, 200, 100, StretchMode.Fill);

        Assert.Equal(new CoreMatrix(2, 0, 0, 3, -100, -100), matrix);
    }

    [Fact]
    public void CalculateMatrix_Uniform_Uses_Smaller_Ratio()
    {
        var matrix = PanAndZoomEngine.CalculateMatrix(400, 300, 200, 100, StretchMode.Uniform);

        Assert.Equal(new CoreMatrix(2, 0, 0, 2, -100, -50), matrix);
    }

    [Fact]
    public void CalculateMatrix_UniformToFill_Uses_Larger_Ratio()
    {
        var matrix = PanAndZoomEngine.CalculateMatrix(400, 300, 200, 100, StretchMode.UniformToFill);

        Assert.Equal(new CoreMatrix(3, 0, 0, 3, -200, -100), matrix);
    }

    [Fact]
    public void CalculateMatrix_Unknown_Mode_Returns_Identity()
    {
        Assert.Equal(CoreMatrix.Identity, PanAndZoomEngine.CalculateMatrix(400, 300, 200, 100, (StretchMode)42));
    }

    [Fact]
    public void CalculateMatrix_Uniform_Centers_Element_Relative_To_Its_Layout_Slot()
    {
        // A 200x100 element centered in a 400x300 control is laid out at (100,100).
        var matrix = PanAndZoomEngine.CalculateMatrix(400, 300, 200, 100, StretchMode.Uniform);
        var layoutOffset = new CorePoint(100, 100);

        var bounds = PanAndZoomEngine.TransformContentToViewport(new CoreRect(0, 0, 200, 100), layoutOffset, matrix);

        Assert.Equal(new CoreRect(0, 50, 400, 200), bounds);
    }

    [Fact]
    public void TransformContentToViewport_Transforms_And_Adds_Layout_Offset()
    {
        var result = PanAndZoomEngine.TransformContentToViewport(
            new CoreRect(0, 0, 10, 10),
            new CorePoint(5, 6),
            MatrixMath.ScaleAndTranslate(2, 3, 1, 2));

        Assert.Equal(new CoreRect(6, 8, 20, 30), result);
    }

    [Fact]
    public void CalculateScrollable_Content_Smaller_Than_Viewport()
    {
        PanAndZoomEngine.CalculateScrollable(
            new CoreRect(0, 0, 200, 100),
            default,
            new CoreSize(400, 300),
            CoreMatrix.Identity,
            out var extent,
            out var viewport,
            out var offset);

        Assert.Equal(new CoreSize(400, 300), extent);
        Assert.Equal(new CoreSize(400, 300), viewport);
        Assert.Equal(new CoreVector(0, 0), offset);
    }

    [Fact]
    public void CalculateScrollable_Content_Larger_Than_Viewport_Panned()
    {
        PanAndZoomEngine.CalculateScrollable(
            new CoreRect(0, 0, 800, 600),
            default,
            new CoreSize(400, 300),
            MatrixMath.Translate(-50, -30),
            out var extent,
            out var viewport,
            out var offset);

        Assert.Equal(new CoreSize(800, 600), extent);
        Assert.Equal(new CoreSize(400, 300), viewport);
        Assert.Equal(new CoreVector(50, 30), offset);
    }

    [Fact]
    public void CalculateScrollable_Small_Content_Translated_Past_Viewport_Grows_Extent()
    {
        PanAndZoomEngine.CalculateScrollable(
            new CoreRect(0, 0, 200, 100),
            default,
            new CoreSize(400, 300),
            MatrixMath.Translate(300, 250),
            out var extent,
            out _,
            out var offset);

        Assert.Equal(new CoreSize(500, 350), extent);
        Assert.Equal(new CoreVector(0, 0), offset);
    }

    [Fact]
    public void CalculateScrollable_Small_Content_Translated_Negative_Grows_Extent_And_Offset()
    {
        PanAndZoomEngine.CalculateScrollable(
            new CoreRect(0, 0, 200, 100),
            default,
            new CoreSize(400, 300),
            MatrixMath.Translate(-50, -20),
            out var extent,
            out _,
            out var offset);

        Assert.Equal(new CoreSize(450, 320), extent);
        Assert.Equal(new CoreVector(50, 20), offset);
    }

    [Fact]
    public void CalculateScrollable_Content_Equal_To_Viewport_Adds_Translation()
    {
        PanAndZoomEngine.CalculateScrollable(
            new CoreRect(0, 0, 400, 300),
            default,
            new CoreSize(400, 300),
            MatrixMath.Translate(10, -10),
            out var extent,
            out _,
            out var offset);

        Assert.Equal(new CoreSize(410, 310), extent);
        Assert.Equal(new CoreVector(0, 10), offset);
    }

    [Fact]
    public void CalculateScrollable_Accounts_For_Layout_Offset()
    {
        PanAndZoomEngine.CalculateScrollable(
            new CoreRect(0, 0, 200, 100),
            new CorePoint(-100, -40),
            new CoreSize(400, 300),
            CoreMatrix.Identity,
            out var extent,
            out _,
            out var offset);

        Assert.Equal(new CoreSize(500, 340), extent);
        Assert.Equal(new CoreVector(100, 40), offset);
    }

    [Fact]
    public void CalculateScrollable_Zoomed_Content()
    {
        PanAndZoomEngine.CalculateScrollable(
            new CoreRect(0, 0, 400, 300),
            default,
            new CoreSize(400, 300),
            MatrixMath.ScaleAt(2, 2, 200, 150),
            out var extent,
            out _,
            out var offset);

        Assert.Equal(new CoreSize(800, 600), extent);
        Assert.Equal(new CoreVector(200, 150), offset);
    }

    [Theory]
    [InlineData(5, 0, 10, 5)]
    [InlineData(-5, 0, 10, 0)]
    [InlineData(15, 0, 10, 10)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1e9, double.NegativeInfinity, double.PositiveInfinity, 1e9)]
    public void ClampValue_Clamps(double value, double min, double max, double expected)
    {
        Assert.Equal(expected, PanAndZoomEngine.ClampValue(value, min, max));
    }

    [Fact]
    public void ClampValue_Throws_When_Minimum_Greater_Than_Maximum()
    {
        Assert.Throws<ArgumentException>(() => PanAndZoomEngine.ClampValue(1, 10, 0));
    }

    [Theory]
    [InlineData(ZoomIndicatorPosition.TopLeft, 10, 10)]
    [InlineData(ZoomIndicatorPosition.TopRight, 310, 10)]
    [InlineData(ZoomIndicatorPosition.BottomLeft, 10, 260)]
    [InlineData(ZoomIndicatorPosition.BottomRight, 310, 260)]
    [InlineData(ZoomIndicatorPosition.Custom, 310, 260)]
    public void CalculateZoomIndicatorPosition_Returns_Corner(ZoomIndicatorPosition position, double x, double y)
    {
        Assert.Equal(new CorePoint(x, y), PanAndZoomEngine.CalculateZoomIndicatorPosition(position, new CoreSize(400, 300)));
    }
}
