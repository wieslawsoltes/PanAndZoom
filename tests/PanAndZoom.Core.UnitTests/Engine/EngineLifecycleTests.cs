// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineLifecycleTests
{
    private static readonly CoreSize s_viewport = new(400, 300);
    private static readonly CoreSize s_element = new(200, 100);

    [Fact]
    public void Attach_Sequence_Applies_Initial_Transform_Then_Auto_Fit()
    {
        var harness = EngineHarness.Create(viewport: s_viewport, element: s_element, clearRecords: false);
        var host = harness.Host;

        Assert.Equal(2, host.AppliedTransforms.Count);
        Assert.Equal((CoreMatrix.Identity, false), host.AppliedTransforms[0]);
        Assert.Equal((new CoreMatrix(2, 0, 0, 2, -100, -50), false), host.AppliedTransforms[1]);
        Assert.Equal((2.0, 2.0, -100.0, -50.0), host.ViewPropertyUpdates[^1]);
        Assert.False(harness.Engine.IsAutoFitPending);
    }

    [Fact]
    public void Attach_Sequence_Records_Initial_Identity_View_In_History()
    {
        var harness = EngineHarness.Create(viewport: s_viewport, element: s_element);

        var state = Assert.Single(harness.Engine.ViewHistory);
        Assert.Equal(CoreMatrix.Identity, state.Matrix);
        Assert.Equal(0, harness.Engine.ViewHistoryIndex);
    }

    [Theory]
    [InlineData(StretchMode.None, 1, 0, 0, 1, 0, 0)]
    [InlineData(StretchMode.Fill, 2, 0, 0, 3, -100, -100)]
    [InlineData(StretchMode.Uniform, 2, 0, 0, 2, -100, -50)]
    [InlineData(StretchMode.UniformToFill, 3, 0, 0, 3, -200, -100)]
    public void Arrange_Auto_Fits_Per_Stretch_Mode(StretchMode stretch, double m11, double m12, double m21, double m22, double m31, double m32)
    {
        var harness = EngineHarness.Create(s => s.Stretch = stretch, s_viewport, s_element);

        Assert.Equal(new CoreMatrix(m11, m12, m21, m22, m31, m32), harness.Engine.Matrix);
        Assert.Equal(m11, harness.Engine.ZoomX);
        Assert.Equal(m22, harness.Engine.ZoomY);
        Assert.Equal(m31, harness.Engine.OffsetX);
        Assert.Equal(m32, harness.Engine.OffsetY);
    }

    [Fact]
    public void Arrange_Without_Element_Does_Nothing()
    {
        var harness = EngineHarness.Create(viewport: s_viewport, element: s_element, attach: false);

        harness.Engine.OnArranged(s_viewport);

        Assert.Empty(harness.Host.AppliedTransforms);
        Assert.True(harness.Engine.IsAutoFitPending);
        Assert.Equal(CoreMatrix.Identity, harness.Engine.Matrix);
    }

    [Fact]
    public void Arrange_Only_Auto_Fits_Once()
    {
        var harness = EngineHarness.Create(viewport: s_viewport, element: s_element);
        harness.Engine.ZoomTo(2, 0, 0, true);
        var matrix = harness.Engine.Matrix;
        harness.Host.ClearRecords();

        harness.Engine.OnArranged(s_viewport);

        Assert.Equal(matrix, harness.Engine.Matrix);
        Assert.Empty(harness.Host.AppliedTransforms);
    }

    [Fact]
    public void Stretch_Changed_Schedules_Auto_Fit_On_Next_Arrange()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.None, s_viewport, s_element);
        Assert.Equal(CoreMatrix.Identity, harness.Engine.Matrix);

        harness.Settings.Stretch = StretchMode.Fill;
        harness.Engine.OnStretchChanged();

        Assert.True(harness.Engine.IsAutoFitPending);
        Assert.Equal(CoreMatrix.Identity, harness.Engine.Matrix);

        harness.Engine.OnArranged(s_viewport);

        Assert.False(harness.Engine.IsAutoFitPending);
        Assert.Equal(new CoreMatrix(2, 0, 0, 3, -100, -100), harness.Engine.Matrix);
    }

    [Fact]
    public void Element_Detach_And_Reattach_Refits()
    {
        var harness = EngineHarness.Create(viewport: s_viewport, element: s_element);
        harness.Engine.ZoomTo(3, 0, 0, true);

        harness.Detach();
        Assert.True(harness.Engine.IsAutoFitPending);

        harness.Host.ElementSize = new CoreSize(100, 100);
        harness.Host.HasElement = true;
        harness.Engine.OnElementAttached();
        harness.Engine.OnArranged(s_viewport);

        // Uniform: min(400/100, 300/100) = 3 around (50,50).
        Assert.Equal(new CoreMatrix(3, 0, 0, 3, -100, -100), harness.Engine.Matrix);
    }

    [Fact]
    public void Operations_Without_Element_Do_Not_Touch_Host()
    {
        var harness = EngineHarness.Create(attach: false);

        harness.Engine.Refresh();
        harness.Engine.ZoomIn();
        harness.Engine.ZoomOut();
        harness.Engine.AutoFit();
        harness.Engine.Fill();
        harness.Engine.Uniform();
        harness.Engine.UniformToFill();
        harness.Engine.None();
        harness.Engine.InvalidateScrollable();
        harness.Engine.CenterOn(new CorePoint(10, 10));

        Assert.Empty(harness.Host.AppliedTransforms);
        Assert.Empty(harness.Host.ViewPropertyUpdates);
        Assert.Equal(CoreMatrix.Identity, harness.Engine.Matrix);
        Assert.False(harness.Engine.BringIntoView(new CoreRect(0, 0, 10, 10)));
    }

    [Fact]
    public void Refresh_Reapplies_Transform_With_Requested_Transition_Flag()
    {
        var harness = EngineHarness.Create();

        harness.Engine.Refresh();
        harness.Engine.Refresh(skipTransitions: false);

        Assert.Equal(2, harness.Host.AppliedTransforms.Count);
        Assert.True(harness.Host.AppliedTransforms[0].SkipTransitions);
        Assert.False(harness.Host.AppliedTransforms[1].SkipTransitions);
        Assert.Equal(2, harness.Host.ViewPropertyUpdates.Count);
    }

    [Fact]
    public void Bounds_Settings_Changed_Reapplies_Constraints_Without_Transitions()
    {
        var harness = EngineHarness.Create(viewport: new CoreSize(400, 400), element: new CoreSize(200, 200), configure: s => s.Stretch = StretchMode.None);
        Assert.Equal(CoreMatrix.Identity, harness.Engine.Matrix);

        harness.Settings.BoundsMode = ContentBoundsMode.KeepCentered;
        harness.Engine.OnBoundsSettingsChanged();

        AssertEx.View(harness.Engine, 1, 100, 100);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Element_Bounds_Changed_Recalculates_Scroll_Extent()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.None);
        var raised = 0;
        harness.Engine.ScrollInvalidated += (_, _) => raised++;
        Assert.Equal(new CoreSize(400, 400), harness.Engine.Extent);

        harness.Host.ElementSize = new CoreSize(800, 600);
        harness.Engine.OnElementBoundsChanged();

        Assert.Equal(1, raised);
        Assert.Equal(new CoreSize(800, 600), harness.Engine.Extent);
        Assert.Equal(new CoreSize(400, 400), harness.Engine.Viewport);
    }

    [Fact]
    public void Viewport_Size_Changed_Recalculates_Scroll_Viewport()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.None);
        var raised = 0;
        harness.Engine.ScrollInvalidated += (_, _) => raised++;

        harness.Resize(200, 100);

        Assert.Equal(1, raised);
        Assert.Equal(new CoreSize(200, 100), harness.Engine.Viewport);
        Assert.Equal(new CoreSize(400, 400), harness.Engine.Extent);
    }

    [Fact]
    public void ShouldAnimate_Requires_Animations_And_Positive_Duration()
    {
        var harness = EngineHarness.Create();
        Assert.False(harness.Engine.ShouldAnimate());

        harness.Settings.EnableAnimations = true;
        Assert.True(harness.Engine.ShouldAnimate());

        harness.Settings.AnimationDuration = TimeSpan.Zero;
        Assert.False(harness.Engine.ShouldAnimate());
    }

    [Fact]
    public void GetDefaultContentBounds_Is_Element_Size_At_Origin()
    {
        var harness = EngineHarness.Create(viewport: s_viewport, element: s_element, layoutOffset: new CorePoint(100, 100));

        Assert.Equal(new CoreRect(0, 0, 200, 100), harness.Engine.GetDefaultContentBounds());

        harness.Detach();

        Assert.Equal(default, harness.Engine.GetDefaultContentBounds());
    }
}
