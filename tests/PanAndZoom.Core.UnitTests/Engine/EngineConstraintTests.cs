// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineConstraintTests
{
    [Fact]
    public void ZoomTo_Clamps_Ratio_To_MaxZoom_Without_Translation_Jump()
    {
        var harness = EngineHarness.Create(s => s.MaxZoomX = s.MaxZoomY = 2);

        harness.Engine.ZoomTo(3, 100, 100);

        // Effective ratio is 2 so the zoom center stays fixed.
        AssertEx.View(harness.Engine, 2, -100, -100);
        AssertEx.Equal(new CorePoint(100, 100), harness.Engine.ContentToViewport(new CorePoint(100, 100)));
    }

    [Fact]
    public void ZoomTo_At_MaxZoom_Is_Ignored()
    {
        var harness = EngineHarness.Create(s => s.MaxZoomX = s.MaxZoomY = 2);
        harness.Engine.ZoomTo(2, 0, 0);
        var raised = 0;
        harness.Engine.ZoomDeltaChanged += (_, _) => raised++;
        harness.Host.ClearRecords();

        harness.Engine.ZoomTo(1.5, 0, 0);

        Assert.Equal(0, raised);
        Assert.Empty(harness.Host.AppliedTransforms);
        AssertEx.View(harness.Engine, 2, 0, 0);
        Assert.Equal(2, harness.Engine.ViewHistory.Count);
    }

    [Fact]
    public void ZoomTo_At_MaxZoom_Can_Zoom_Out()
    {
        var harness = EngineHarness.Create(s => s.MaxZoomX = s.MaxZoomY = 2);
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.ZoomTo(0.5, 0, 0);

        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void ZoomTo_Clamps_Ratio_To_MinZoom()
    {
        var harness = EngineHarness.Create(s => s.MinZoomX = s.MinZoomY = 0.5);

        harness.Engine.ZoomTo(0.25, 200, 200);

        AssertEx.View(harness.Engine, 0.5, 100, 100);

        harness.Engine.ZoomTo(0.5, 200, 200);

        AssertEx.View(harness.Engine, 0.5, 100, 100);
    }

    [Fact]
    public void ZoomIn_Repeatedly_Stops_At_MaxZoom()
    {
        var harness = EngineHarness.Create(s => s.MaxZoomX = s.MaxZoomY = 3);

        for (var i = 0; i < 20; i++)
        {
            harness.Engine.ZoomIn();
        }

        Assert.Equal(3, harness.Engine.ZoomX, 12);
        Assert.Equal(3, harness.Engine.ZoomY, 12);
    }

    [Fact]
    public void Disabled_Constraints_Ignore_Zoom_Limits()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.EnableConstrains = false;
            s.MaxZoomX = s.MaxZoomY = 2;
            s.MinOffsetX = s.MinOffsetY = -10;
        });

        harness.Engine.ZoomTo(3, 100, 100);
        harness.Engine.Zoom(5, 0, 0);
        harness.Engine.PanDelta(-100, -100);

        AssertEx.View(harness.Engine, 5, -100, -100);
    }

    [Fact]
    public void Offsets_Are_Clamped_To_Min_And_Max_Offsets()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.MinOffsetX = -50;
            s.MaxOffsetX = 50;
            s.MinOffsetY = -30;
            s.MaxOffsetY = 30;
        });

        harness.Engine.PanDelta(100, -100);
        AssertEx.View(harness.Engine, 1, 50, -30);

        harness.Engine.Pan(-100, 100);
        AssertEx.View(harness.Engine, 1, -50, 30);
    }

    [Fact]
    public void SetMatrix_Clamps_Zoom_Per_Axis()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.MinZoomX = 0.5;
            s.MaxZoomX = 2;
            s.MinZoomY = 1;
            s.MaxZoomY = 4;
        });

        harness.Engine.SetMatrix(MatrixMath.ScaleAndTranslate(10, 0.1, 0, 0));

        Assert.Equal(new CoreMatrix(2, 0, 0, 1, 0, 0), harness.Engine.Matrix);
    }

    [Fact]
    public void GetEffectiveZoomLimits_Without_Auto_Calculation_Returns_Settings()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.MinZoomX = 0.1;
            s.MaxZoomX = 10;
            s.MinZoomY = 0.2;
            s.MaxZoomY = 20;
        });

        harness.Engine.GetEffectiveZoomLimits(out var minX, out var maxX, out var minY, out var maxY);

        Assert.Equal((0.1, 10.0, 0.2, 20.0), (minX, maxX, minY, maxY));
        Assert.Equal(0, harness.Host.CalculateAutoZoomLimitsCalls);
    }

    [Fact]
    public void AutoCalculateMinZoom_Uses_Fit_Zoom()
    {
        var harness = EngineHarness.Create(
            s =>
            {
                s.Stretch = StretchMode.None;
                s.AutoCalculateMinZoom = true;
            },
            new CoreSize(400, 400),
            new CoreSize(800, 1600));

        Assert.Equal((0.25, double.PositiveInfinity), harness.Engine.CalculateDefaultAutoZoomLimits());

        harness.Engine.GetEffectiveZoomLimits(out var minX, out var maxX, out var minY, out var maxY);

        Assert.Equal((0.25, double.PositiveInfinity, 0.25, double.PositiveInfinity), (minX, maxX, minY, maxY));
    }

    [Fact]
    public void AutoCalculateMinZoom_Prevents_Zooming_Out_Past_Fit()
    {
        var harness = EngineHarness.Create(
            s =>
            {
                s.Stretch = StretchMode.None;
                s.AutoCalculateMinZoom = true;
            },
            new CoreSize(400, 400),
            new CoreSize(800, 800));

        for (var i = 0; i < 20; i++)
        {
            harness.Engine.ZoomOut();
        }

        Assert.Equal(0.5, harness.Engine.ZoomX, 12);
    }

    [Fact]
    public void AutoCalculateMaxZoom_Uses_MaxZoomPixelSize()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.AutoCalculateMaxZoom = true;
            s.MaxZoomPixelSize = 4;
        });

        Assert.Equal((double.NegativeInfinity, 4.0), harness.Engine.CalculateDefaultAutoZoomLimits());

        harness.Engine.ZoomTo(10, 0, 0);

        AssertEx.View(harness.Engine, 4, 0, 0);
    }

    [Fact]
    public void Auto_Limits_Combine_With_Manual_Limits_Using_Stricter_Value()
    {
        var harness = EngineHarness.Create(
            s =>
            {
                s.Stretch = StretchMode.None;
                s.AutoCalculateMinZoom = true;
                s.AutoCalculateMaxZoom = true;
                s.MaxZoomPixelSize = 4;
                s.MinZoomX = 0.75;
                s.MinZoomY = 0.25;
                s.MaxZoomX = 3;
                s.MaxZoomY = 8;
            },
            new CoreSize(400, 400),
            new CoreSize(800, 800));

        harness.Engine.GetEffectiveZoomLimits(out var minX, out var maxX, out var minY, out var maxY);

        Assert.Equal((0.75, 3.0, 0.5, 4.0), (minX, maxX, minY, maxY));
    }

    [Fact]
    public void Host_CalculateAutoZoomLimits_Override_Is_Used()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.AutoCalculateMinZoom = true;
            s.AutoCalculateMaxZoom = true;
        });
        harness.Host.AutoZoomLimitsOverride = () => (0.25, 1.5);
        harness.Host.ClearRecords();

        harness.Engine.GetEffectiveZoomLimits(out var minX, out var maxX, out var minY, out var maxY);

        Assert.Equal((0.25, 1.5, 0.25, 1.5), (minX, maxX, minY, maxY));
        Assert.Equal(1, harness.Host.CalculateAutoZoomLimitsCalls);

        harness.Engine.ZoomTo(4, 0, 0);
        AssertEx.View(harness.Engine, 1.5, 0, 0);
    }

    [Fact]
    public void Host_CalculateAutoZoomLimits_Is_Only_Used_For_Enabled_Directions()
    {
        var harness = EngineHarness.Create(s => s.AutoCalculateMaxZoom = true);
        harness.Host.AutoZoomLimitsOverride = () => (0.9, 1.5);

        harness.Engine.GetEffectiveZoomLimits(out var minX, out var maxX, out _, out _);

        Assert.Equal(double.NegativeInfinity, minX);
        Assert.Equal(1.5, maxX);
    }

    [Fact]
    public void Host_Infinite_Auto_Limits_Leave_Settings_Unchanged()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.AutoCalculateMinZoom = true;
            s.AutoCalculateMaxZoom = true;
            s.MinZoomX = 0.5;
            s.MaxZoomX = 2;
        });
        harness.Host.AutoZoomLimitsOverride = () => (double.NegativeInfinity, double.PositiveInfinity);

        harness.Engine.GetEffectiveZoomLimits(out var minX, out var maxX, out _, out _);

        Assert.Equal(0.5, minX);
        Assert.Equal(2, maxX);
    }

    [Fact]
    public void CalculateDefaultAutoZoomLimits_Without_Element_Or_Flags_Is_Unbounded()
    {
        var detached = EngineHarness.Create(s => s.AutoCalculateMinZoom = s.AutoCalculateMaxZoom = true, attach: false);
        Assert.Equal((double.NegativeInfinity, double.PositiveInfinity), detached.Engine.CalculateDefaultAutoZoomLimits());

        var noFlags = EngineHarness.Create();
        Assert.Equal((double.NegativeInfinity, double.PositiveInfinity), noFlags.Engine.CalculateDefaultAutoZoomLimits());

        var emptyElement = EngineHarness.Create(
            s =>
            {
                s.Stretch = StretchMode.None;
                s.AutoCalculateMinZoom = s.AutoCalculateMaxZoom = true;
            },
            element: new CoreSize(0, 0));
        Assert.Equal((double.NegativeInfinity, double.PositiveInfinity), emptyElement.Engine.CalculateDefaultAutoZoomLimits());
    }
}
