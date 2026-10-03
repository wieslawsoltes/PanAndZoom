// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineZoomTests
{
    [Fact]
    public void ZoomTo_Scales_Around_Content_Point()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomTo(2, 100, 100);

        AssertEx.View(harness.Engine, 2, -100, -100);
        AssertEx.Equal(new CorePoint(100, 100), harness.Engine.ContentToViewport(new CorePoint(100, 100)));
        Assert.Equal((new CoreMatrix(2, 0, 0, 2, -100, -100), false), harness.Host.LastTransform);
        Assert.Equal((2.0, 2.0, -100.0, -100.0), harness.Host.ViewPropertyUpdates[^1]);
    }

    [Fact]
    public void ZoomTo_Is_Relative_To_Current_Matrix()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomTo(2, 0, 0);
        harness.Engine.ZoomTo(1.5, 100, 100);

        // ScaleAt(1.5, 100, 100) * Scale(2): content (100,100) stays at viewport (200,200).
        AssertEx.View(harness.Engine, 3, -100, -100);
        AssertEx.Equal(new CorePoint(200, 200), harness.Engine.ContentToViewport(new CorePoint(100, 100)));
    }

    [Fact]
    public void ZoomTo_Passes_SkipTransitions_To_Host()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomTo(2, 0, 0, skipTransitions: true);

        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ZoomTo_Adds_View_History()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomTo(2, 0, 0);

        Assert.Equal(2, harness.Engine.ViewHistory.Count);
        Assert.Equal(harness.Engine.Matrix, harness.Engine.ViewHistory[^1].Matrix);
    }

    [Fact]
    public void ZoomTo_Ratio_One_With_Constraints_Is_Ignored()
    {
        var harness = EngineHarness.Create();
        var raised = 0;
        harness.Engine.ZoomDeltaChanged += (_, _) => raised++;

        harness.Engine.ZoomTo(1, 50, 50);

        Assert.Equal(0, raised);
        Assert.Empty(harness.Host.AppliedTransforms);
        Assert.Single(harness.Engine.ViewHistory);
    }

    [Fact]
    public void ZoomTo_Ratio_One_Without_Constraints_Still_Updates()
    {
        var harness = EngineHarness.Create(s => s.EnableConstrains = false);
        var raised = 0;
        harness.Engine.ZoomDeltaChanged += (_, _) => raised++;

        harness.Engine.ZoomTo(1, 50, 50);

        Assert.Equal(1, raised);
        Assert.Single(harness.Host.AppliedTransforms);
        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void Zoom_Sets_Absolute_Zoom_Around_Point()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.Zoom(3, 50, 50);

        AssertEx.View(harness.Engine, 3, -100, -100);
    }

    [Fact]
    public void Zoom_Does_Not_Add_View_History()
    {
        var harness = EngineHarness.Create();

        harness.Engine.Zoom(3, 50, 50, skipTransitions: true);

        Assert.Single(harness.Engine.ViewHistory);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Zoom_Clamps_To_Effective_Limits()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.MinZoomX = s.MinZoomY = 0.5;
            s.MaxZoomX = s.MaxZoomY = 2;
        });

        harness.Engine.Zoom(5, 100, 100);
        AssertEx.View(harness.Engine, 2, -100, -100);

        harness.Engine.Zoom(0.1, 100, 100);
        AssertEx.View(harness.Engine, 0.5, 50, 50);
    }

    [Fact]
    public void Zoom_Uses_Stricter_Axis_Limits()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.MaxZoomX = 3;
            s.MaxZoomY = 2;
        });

        harness.Engine.Zoom(5, 0, 0);

        AssertEx.View(harness.Engine, 2, 0, 0);
    }

    [Fact]
    public void ZoomIn_Uses_ZoomSpeed_Around_Element_Center()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomIn();

        AssertEx.View(harness.Engine, 1.2, 200 - 240, 200 - 240);
    }

    [Fact]
    public void ZoomOut_Uses_Inverse_ZoomSpeed_Around_Element_Center()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomOut();

        AssertEx.View(harness.Engine, 1 / 1.2, 200 - 200 / 1.2, 200 - 200 / 1.2);
    }

    [Fact]
    public void ZoomIn_Then_ZoomOut_Returns_To_Identity()
    {
        var harness = EngineHarness.Create(s => s.ZoomSpeed = 1.5);

        harness.Engine.ZoomIn(true);
        harness.Engine.ZoomOut(true);

        AssertEx.View(harness.Engine, 1, 0, 0);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Theory]
    [InlineData(1.0, 1.2, false)]
    [InlineData(-1.0, 1 / 1.2, false)]
    [InlineData(0.5, 1.0954451150103321, true)] // 1.2^0.5, |delta| <= TransitionThreshold
    [InlineData(-0.5, 0.9128709291752769, true)]
    [InlineData(3.0, 1.728, false)]
    public void ZoomDeltaTo_Uses_ZoomSpeed_Power_And_TransitionThreshold(double delta, double expectedZoom, bool expectedSkip)
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomDeltaTo(delta, 0, 0);

        AssertEx.View(harness.Engine, expectedZoom, 0, 0, 1e-12);
        Assert.Equal(expectedSkip, harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ZoomDeltaTo_Applies_PowerFactor()
    {
        var harness = EngineHarness.Create(s => s.PowerFactor = 2);

        harness.Engine.ZoomDeltaTo(2, 0, 0);

        AssertEx.View(harness.Engine, Math.Pow(1.2, 4), 0, 0, 1e-12);
        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ZoomDeltaTo_PowerFactor_Preserves_Sign()
    {
        var harness = EngineHarness.Create(s => s.PowerFactor = 2);

        harness.Engine.ZoomDeltaTo(-2, 0, 0);

        AssertEx.View(harness.Engine, Math.Pow(1.2, -4), 0, 0, 1e-12);
    }

    [Fact]
    public void ZoomDeltaTo_PowerFactor_Applies_Before_TransitionThreshold()
    {
        var harness = EngineHarness.Create(s => s.PowerFactor = 2);

        // |0.6|^2 = 0.36 <= 0.5 so transitions are skipped although |0.6| > 0.5.
        harness.Engine.ZoomDeltaTo(0.6, 0, 0);

        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ZoomDeltaTo_Respects_TransitionThreshold_Setting()
    {
        var harness = EngineHarness.Create(s => s.TransitionThreshold = 0);

        harness.Engine.ZoomDeltaTo(0.5, 0, 0);

        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ZoomDeltaTo_SkipTransitions_Argument_Wins()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomDeltaTo(2, 0, 0, skipTransitions: true);

        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ProcessMagnify_Zooms_By_Delta()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ProcessMagnify(1, new CorePoint(100, 100));
        AssertEx.View(harness.Engine, 1.2, -20, -20);

        harness.Engine.ProcessMagnify(-1, new CorePoint(100, 100));
        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void Stretch_Methods_Apply_Matrices()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.None, new CoreSize(400, 300), new CoreSize(200, 100));

        harness.Engine.Fill();
        Assert.Equal(new CoreMatrix(2, 0, 0, 3, -100, -100), harness.Engine.Matrix);

        harness.Engine.Uniform();
        Assert.Equal(new CoreMatrix(2, 0, 0, 2, -100, -50), harness.Engine.Matrix);

        harness.Engine.UniformToFill();
        Assert.Equal(new CoreMatrix(3, 0, 0, 3, -200, -100), harness.Engine.Matrix);

        harness.Engine.None();
        Assert.Equal(CoreMatrix.Identity, harness.Engine.Matrix);

        // Applying a stretch does not change the Stretch setting.
        Assert.Equal(StretchMode.None, harness.Settings.Stretch);
    }

    [Fact]
    public void Explicit_Size_Stretch_Methods_Apply_Matrices()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.None);

        harness.Engine.Fill(400, 300, 200, 100, true);
        Assert.Equal(new CoreMatrix(2, 0, 0, 3, -100, -100), harness.Engine.Matrix);
        Assert.True(harness.Host.LastTransform.SkipTransitions);

        harness.Engine.Uniform(400, 300, 200, 100);
        Assert.Equal(new CoreMatrix(2, 0, 0, 2, -100, -50), harness.Engine.Matrix);
        Assert.False(harness.Host.LastTransform.SkipTransitions);

        harness.Engine.UniformToFill(400, 300, 200, 100);
        Assert.Equal(new CoreMatrix(3, 0, 0, 3, -200, -100), harness.Engine.Matrix);

        harness.Engine.None(400, 300, 200, 100);
        Assert.Equal(CoreMatrix.Identity, harness.Engine.Matrix);
    }

    [Theory]
    [InlineData(StretchMode.None, 1, 0, 0)]
    [InlineData(StretchMode.Uniform, 2, -100, -50)]
    [InlineData(StretchMode.UniformToFill, 3, -200, -100)]
    public void AutoFit_Uses_Current_Stretch(StretchMode stretch, double zoom, double offsetX, double offsetY)
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.None, new CoreSize(400, 300), new CoreSize(200, 100));
        harness.Settings.Stretch = stretch;

        harness.Engine.AutoFit();

        AssertEx.View(harness.Engine, zoom, offsetX, offsetY);
    }

    [Fact]
    public void AutoFit_Fill_Uses_Independent_Axes()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.Fill, new CoreSize(400, 300), new CoreSize(200, 100));
        harness.Engine.ResetMatrix();

        harness.Engine.AutoFit();

        Assert.Equal(new CoreMatrix(2, 0, 0, 3, -100, -100), harness.Engine.Matrix);
    }

    [Fact]
    public void ToggleStretchMode_Cycles_Modes()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.None);

        harness.Engine.ToggleStretchMode();
        Assert.Equal(StretchMode.Fill, harness.Settings.Stretch);
        harness.Engine.ToggleStretchMode();
        Assert.Equal(StretchMode.Uniform, harness.Settings.Stretch);
        harness.Engine.ToggleStretchMode();
        Assert.Equal(StretchMode.UniformToFill, harness.Settings.Stretch);
        harness.Engine.ToggleStretchMode();
        Assert.Equal(StretchMode.None, harness.Settings.Stretch);
    }

    [Fact]
    public void ResetMatrix_Returns_To_Identity()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(3, 10, 10);

        harness.Engine.ResetMatrix(skipTransitions: true);

        AssertEx.View(harness.Engine, 1, 0, 0);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void SetMatrix_Applies_Matrix_And_Updates_Properties()
    {
        var harness = EngineHarness.Create();

        harness.Engine.SetMatrix(MatrixMath.ScaleAndTranslate(2, 3, 4, 5));

        Assert.Equal(new CoreMatrix(2, 0, 0, 3, 4, 5), harness.Engine.Matrix);
        Assert.Equal(2, harness.Engine.ZoomX);
        Assert.Equal(3, harness.Engine.ZoomY);
        Assert.Equal(4, harness.Engine.OffsetX);
        Assert.Equal(5, harness.Engine.OffsetY);
        Assert.Equal((new CoreMatrix(2, 0, 0, 3, 4, 5), false), harness.Host.LastTransform);
    }

    [Fact]
    public void SetMatrix_With_Constraints_Removes_Skew()
    {
        var harness = EngineHarness.Create();

        harness.Engine.SetMatrix(new CoreMatrix(2, 0.5, 0.25, 3, 4, 5));

        Assert.Equal(new CoreMatrix(2, 0, 0, 3, 4, 5), harness.Engine.Matrix);
    }

    [Fact]
    public void SetMatrix_Without_Constraints_Keeps_Matrix()
    {
        var harness = EngineHarness.Create(s => s.EnableConstrains = false);
        var matrix = new CoreMatrix(2, 0.5, 0.25, 3, 4, 5);

        harness.Engine.SetMatrix(matrix);

        Assert.Equal(matrix, harness.Engine.Matrix);
        Assert.Equal(matrix, harness.Host.LastTransform.Matrix);
    }
}
