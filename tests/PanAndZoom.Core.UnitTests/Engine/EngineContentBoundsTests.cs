// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineContentBoundsTests
{
    private static EngineHarness Create(ContentBoundsMode mode, Action<PanAndZoomSettings>? configure = null, CorePoint layoutOffset = default, CoreSize? element = null)
    {
        return EngineHarness.Create(
            s =>
            {
                s.Stretch = StretchMode.None;
                s.BoundsMode = mode;
                configure?.Invoke(s);
            },
            new CoreSize(400, 400),
            element ?? new CoreSize(200, 200),
            layoutOffset);
    }

    [Fact]
    public void KeepContentVisible_Clamps_Offsets_To_Keep_Minimum_Content_Visible()
    {
        var harness = Create(ContentBoundsMode.KeepContentVisible);

        harness.Engine.PanDelta(1000, 1000);
        AssertEx.View(harness.Engine, 1, 380, 380);

        harness.Engine.PanDelta(-5000, -5000);
        AssertEx.View(harness.Engine, 1, -180, -180);
    }

    [Fact]
    public void KeepContentVisible_Uses_MinimumVisibleContentPercentage()
    {
        var harness = Create(ContentBoundsMode.KeepContentVisible, s => s.MinimumVisibleContentPercentage = 0.5);

        harness.Engine.PanDelta(1000, -1000);

        AssertEx.View(harness.Engine, 1, 300, -100);
    }

    [Fact]
    public void KeepContentVisible_Scales_With_Zoom()
    {
        var harness = Create(ContentBoundsMode.KeepContentVisible);
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.PanDelta(-5000, 5000);

        // Content is 400 wide, 10% = 40 must stay visible.
        AssertEx.View(harness.Engine, 2, -360, 360);
    }

    [Fact]
    public void KeepContentVisible_Applies_Padding()
    {
        var harness = Create(ContentBoundsMode.KeepContentVisible, s => s.BoundsPadding = new CoreThickness(10, 20, 30, 40));

        harness.Engine.PanDelta(1000, 1000);
        AssertEx.View(harness.Engine, 1, 410, 420);

        harness.Engine.PanDelta(-5000, -5000);
        AssertEx.View(harness.Engine, 1, -190, -200);
    }

    [Fact]
    public void KeepContentVisible_Accounts_For_Layout_Offset()
    {
        var harness = Create(ContentBoundsMode.KeepContentVisible, layoutOffset: new CorePoint(50, 60));

        harness.Engine.PanDelta(1000, 1000);
        AssertEx.View(harness.Engine, 1, 330, 320);

        harness.Engine.PanDelta(-5000, -5000);
        AssertEx.View(harness.Engine, 1, -230, -240);
    }

    [Fact]
    public void FillViewport_Centers_Content_Smaller_Than_Viewport()
    {
        var harness = Create(ContentBoundsMode.FillViewport);

        AssertEx.View(harness.Engine, 1, 100, 100);

        harness.Engine.PanDelta(50, -70);

        AssertEx.View(harness.Engine, 1, 100, 100);
    }

    [Fact]
    public void FillViewport_Prevents_Empty_Space_For_Large_Content()
    {
        var harness = Create(ContentBoundsMode.FillViewport);

        harness.SetMatrix(4, 100, -1000);

        AssertEx.View(harness.Engine, 4, 0, -400);
    }

    [Fact]
    public void FillViewport_Applies_Padding()
    {
        var harness = Create(ContentBoundsMode.FillViewport, s => s.BoundsPadding = new CoreThickness(10, 20, 30, 40));

        harness.SetMatrix(4, 100, -1000);
        AssertEx.View(harness.Engine, 4, 30, -420);

        harness.SetMatrix(4, -1000, 100);
        AssertEx.View(harness.Engine, 4, -410, 40);
    }

    [Fact]
    public void FillViewport_Accounts_For_Layout_Offset()
    {
        var harness = Create(ContentBoundsMode.FillViewport, layoutOffset: new CorePoint(50, 60));

        AssertEx.View(harness.Engine, 1, 50, 40);

        harness.SetMatrix(4, 100, -1000);
        AssertEx.View(harness.Engine, 4, -50, -460);
    }

    [Fact]
    public void FillViewport_Handles_Axes_Independently()
    {
        var harness = Create(ContentBoundsMode.FillViewport, element: new CoreSize(800, 100));

        harness.SetMatrix(1, 100, 100);

        // Width 800 > 400 is clamped to [-400, 0], height 100 < 400 is centered.
        AssertEx.View(harness.Engine, 1, 0, 150);
    }

    [Theory]
    [InlineData(1, 100, 100)]
    [InlineData(2, 0, 0)]
    [InlineData(4, -200, -200)]
    public void KeepCentered_Always_Centers_Content(double zoom, double expectedX, double expectedY)
    {
        var harness = Create(ContentBoundsMode.KeepCentered);

        harness.SetMatrix(zoom, 1234, -987);

        AssertEx.View(harness.Engine, zoom, expectedX, expectedY);
    }

    [Fact]
    public void KeepCentered_Ignores_Panning()
    {
        var harness = Create(ContentBoundsMode.KeepCentered);

        harness.Engine.PanDelta(10, 10);

        AssertEx.View(harness.Engine, 1, 100, 100);
    }

    [Fact]
    public void KeepCentered_Accounts_For_Layout_Offset()
    {
        var harness = Create(ContentBoundsMode.KeepCentered, layoutOffset: new CorePoint(50, 60));

        AssertEx.View(harness.Engine, 1, 50, 40);
    }

    [Fact]
    public void Custom_Centers_Small_Custom_Bounds()
    {
        var host = new TestHost { ContentBoundsOverride = () => new CoreRect(50, 50, 100, 100) };
        var harness = EngineHarness.Create(
            s =>
            {
                s.Stretch = StretchMode.None;
                s.BoundsMode = ContentBoundsMode.Custom;
            },
            new CoreSize(400, 400),
            new CoreSize(200, 200),
            host: host);

        // The custom rectangle (50,50,100,100) is centered in the 400x400 viewport.
        AssertEx.View(harness.Engine, 1, 100, 100);

        harness.Engine.PanDelta(25, -25);

        AssertEx.View(harness.Engine, 1, 100, 100);
        Assert.True(host.GetContentBoundsCalls > 0);
    }

    [Fact]
    public void Custom_Clamps_Large_Custom_Bounds_To_Viewport()
    {
        var host = new TestHost { ContentBoundsOverride = () => new CoreRect(50, 50, 100, 100) };
        var harness = EngineHarness.Create(
            s =>
            {
                s.Stretch = StretchMode.None;
                s.BoundsMode = ContentBoundsMode.Custom;
            },
            new CoreSize(400, 400),
            new CoreSize(200, 200),
            host: host);

        harness.SetMatrix(8, 0, 0);

        // Custom rect in viewport is (400,400,800,800) -> clamped so its left/top edge is at 0.
        AssertEx.View(harness.Engine, 8, -400, -400);

        harness.SetMatrix(8, -2000, -2000);

        // Clamped so its right/bottom edge is at the viewport edge.
        AssertEx.View(harness.Engine, 8, -800, -800);
    }

    [Fact]
    public void Custom_Default_Content_Bounds_Accounts_For_Layout_Offset()
    {
        var harness = Create(ContentBoundsMode.Custom, layoutOffset: new CorePoint(100, 100));

        AssertEx.View(harness.Engine, 1, 0, 0);

        harness.SetMatrix(4, 0, 0);

        AssertEx.View(harness.Engine, 4, -100, -100);
    }

    [Fact]
    public void Custom_ValidateTransform_Can_Veto_Transform()
    {
        var harness = Create(ContentBoundsMode.Custom, element: new CoreSize(400, 400));
        harness.Host.ValidateTransformOverride = m => m.M11 <= 1.5;

        harness.Engine.ZoomTo(1.5, 200, 200);
        AssertEx.View(harness.Engine, 1.5, -100, -100);

        harness.Engine.ZoomTo(2, 200, 200);

        // The vetoed matrix is replaced by the last valid matrix.
        AssertEx.View(harness.Engine, 1.5, -100, -100);
        Assert.Equal(new CoreMatrix(1.5, 0, 0, 1.5, -100, -100), harness.Host.LastTransform.Matrix);
        Assert.Contains(harness.Host.ValidatedMatrices, m => m.M11 == 3);
    }

    [Fact]
    public void ValidateTransform_Is_Only_Called_In_Custom_Mode()
    {
        var harness = Create(ContentBoundsMode.KeepContentVisible);
        harness.Host.ValidateTransformOverride = _ => false;

        harness.Engine.ZoomTo(2, 0, 0);

        Assert.Empty(harness.Host.ValidatedMatrices);
        Assert.Equal(2, harness.Engine.ZoomX);
    }

    [Fact]
    public void Custom_Invalid_Bounds_Do_Not_Restrict()
    {
        var host = new TestHost { ContentBoundsOverride = () => new CoreRect(0, 0, 0, 100) };
        var harness = EngineHarness.Create(
            s =>
            {
                s.Stretch = StretchMode.None;
                s.BoundsMode = ContentBoundsMode.Custom;
            },
            new CoreSize(400, 400),
            new CoreSize(200, 200),
            host: host);

        harness.Engine.PanDelta(1000, -1000);

        AssertEx.View(harness.Engine, 1, 1000, -1000);
    }

    [Fact]
    public void Custom_Scroll_Extent_Uses_Custom_Content_Bounds()
    {
        var host = new TestHost { ContentBoundsOverride = () => new CoreRect(0, 0, 1000, 800) };
        var harness = EngineHarness.Create(
            s =>
            {
                s.Stretch = StretchMode.None;
                s.BoundsMode = ContentBoundsMode.Custom;
            },
            new CoreSize(400, 400),
            new CoreSize(200, 200),
            host: host);

        Assert.Equal(new CoreSize(1000, 800), harness.Engine.Extent);
        Assert.Equal(new CoreVector(0, 0), harness.Engine.ScrollOffset);
    }

    [Fact]
    public void Non_Custom_Scroll_Extent_Uses_Element_Bounds()
    {
        var host = new TestHost { ContentBoundsOverride = () => new CoreRect(0, 0, 1000, 800) };
        var harness = EngineHarness.Create(
            s => s.Stretch = StretchMode.None,
            new CoreSize(400, 400),
            new CoreSize(200, 200),
            host: host);

        Assert.Equal(new CoreSize(400, 400), harness.Engine.Extent);
        Assert.Equal(0, host.GetContentBoundsCalls);
    }
}
