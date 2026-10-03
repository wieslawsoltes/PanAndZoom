// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineZoomToRectangleTests
{
    [Fact]
    public void ZoomToRectangle_Fits_And_Centers_Rectangle()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomToRectangle(new CoreRect(100, 100, 100, 50));

        AssertEx.View(harness.Engine, 4, -400, -300);
        AssertEx.Equal(new CorePoint(200, 200), harness.Engine.ContentToViewport(new CorePoint(150, 125)));
        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ZoomToRectangle_Applies_Padding()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomToRectangle(new CoreRect(100, 100, 100, 50), new CoreThickness(50), animate: false);

        AssertEx.View(harness.Engine, 3, -250, -175);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ZoomToRectangle_Applies_Asymmetric_Padding()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomToRectangle(new CoreRect(0, 0, 100, 100), new CoreThickness(100, 0, 0, 0));

        // Available 300x400 -> zoom 3, center x = 100 + 150 = 250, center y = 200.
        AssertEx.View(harness.Engine, 3, 100, 50);
    }

    [Fact]
    public void ZoomToRectangle_Accounts_For_Layout_Offset()
    {
        var harness = EngineHarness.Create(layoutOffset: new CorePoint(10, 20));

        harness.Engine.ZoomToRectangle(new CoreRect(100, 100, 100, 50));

        AssertEx.View(harness.Engine, 4, -410, -320);
        AssertEx.Equal(new CorePoint(200, 200), harness.Engine.ContentToViewport(new CorePoint(150, 125)));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    public void ZoomToRectangle_Ignores_Empty_Rectangle(double width, double height)
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomToRectangle(new CoreRect(0, 0, width, height));
        harness.Engine.ZoomToRectangleExact(new CoreRect(0, 0, width, height), new CoreRect(0, 0, 100, 100));

        Assert.Empty(harness.Host.AppliedTransforms);
        Assert.Single(harness.Engine.ViewHistory);
    }

    [Fact]
    public void ZoomToRectangle_Ignores_Padding_Larger_Than_Viewport()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomToRectangle(new CoreRect(0, 0, 10, 10), new CoreThickness(200));

        Assert.Empty(harness.Host.AppliedTransforms);
        Assert.False(harness.Engine.IsUpdating);
    }

    [Fact]
    public void ZoomToRectangle_Applies_Constraints()
    {
        var harness = EngineHarness.Create(s => s.MaxZoomX = s.MaxZoomY = 2);

        harness.Engine.ZoomToRectangle(new CoreRect(100, 100, 100, 50));

        Assert.Equal(2, harness.Engine.ZoomX);
    }

    [Fact]
    public void ZoomToRectangleExact_Maps_Rectangle_Into_Viewport_Rectangle()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomToRectangleExact(new CoreRect(0, 0, 100, 100), new CoreRect(0, 0, 200, 100), animate: false);

        AssertEx.View(harness.Engine, 1, 50, 0);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ZoomToRectangleExact_Centers_Within_Target_Rectangle()
    {
        var harness = EngineHarness.Create(layoutOffset: new CorePoint(5, 5));

        harness.Engine.ZoomToRectangleExact(new CoreRect(10, 10, 20, 20), new CoreRect(100, 100, 100, 100));

        AssertEx.View(harness.Engine, 5, 150 - 5 - 100, 150 - 5 - 100);
        AssertEx.Equal(new CorePoint(150, 150), harness.Engine.ContentToViewport(new CorePoint(20, 20)));
    }

    [Fact]
    public void ZoomToRectangleExact_Suppresses_Constraints_Only_For_That_Operation()
    {
        var harness = EngineHarness.Create(s => s.MaxZoomX = s.MaxZoomY = 2);

        harness.Engine.ZoomToRectangleExact(new CoreRect(0, 0, 50, 50), new CoreRect(0, 0, 400, 400));

        AssertEx.View(harness.Engine, 8, 0, 0);

        harness.Engine.PanDelta(1, 0);

        Assert.Equal(2, harness.Engine.ZoomX);
    }

    [Fact]
    public void ZoomToRectangleExact_Does_Not_Validate_Custom_Bounds()
    {
        var harness = EngineHarness.Create(s => s.BoundsMode = ContentBoundsMode.Custom);
        harness.Host.ValidateTransformOverride = _ => false;
        harness.Host.ClearRecords();

        harness.Engine.ZoomToRectangleExact(new CoreRect(0, 0, 50, 50), new CoreRect(0, 0, 400, 400));

        Assert.Empty(harness.Host.ValidatedMatrices);
        Assert.Equal(8, harness.Engine.ZoomX);
    }
}

public class EngineCenterOnTests
{
    [Fact]
    public void CenterOn_Point_Keeps_Zoom_And_Centers_Point()
    {
        var harness = EngineHarness.Create();

        harness.Engine.CenterOn(new CorePoint(100, 100));

        AssertEx.View(harness.Engine, 1, 100, 100);
        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void CenterOn_Point_At_Zoom()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.CenterOn(new CorePoint(50, 50), animate: false);

        AssertEx.View(harness.Engine, 2, 100, 100);
        AssertEx.Equal(new CorePoint(200, 200), harness.Engine.ContentToViewport(new CorePoint(50, 50)));
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void CenterOn_Point_Uses_CenterPadding()
    {
        var harness = EngineHarness.Create(s => s.CenterPadding = new CoreThickness(100, 0, 0, 50));

        harness.Engine.CenterOn(new CorePoint(100, 100));

        // Center x = (400 - 100) / 2 + 100 = 250, center y = (400 - 50) / 2 = 175.
        AssertEx.View(harness.Engine, 1, 150, 75);
    }

    [Fact]
    public void CenterOn_Point_Accounts_For_Layout_Offset()
    {
        var harness = EngineHarness.Create(layoutOffset: new CorePoint(10, 20));

        harness.Engine.CenterOn(new CorePoint(100, 100));

        AssertEx.View(harness.Engine, 1, 90, 80);
        AssertEx.Equal(new CorePoint(200, 200), harness.Engine.ContentToViewport(new CorePoint(100, 100)));
    }

    [Fact]
    public void CenterOn_Point_With_Zoom_Sets_Zoom()
    {
        var harness = EngineHarness.Create();

        harness.Engine.CenterOn(new CorePoint(100, 100), 2, animate: false);

        AssertEx.View(harness.Engine, 2, 0, 0);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void CenterOn_Point_With_Zoom_Uses_CenterPadding_And_Layout_Offset()
    {
        var harness = EngineHarness.Create(s => s.CenterPadding = new CoreThickness(0, 100, 0, 0), layoutOffset: new CorePoint(10, 10));

        harness.Engine.CenterOn(new CorePoint(100, 100), 2);

        // Center = (200, 250).
        AssertEx.View(harness.Engine, 2, 200 - 10 - 200, 250 - 10 - 200);
    }

    [Fact]
    public void CenterOn_Rect_Fits_And_Centers()
    {
        var harness = EngineHarness.Create();

        harness.Engine.CenterOn(new CoreRect(0, 0, 200, 100));

        AssertEx.View(harness.Engine, 2, 0, 100);
    }

    [Fact]
    public void CenterOn_Rect_Uses_CenterPadding()
    {
        var harness = EngineHarness.Create(s => s.CenterPadding = new CoreThickness(50));

        harness.Engine.CenterOn(new CoreRect(0, 0, 150, 100));

        // Available 300x300 -> zoom 2, center content (75, 50) at viewport (200, 200).
        AssertEx.View(harness.Engine, 2, 50, 100);
    }
}

public class EngineCoordinateTests
{
    private static EngineHarness CreateTransformed()
    {
        var harness = EngineHarness.Create(layoutOffset: new CorePoint(5, 5));
        harness.SetMatrix(2, 10, 20);
        return harness;
    }

    [Fact]
    public void ContentToViewport_And_Back_For_Points()
    {
        var harness = CreateTransformed();

        var viewport = harness.Engine.ContentToViewport(new CorePoint(10, 10));

        AssertEx.Equal(new CorePoint(35, 45), viewport);
        AssertEx.Equal(new CorePoint(10, 10), harness.Engine.ViewportToContent(viewport));
    }

    [Fact]
    public void ContentToViewport_And_Back_For_Rects()
    {
        var harness = CreateTransformed();

        var viewport = harness.Engine.ContentToViewport(new CoreRect(0, 0, 10, 10));

        AssertEx.Equal(new CoreRect(15, 25, 20, 20), viewport);
        AssertEx.Equal(new CoreRect(0, 0, 10, 10), harness.Engine.ViewportToContent(viewport));
    }

    [Fact]
    public void Vector_Conversions_Ignore_Translation()
    {
        var harness = CreateTransformed();

        AssertEx.Equal(new CoreVector(10, 20), harness.Engine.ScreenToContent(new CoreVector(20, 40)));
        AssertEx.Equal(new CoreVector(20, 40), harness.Engine.ContentToScreen(new CoreVector(10, 20)));
    }

    [Fact]
    public void Size_Conversions_Use_Zoom()
    {
        var harness = EngineHarness.Create(s => s.EnableConstrains = false);
        harness.Engine.SetMatrix(MatrixMath.ScaleAndTranslate(2, 4, 10, 20));

        Assert.Equal(new CoreSize(10, 5), harness.Engine.ScreenToContent(new CoreSize(20, 20)));
        Assert.Equal(new CoreSize(20, 40), harness.Engine.ContentToScreen(new CoreSize(10, 10)));
    }

    [Fact]
    public void GetScreenToContentMatrix_Is_Inverse_Of_Matrix()
    {
        var harness = CreateTransformed();

        AssertEx.Equal(CoreMatrix.Identity, harness.Engine.Matrix * harness.Engine.GetScreenToContentMatrix());
        AssertEx.Equal(new CoreMatrix(0.5, 0, 0, 0.5, -5, -10), harness.Engine.GetScreenToContentMatrix());
    }

    [Fact]
    public void Conversions_With_Non_Invertible_Matrix_Return_Input()
    {
        var harness = EngineHarness.Create(s => s.EnableConstrains = false);
        harness.Engine.SetMatrix(new CoreMatrix(0, 0, 0, 0, 0, 0));

        Assert.Equal(new CorePoint(3, 4), harness.Engine.ViewportToContent(new CorePoint(3, 4)));
        Assert.Equal(new CoreRect(1, 2, 3, 4), harness.Engine.ViewportToContent(new CoreRect(1, 2, 3, 4)));
        Assert.Equal(new CoreVector(3, 4), harness.Engine.ScreenToContent(new CoreVector(3, 4)));
        Assert.Equal(default, harness.Engine.GetScreenToContentMatrix());
    }

    [Fact]
    public void Layout_Offset_Is_Ignored_Without_Element()
    {
        var harness = EngineHarness.Create(layoutOffset: new CorePoint(5, 5));
        harness.Detach();

        Assert.Equal(new CorePoint(10, 10), harness.Engine.ContentToViewport(new CorePoint(10, 10)));
        Assert.Equal(new CorePoint(10, 10), harness.Engine.ViewportToContent(new CorePoint(10, 10)));
    }
}

public class EngineVisibilityTests
{
    private static EngineHarness CreateZoomed()
    {
        var harness = EngineHarness.Create();
        harness.SetMatrix(2, -100, -100);
        return harness;
    }

    [Fact]
    public void GetViewportBounds_Is_Viewport_Size()
    {
        Assert.Equal(new CoreRect(0, 0, 400, 400), CreateZoomed().Engine.GetViewportBounds());
    }

    [Fact]
    public void GetVisibleContentBounds_Inverts_Transform()
    {
        AssertEx.Equal(new CoreRect(50, 50, 200, 200), CreateZoomed().Engine.GetVisibleContentBounds());
    }

    [Fact]
    public void GetVisibleContentBounds_Accounts_For_Layout_Offset()
    {
        var harness = EngineHarness.Create(layoutOffset: new CorePoint(100, 0));
        harness.SetMatrix(2, -100, -100);

        AssertEx.Equal(new CoreRect(0, 50, 200, 200), harness.Engine.GetVisibleContentBounds());
    }

    [Theory]
    [InlineData(50, 50, true)]
    [InlineData(250, 250, true)]
    [InlineData(150, 100, true)]
    [InlineData(49, 50, false)]
    [InlineData(251, 100, false)]
    public void IsPointVisible(double x, double y, bool expected)
    {
        Assert.Equal(expected, CreateZoomed().Engine.IsPointVisible(new CorePoint(x, y)));
    }

    [Fact]
    public void IsRectangleVisible_Uses_Strict_Intersection()
    {
        var engine = CreateZoomed().Engine;

        Assert.True(engine.IsRectangleVisible(new CoreRect(0, 0, 51, 51)));
        Assert.True(engine.IsRectangleVisible(new CoreRect(100, 100, 10, 10)));
        Assert.False(engine.IsRectangleVisible(new CoreRect(0, 0, 50, 50)));
        Assert.False(engine.IsRectangleVisible(new CoreRect(300, 300, 10, 10)));
    }

    [Fact]
    public void GetVisiblePortion_Returns_Intersection()
    {
        var engine = CreateZoomed().Engine;

        AssertEx.Equal(new CoreRect(50, 50, 50, 50), engine.GetVisiblePortion(new CoreRect(0, 0, 100, 100)));
        Assert.Equal(default, engine.GetVisiblePortion(new CoreRect(300, 300, 10, 10)));
    }

    [Fact]
    public void BringIntoView_Fully_Visible_Target_Does_Not_Pan()
    {
        var harness = EngineHarness.Create();

        Assert.True(harness.Engine.BringIntoView(new CoreRect(10, 10, 50, 50)));

        Assert.Empty(harness.Host.AppliedTransforms);
        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void BringIntoView_Pans_Right_And_Bottom_Edges_Into_View()
    {
        var harness = EngineHarness.Create();

        Assert.True(harness.Engine.BringIntoView(new CoreRect(500, 420, 50, 50)));

        AssertEx.View(harness.Engine, 1, -150, -70);
    }

    [Fact]
    public void BringIntoView_Pans_Left_And_Top_Edges_Into_View()
    {
        var harness = EngineHarness.Create();

        Assert.True(harness.Engine.BringIntoView(new CoreRect(-100, -50, 20, 20)));

        AssertEx.View(harness.Engine, 1, 100, 50);
    }

    [Fact]
    public void BringIntoView_Uses_Zoom_And_Layout_Offset()
    {
        var harness = EngineHarness.Create(layoutOffset: new CorePoint(10, 0));
        harness.SetMatrix(2, 0, 0);

        Assert.True(harness.Engine.BringIntoView(new CoreRect(200, 0, 10, 10)));

        // Target in viewport: (410, 0, 20, 20) -> pan by -30 so its right edge is at 400.
        AssertEx.View(harness.Engine, 2, -30, 0);
    }

    [Fact]
    public void BringIntoView_Target_Larger_Than_Viewport_Prefers_Left_Top_Edge_When_It_Is_Out_Of_View()
    {
        var harness = EngineHarness.Create();
        harness.Engine.PanDelta(-50, -60);

        Assert.True(harness.Engine.BringIntoView(new CoreRect(0, 0, 1000, 1000)));

        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void BringIntoView_Target_Larger_Than_Viewport_Aligns_Right_Bottom_Edge_When_Left_Top_Is_Visible()
    {
        var harness = EngineHarness.Create();
        harness.Engine.PanDelta(50, 60);

        Assert.True(harness.Engine.BringIntoView(new CoreRect(0, 0, 1000, 1000)));

        AssertEx.View(harness.Engine, 1, -600, -600);
    }
}
