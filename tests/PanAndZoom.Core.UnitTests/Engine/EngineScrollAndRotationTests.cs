// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineScrollTests
{
    [Fact]
    public void Initial_Scroll_State_Matches_Element_And_Viewport()
    {
        var harness = EngineHarness.Create();

        Assert.Equal(new CoreSize(400, 400), harness.Engine.Extent);
        Assert.Equal(new CoreSize(400, 400), harness.Engine.Viewport);
        Assert.Equal(new CoreVector(0, 0), harness.Engine.ScrollOffset);
    }

    [Fact]
    public void Scroll_State_Follows_Zoom_And_Pan()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomTo(2, 0, 0);
        Assert.Equal(new CoreSize(800, 800), harness.Engine.Extent);
        Assert.Equal(new CoreVector(0, 0), harness.Engine.ScrollOffset);

        harness.Engine.PanDelta(-100, -50);
        Assert.Equal(new CoreSize(800, 800), harness.Engine.Extent);
        Assert.Equal(new CoreVector(100, 50), harness.Engine.ScrollOffset);
    }

    [Fact]
    public void SetScrollOffset_Pans_Opposite_To_Scroll_Direction()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.SetScrollOffset(new CoreVector(100, 50), skipTransitions: true);

        AssertEx.View(harness.Engine, 2, -100, -50);
        Assert.Equal(new CoreVector(100, 50), harness.Engine.ScrollOffset);
        Assert.True(harness.Host.LastTransform.SkipTransitions);

        harness.Engine.SetScrollOffset(new CoreVector(40, 50), skipTransitions: false);

        AssertEx.View(harness.Engine, 2, -40, -50);
        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void SetScrollOffset_Same_Value_Does_Nothing()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);
        harness.Host.ClearRecords();

        harness.Engine.SetScrollOffset(new CoreVector(0, 0), true);

        Assert.Empty(harness.Host.AppliedTransforms);
        Assert.False(harness.Engine.IsUpdating);
    }

    [Fact]
    public void ScrollInvalidated_Is_Raised_With_Updating_Guard()
    {
        var harness = EngineHarness.Create();
        var engine = harness.Engine;
        var updatingDuringEvent = new List<bool>();
        engine.ScrollInvalidated += (_, _) =>
        {
            updatingDuringEvent.Add(engine.IsUpdating);

            // A scroll viewer writing the offset back must not feed back into the engine.
            engine.SetScrollOffset(new CoreVector(999, 999), true);
            engine.SetMatrix(MatrixMath.Scale(5, 5), true);
        };

        engine.PanDelta(-10, 0);

        Assert.Equal(new[] { true }, updatingDuringEvent);
        AssertEx.View(engine, 1, -10, 0);
        Assert.False(engine.IsUpdating);
    }

    [Fact]
    public void InvalidateScrollable_Restores_Updating_State()
    {
        var harness = EngineHarness.Create();
        var engine = harness.Engine;
        var updatingDuringEvent = false;
        engine.ScrollInvalidated += (_, _) => updatingDuringEvent = engine.IsUpdating;

        engine.InvalidateScrollable();

        Assert.True(updatingDuringEvent);
        Assert.False(engine.IsUpdating);
    }

    [Fact]
    public void Operations_Are_Ignored_While_Updating()
    {
        var harness = EngineHarness.Create();
        var engine = harness.Engine;
        engine.ScrollInvalidated += (_, _) =>
        {
            engine.ZoomTo(3, 0, 0);
            engine.Zoom(3, 0, 0);
            engine.PanDelta(50, 50);
            engine.Pan(50, 50);
            engine.ContinuePanTo(50, 50);
            engine.ZoomToRectangle(new CoreRect(0, 0, 10, 10));
        };

        engine.SetMatrix(MatrixMath.ScaleAndTranslate(2, 2, 10, 10), true);

        AssertEx.View(engine, 2, 10, 10);
    }
}

public class EngineRotationTests
{
    [Fact]
    public void Rotate_Updates_Rotation_And_Render_Matrix()
    {
        var harness = EngineHarness.Create();

        harness.Engine.Rotate(30);

        Assert.Equal(30, harness.Settings.Rotation);
        Assert.Equal(CoreMatrix.Identity, harness.Engine.Matrix);
        var expected = MatrixMath.Rotation(Math.PI / 6, 200, 200);
        AssertEx.Equal(expected, harness.Engine.GetRenderMatrix());
        AssertEx.Equal(expected, harness.Host.LastTransform.Matrix);
        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Rotate_Accumulates_And_Supports_Animate_Flag()
    {
        var harness = EngineHarness.Create();

        harness.Engine.Rotate(30, animate: false);
        harness.Engine.Rotate(15, animate: false);

        Assert.Equal(45, harness.Settings.Rotation);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Render_Matrix_Applies_Rotation_After_Pan_Zoom_Around_Element_Center()
    {
        var harness = EngineHarness.Create(element: new CoreSize(200, 100));
        harness.SetMatrix(2, 10, 20);

        harness.Engine.Rotate(90);

        var expected = MatrixMath.ScaleAndTranslate(2, 2, 10, 20) * MatrixMath.Rotation(Math.PI / 2, 100, 50);
        AssertEx.Equal(expected, harness.Engine.GetRenderMatrix());
        AssertEx.Equal(new CorePoint(100, 50), MatrixMath.Rotation(Math.PI / 2, 100, 50).Transform(new CorePoint(100, 50)));

        // The pan/zoom matrix itself is not rotated.
        AssertEx.View(harness.Engine, 2, 10, 20);
    }

    [Fact]
    public void Render_Matrix_Maps_Points_Around_Element_Center()
    {
        var harness = EngineHarness.Create();

        harness.Engine.Rotate(90);

        var render = harness.Engine.GetRenderMatrix();
        AssertEx.Equal(new CorePoint(200, 200), render.Transform(new CorePoint(200, 200)));
        AssertEx.Equal(new CorePoint(200, 400), render.Transform(new CorePoint(400, 200)));
    }

    [Theory]
    [InlineData(30, 45)]
    [InlineData(20, 0)]
    [InlineData(-30, -45)]
    [InlineData(100, 90)]
    public void Rotate_Snaps_When_Enabled(double degrees, double expected)
    {
        var harness = EngineHarness.Create(s =>
        {
            s.EnableRotationSnapping = true;
            s.RotationSnapAngle = 45;
        });

        harness.Engine.Rotate(degrees);

        Assert.Equal(expected, harness.Settings.Rotation);
    }

    [Fact]
    public void Rotate_Does_Not_Snap_With_Zero_Snap_Angle()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.EnableRotationSnapping = true;
            s.RotationSnapAngle = 0;
        });

        harness.Engine.Rotate(33);

        Assert.Equal(33, harness.Settings.Rotation);
    }

    [Theory]
    [InlineData(120, 90)]
    [InlineData(-300, -90)]
    [InlineData(45, 45)]
    public void Rotate_Clamps_To_Limits(double degrees, double expected)
    {
        var harness = EngineHarness.Create(s =>
        {
            s.MinRotation = -90;
            s.MaxRotation = 90;
        });

        harness.Engine.Rotate(degrees);

        Assert.Equal(expected, harness.Settings.Rotation);
    }

    [Fact]
    public void Rotate_Default_Limits_Are_Plus_Minus_180()
    {
        var harness = EngineHarness.Create();

        harness.Engine.Rotate(170);
        harness.Engine.Rotate(30);

        Assert.Equal(180, harness.Settings.Rotation);
    }

    [Fact]
    public void Rotation_Disabled_When_EnableGestureRotation_Is_False()
    {
        var harness = EngineHarness.Create(s => s.EnableGestureRotation = false);

        harness.Engine.Rotate(30);
        harness.Engine.RotateAt(30, new CorePoint(10, 10));

        Assert.Equal(0, harness.Settings.Rotation);
        Assert.Empty(harness.Host.AppliedTransforms);

        harness.Settings.Rotation = 45;
        Assert.Equal(harness.Engine.Matrix, harness.Engine.GetRenderMatrix());
    }

    [Fact]
    public void RotateAt_Rotates_Like_Rotate()
    {
        var harness = EngineHarness.Create();

        harness.Engine.RotateAt(20, new CorePoint(10, 10), animate: false);

        Assert.Equal(20, harness.Settings.Rotation);
        AssertEx.Equal(MatrixMath.Rotation(20 * Math.PI / 180, 200, 200), harness.Engine.GetRenderMatrix());
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Tiny_Rotation_Is_Ignored_In_Render_Matrix()
    {
        var harness = EngineHarness.Create();
        harness.Settings.Rotation = 0.00001;

        Assert.Equal(harness.Engine.Matrix, harness.Engine.GetRenderMatrix());
    }

    [Fact]
    public void ResetRotation_Sets_Zero_And_Invalidates()
    {
        var harness = EngineHarness.Create();
        harness.Engine.Rotate(30);

        harness.Engine.ResetRotation(animate: false);

        Assert.Equal(0, harness.Settings.Rotation);
        Assert.Equal((CoreMatrix.Identity, true), harness.Host.LastTransform);
    }

    [Fact]
    public void SnapRotation_Snaps_Current_Rotation_When_Enabled()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.EnableRotationSnapping = true;
            s.RotationSnapAngle = 45;
        });
        harness.Settings.Rotation = 50;

        harness.Engine.SnapRotation();

        Assert.Equal(45, harness.Settings.Rotation);
        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void SnapRotation_Does_Nothing_When_Disabled()
    {
        var harness = EngineHarness.Create();
        harness.Settings.Rotation = 50;

        harness.Engine.SnapRotation();

        Assert.Equal(50, harness.Settings.Rotation);
        Assert.Empty(harness.Host.AppliedTransforms);
    }
}
