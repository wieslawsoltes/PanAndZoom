// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineResizeBehaviorTests
{
    private static EngineHarness Create(ResizeBehaviorMode mode, StretchMode stretch = StretchMode.None)
    {
        var harness = EngineHarness.Create(s =>
        {
            s.Stretch = stretch;
            s.ResizeBehavior = mode;
        });
        return harness;
    }

    [Fact]
    public void None_Keeps_View()
    {
        var harness = Create(ResizeBehaviorMode.None);
        harness.SetMatrix(2, -100, -50);

        harness.Resize(600, 500);

        AssertEx.View(harness.Engine, 2, -100, -50);
        Assert.Empty(harness.Host.ResizeCalls);
    }

    [Fact]
    public void MaintainCenter_Keeps_Viewport_Center_On_Same_Content_Point()
    {
        var harness = Create(ResizeBehaviorMode.MaintainCenter);
        harness.SetMatrix(2, -100, -50);
        var centerContent = harness.Engine.ViewportToContent(new CorePoint(200, 200));

        harness.Resize(600, 500);

        AssertEx.View(harness.Engine, 2, 0, 0);
        AssertEx.Equal(centerContent, harness.Engine.ViewportToContent(new CorePoint(300, 250)));
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void MaintainTopLeft_Keeps_View()
    {
        var harness = Create(ResizeBehaviorMode.MaintainTopLeft);
        harness.SetMatrix(2, -100, -50);

        harness.Resize(600, 500);

        AssertEx.View(harness.Engine, 2, -100, -50);
    }

    [Fact]
    public void MaintainZoom_Scales_Offsets_Proportionally()
    {
        var harness = Create(ResizeBehaviorMode.MaintainZoom);
        harness.SetMatrix(2, -100, -50);

        harness.Resize(600, 500);

        AssertEx.View(harness.Engine, 2, -150, -62.5);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ReapplyStretch_Refits_To_New_Size()
    {
        var harness = Create(ResizeBehaviorMode.ReapplyStretch, StretchMode.Uniform);
        harness.SetMatrix(3, 10, 10);
        var autoFitApplied = 0;
        harness.Engine.AutoFitApplied += (_, _) => autoFitApplied++;

        harness.Resize(800, 600);

        AssertEx.View(harness.Engine, 1.5, -100, -100);
        Assert.Equal(1, autoFitApplied);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Custom_Calls_Host_OnResized_With_Old_And_New_Size()
    {
        var harness = Create(ResizeBehaviorMode.Custom);
        harness.SetMatrix(2, -100, -50);

        harness.Resize(600, 500);
        harness.Resize(700, 700);

        Assert.Equal(
            new[]
            {
                (new CoreSize(400, 400), new CoreSize(600, 500)),
                (new CoreSize(600, 500), new CoreSize(700, 700))
            },
            harness.Host.ResizeCalls);
        AssertEx.View(harness.Engine, 2, -100, -50);
    }

    [Fact]
    public void Same_Size_Is_Ignored()
    {
        var harness = Create(ResizeBehaviorMode.Custom);

        harness.Resize(400, 400);

        Assert.Empty(harness.Host.ResizeCalls);
    }

    [Fact]
    public void First_Size_Is_Only_Recorded()
    {
        var harness = EngineHarness.Create(s => s.ResizeBehavior = ResizeBehaviorMode.Custom, attach: false);
        harness.Host.HasElement = true;
        harness.Engine.OnElementAttached();
        harness.Engine.OnAttachedToVisualTree();

        harness.Engine.OnViewportSizeChanged(new CoreSize(400, 400));
        Assert.Empty(harness.Host.ResizeCalls);

        harness.Resize(500, 500);
        Assert.Equal((new CoreSize(400, 400), new CoreSize(500, 500)), Assert.Single(harness.Host.ResizeCalls));
    }

    [Fact]
    public void Zero_Previous_Size_Is_Only_Recorded()
    {
        var harness = EngineHarness.Create(
            s =>
            {
                s.Stretch = StretchMode.None;
                s.ResizeBehavior = ResizeBehaviorMode.Custom;
            },
            viewport: new CoreSize(0, 0));

        harness.Resize(500, 500);
        Assert.Empty(harness.Host.ResizeCalls);

        harness.Resize(600, 600);
        Assert.Single(harness.Host.ResizeCalls);
    }

    [Fact]
    public void Resize_Without_Element_Only_Records_Size()
    {
        var harness = Create(ResizeBehaviorMode.Custom);
        harness.Detach();

        harness.Resize(600, 500);
        Assert.Empty(harness.Host.ResizeCalls);

        harness.Host.HasElement = true;
        harness.Resize(700, 700);

        Assert.Equal((new CoreSize(600, 500), new CoreSize(700, 700)), Assert.Single(harness.Host.ResizeCalls));
    }
}
