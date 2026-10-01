// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Input;

public class WheelInputTests
{
    private static readonly CorePoint s_point = new(100, 100);

    [Theory]
    [InlineData(ZoomBorderKeyModifiers.None, WheelBehaviorMode.Zoom)]
    [InlineData(ZoomBorderKeyModifiers.Control, WheelBehaviorMode.PanVertical)]
    [InlineData(ZoomBorderKeyModifiers.Shift, WheelBehaviorMode.None)]
    [InlineData(ZoomBorderKeyModifiers.Control | ZoomBorderKeyModifiers.Shift, WheelBehaviorMode.PanVertical)]
    [InlineData(ZoomBorderKeyModifiers.Alt, WheelBehaviorMode.Zoom)]
    [InlineData(ZoomBorderKeyModifiers.Meta, WheelBehaviorMode.Zoom)]
    public void GetWheelBehavior_Uses_Modifiers(ZoomBorderKeyModifiers modifiers, WheelBehaviorMode expected)
    {
        var harness = EngineHarness.Create(s =>
        {
            s.WheelBehavior = WheelBehaviorMode.Zoom;
            s.WheelWithCtrl = WheelBehaviorMode.PanVertical;
            s.WheelWithShift = WheelBehaviorMode.None;
        });

        Assert.Equal(expected, harness.Engine.GetWheelBehavior(modifiers));
    }

    [Fact]
    public void Default_Wheel_Zooms_At_Pointer()
    {
        var harness = EngineHarness.Create();

        Assert.True(harness.Engine.ProcessPointerWheel(new CoreVector(0, 1), s_point, ZoomBorderKeyModifiers.None));

        AssertEx.View(harness.Engine, 1.2, -20, -20);
        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Wheel_Down_Zooms_Out()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ProcessPointerWheel(new CoreVector(0, -1), s_point, ZoomBorderKeyModifiers.None);

        Assert.Equal(1 / 1.2, harness.Engine.ZoomX, 12);
    }

    [Fact]
    public void Wheel_Zoom_Uses_Sensitivity()
    {
        var harness = EngineHarness.Create(s => s.WheelZoomSensitivity = 0.5);

        harness.Engine.ProcessPointerWheel(new CoreVector(0, 1), s_point, ZoomBorderKeyModifiers.None);

        Assert.Equal(Math.Pow(1.2, 0.5), harness.Engine.ZoomX, 12);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Ctrl_Wheel_Zooms_By_Default()
    {
        var harness = EngineHarness.Create();

        Assert.True(harness.Engine.ProcessPointerWheel(new CoreVector(0, 1), s_point, ZoomBorderKeyModifiers.Control));

        Assert.Equal(1.2, harness.Engine.ZoomX, 12);
    }

    [Fact]
    public void Shift_Wheel_Pans_Horizontally_By_Default()
    {
        var harness = EngineHarness.Create();

        Assert.True(harness.Engine.ProcessPointerWheel(new CoreVector(0, 1), s_point, ZoomBorderKeyModifiers.Shift));

        AssertEx.View(harness.Engine, 1, 10, 0);
    }

    [Fact]
    public void PanHorizontal_Swaps_Axes_And_Uses_Sensitivity()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.WheelBehavior = WheelBehaviorMode.PanHorizontal;
            s.WheelPanSensitivity = 2;
        });

        harness.Engine.ProcessPointerWheel(new CoreVector(1, -1), s_point, ZoomBorderKeyModifiers.None);

        AssertEx.View(harness.Engine, 1, -20, 20);
    }

    [Fact]
    public void PanVertical_Uses_Delta_Axes()
    {
        var harness = EngineHarness.Create(s => s.WheelBehavior = WheelBehaviorMode.PanVertical);

        Assert.True(harness.Engine.ProcessPointerWheel(new CoreVector(1, -1), s_point, ZoomBorderKeyModifiers.None));

        AssertEx.View(harness.Engine, 1, 10, -10);
    }

    [Theory]
    [InlineData(WheelBehaviorMode.PanVertical)]
    [InlineData(WheelBehaviorMode.PanHorizontal)]
    public void Pan_Behaviors_Require_EnablePan(WheelBehaviorMode mode)
    {
        var harness = EngineHarness.Create(s =>
        {
            s.WheelBehavior = mode;
            s.EnablePan = false;
        });

        Assert.False(harness.Engine.ProcessPointerWheel(new CoreVector(0, 1), s_point, ZoomBorderKeyModifiers.None));
        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void None_Behavior_Is_Not_Handled()
    {
        var harness = EngineHarness.Create(s => s.WheelBehavior = WheelBehaviorMode.None);

        Assert.False(harness.Engine.ProcessPointerWheel(new CoreVector(0, 1), s_point, ZoomBorderKeyModifiers.None));
        Assert.Empty(harness.Host.AppliedTransforms);
    }

    [Fact]
    public void Zoom_Disabled_Falls_Back_To_Panning_Without_Modifiers()
    {
        var harness = EngineHarness.Create(s => s.EnableZoom = false);

        Assert.True(harness.Engine.ProcessPointerWheel(new CoreVector(0.5, -1), s_point, ZoomBorderKeyModifiers.None));

        AssertEx.View(harness.Engine, 1, 5, -10);
    }

    [Theory]
    [InlineData(ZoomBorderKeyModifiers.Control)]
    [InlineData(ZoomBorderKeyModifiers.Alt)]
    public void Zoom_Disabled_With_Modifiers_Is_Not_Handled(ZoomBorderKeyModifiers modifiers)
    {
        var harness = EngineHarness.Create(s => s.EnableZoom = false);

        Assert.False(harness.Engine.ProcessPointerWheel(new CoreVector(0, 1), s_point, modifiers));
        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void Zoom_And_Pan_Disabled_Is_Not_Handled()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.EnableZoom = false;
            s.EnablePan = false;
        });

        Assert.False(harness.Engine.ProcessPointerWheel(new CoreVector(0, 1), s_point, ZoomBorderKeyModifiers.None));
    }
}

public class DoubleTapInputTests
{
    private static readonly CorePoint s_point = new(100, 100);

    [Fact]
    public void Disabled_Is_Not_Handled()
    {
        var harness = EngineHarness.Create(s => s.EnableDoubleClickZoom = false);

        Assert.False(harness.Engine.ProcessDoubleTapped(s_point));
        Assert.Equal(1, harness.Engine.ZoomX);
    }

    [Fact]
    public void Without_Element_Is_Not_Handled()
    {
        var harness = EngineHarness.Create(attach: false);

        Assert.False(harness.Engine.ProcessDoubleTapped(s_point));
    }

    [Fact]
    public void ZoomIn_Mode_Zooms_In_By_Factor()
    {
        var harness = EngineHarness.Create(s => s.DoubleClickZoomMode = DoubleClickZoomMode.ZoomIn);

        Assert.True(harness.Engine.ProcessDoubleTapped(s_point));
        AssertEx.View(harness.Engine, 2, -100, -100);
        Assert.True(harness.Host.LastTransform.SkipTransitions);

        harness.Engine.ProcessDoubleTapped(s_point);
        Assert.Equal(4, harness.Engine.ZoomX);
    }

    [Fact]
    public void ZoomOut_Mode_Zooms_Out_By_Factor()
    {
        var harness = EngineHarness.Create(s => s.DoubleClickZoomMode = DoubleClickZoomMode.ZoomOut);

        Assert.True(harness.Engine.ProcessDoubleTapped(s_point));

        AssertEx.View(harness.Engine, 0.5, 50, 50);
    }

    [Fact]
    public void Custom_Factor_Is_Used()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.DoubleClickZoomMode = DoubleClickZoomMode.ZoomIn;
            s.DoubleClickZoomFactor = 3;
        });

        harness.Engine.ProcessDoubleTapped(s_point);

        Assert.Equal(3, harness.Engine.ZoomX);
    }

    [Fact]
    public void ZoomInOut_Zooms_In_Below_Threshold_And_Resets_Above()
    {
        var harness = EngineHarness.Create();
        var resets = 0;
        harness.Engine.MatrixReset += (_, _) => resets++;

        Assert.True(harness.Engine.ProcessDoubleTapped(s_point));
        AssertEx.View(harness.Engine, 2, -100, -100);

        Assert.True(harness.Engine.ProcessDoubleTapped(s_point));
        AssertEx.View(harness.Engine, 1, 0, 0);
        Assert.Equal(1, resets);
    }

    [Theory]
    [InlineData(1.49, false)]
    [InlineData(1.5, true)]
    public void ZoomInOut_Threshold_Is_DoubleClickZoomThreshold(double zoom, bool expectReset)
    {
        var harness = EngineHarness.Create();
        harness.SetMatrix(zoom, 0, 0);

        harness.Engine.ProcessDoubleTapped(new CorePoint(0, 0));

        Assert.Equal(expectReset ? 1 : zoom * 2, harness.Engine.ZoomX, 12);
    }

    [Fact]
    public void ZoomToFit_Mode_Applies_AutoFit()
    {
        var harness = EngineHarness.Create(s => s.DoubleClickZoomMode = DoubleClickZoomMode.ZoomToFit, new CoreSize(400, 300), new CoreSize(200, 100));
        harness.Engine.ResetMatrix();

        Assert.True(harness.Engine.ProcessDoubleTapped(s_point));

        AssertEx.View(harness.Engine, 2, -100, -50);
    }

    [Fact]
    public void None_Mode_Is_Not_Handled()
    {
        var harness = EngineHarness.Create(s => s.DoubleClickZoomMode = DoubleClickZoomMode.None);

        Assert.False(harness.Engine.ProcessDoubleTapped(s_point));
        Assert.Empty(harness.Host.AppliedTransforms);
    }

    [Fact]
    public void Animations_Enabled_Uses_Transitions()
    {
        var harness = EngineHarness.Create(s => s.EnableAnimations = true);

        harness.Engine.ProcessDoubleTapped(s_point);

        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }
}

public class KeyboardInputTests
{
    [Fact]
    public void Disabled_Is_Not_Handled()
    {
        var harness = EngineHarness.Create(s => s.EnableKeyboardNavigation = false);

        Assert.False(harness.Engine.ProcessKeyDown(ZoomBorderKey.Left, ZoomBorderKeyModifiers.None));
        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void Without_Element_Is_Not_Handled()
    {
        var harness = EngineHarness.Create(attach: false);

        Assert.False(harness.Engine.ProcessKeyDown(ZoomBorderKey.Left, ZoomBorderKeyModifiers.None));
    }

    [Theory]
    [InlineData(ZoomBorderKey.Left, -50, 0)]
    [InlineData(ZoomBorderKey.Right, 50, 0)]
    [InlineData(ZoomBorderKey.Up, 0, -50)]
    [InlineData(ZoomBorderKey.Down, 0, 50)]
    public void Arrow_Keys_Pan_By_KeyboardPanStep(ZoomBorderKey key, double offsetX, double offsetY)
    {
        var harness = EngineHarness.Create();

        Assert.True(harness.Engine.ProcessKeyDown(key, ZoomBorderKeyModifiers.None));

        AssertEx.View(harness.Engine, 1, offsetX, offsetY);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Arrow_Keys_Use_Custom_Pan_Step()
    {
        var harness = EngineHarness.Create(s => s.KeyboardPanStep = 10);

        harness.Engine.ProcessKeyDown(ZoomBorderKey.Right, ZoomBorderKeyModifiers.Shift);
        harness.Engine.ProcessKeyDown(ZoomBorderKey.Down, ZoomBorderKeyModifiers.None);

        AssertEx.View(harness.Engine, 1, 10, 10);
    }

    [Fact]
    public void Arrow_Keys_Use_Transitions_When_Animations_Enabled()
    {
        var harness = EngineHarness.Create(s => s.EnableAnimations = true);

        harness.Engine.ProcessKeyDown(ZoomBorderKey.Left, ZoomBorderKeyModifiers.None);

        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Add_And_Subtract_Zoom_Around_Element_Center()
    {
        var harness = EngineHarness.Create();

        Assert.True(harness.Engine.ProcessKeyDown(ZoomBorderKey.Add, ZoomBorderKeyModifiers.None));
        AssertEx.View(harness.Engine, 1.1, 200 - 220, 200 - 220);

        Assert.True(harness.Engine.ProcessKeyDown(ZoomBorderKey.Subtract, ZoomBorderKeyModifiers.None));
        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void Add_Uses_KeyboardZoomStep()
    {
        var harness = EngineHarness.Create(s => s.KeyboardZoomStep = 2);

        harness.Engine.ProcessKeyDown(ZoomBorderKey.Add, ZoomBorderKeyModifiers.None);

        AssertEx.View(harness.Engine, 2, -200, -200);
    }

    [Fact]
    public void D0_Without_Control_Is_Not_Handled()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);

        Assert.False(harness.Engine.ProcessKeyDown(ZoomBorderKey.D0, ZoomBorderKeyModifiers.None));

        Assert.Equal(2, harness.Engine.ZoomX);
    }

    [Fact]
    public void Ctrl_D0_Resets_Matrix()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 10, 10);

        Assert.True(harness.Engine.ProcessKeyDown(ZoomBorderKey.D0, ZoomBorderKeyModifiers.Control));

        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void Home_Applies_AutoFit()
    {
        var harness = EngineHarness.Create(viewport: new CoreSize(400, 300), element: new CoreSize(200, 100));
        harness.Engine.ResetMatrix();

        Assert.True(harness.Engine.ProcessKeyDown(ZoomBorderKey.Home, ZoomBorderKeyModifiers.None));

        AssertEx.View(harness.Engine, 2, -100, -50);
    }

    [Fact]
    public void Unknown_Key_Is_Not_Handled()
    {
        var harness = EngineHarness.Create();

        Assert.False(harness.Engine.ProcessKeyDown(ZoomBorderKey.None, ZoomBorderKeyModifiers.None));
        Assert.False(harness.Engine.ProcessKeyDown((ZoomBorderKey)99, ZoomBorderKeyModifiers.Control));
        Assert.Empty(harness.Host.AppliedTransforms);
    }

    [Fact]
    public void Ctrl_Left_And_Right_Navigate_History()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);

        Assert.True(harness.Engine.ProcessKeyDown(ZoomBorderKey.Left, ZoomBorderKeyModifiers.Control));
        AssertEx.View(harness.Engine, 1, 0, 0);
        Assert.True(harness.Host.LastTransform.SkipTransitions);

        Assert.True(harness.Engine.ProcessKeyDown(ZoomBorderKey.Right, ZoomBorderKeyModifiers.Control));
        AssertEx.View(harness.Engine, 2, 0, 0);
    }

    [Fact]
    public void Ctrl_Left_Without_History_Is_Handled_But_Does_Nothing()
    {
        var harness = EngineHarness.Create();

        Assert.True(harness.Engine.ProcessKeyDown(ZoomBorderKey.Left, ZoomBorderKeyModifiers.Control));
        Assert.True(harness.Engine.ProcessKeyDown(ZoomBorderKey.Right, ZoomBorderKeyModifiers.Control));

        AssertEx.View(harness.Engine, 1, 0, 0);
        Assert.Empty(harness.Host.AppliedTransforms);
    }

    [Fact]
    public void Ctrl_Left_With_History_Disabled_Does_Nothing()
    {
        var harness = EngineHarness.Create(s => s.EnableViewHistory = false);
        harness.Engine.ZoomTo(2, 0, 0);

        Assert.True(harness.Engine.ProcessKeyDown(ZoomBorderKey.Left, ZoomBorderKeyModifiers.Control));

        Assert.Equal(2, harness.Engine.ZoomX);
    }
}
