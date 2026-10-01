// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineSavedViewsTests
{
    [Fact]
    public void SaveView_And_RestoreView_Round_Trip()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.Fill);
        harness.Engine.ZoomTo(2, 50, 50);
        var before = DateTime.UtcNow;
        harness.Engine.SaveView("detail", "A detail view");
        var saved = harness.Engine.Matrix;

        harness.Settings.Stretch = StretchMode.None;
        harness.Engine.ResetMatrix();

        Assert.True(harness.Engine.RestoreView("detail", animate: false));

        Assert.Equal(saved, harness.Engine.Matrix);
        Assert.Equal(StretchMode.Fill, harness.Settings.Stretch);
        Assert.True(harness.Host.LastTransform.SkipTransitions);

        var view = harness.Engine.GetSavedView("detail");
        Assert.NotNull(view);
        Assert.Equal("detail", view!.Value.Name);
        Assert.Equal(saved, view.Value.Matrix);
        Assert.Equal(StretchMode.Fill, view.Value.Stretch);
        Assert.Equal("A detail view", view.Value.Description);
        Assert.InRange(view.Value.Timestamp, before, DateTime.UtcNow);
    }

    [Fact]
    public void RestoreView_Animate_Uses_Transitions()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 50, 50);
        harness.Engine.SaveView("a");
        harness.Engine.ResetMatrix();

        harness.Engine.RestoreView("a");

        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void RestoreView_Unknown_Returns_False()
    {
        var harness = EngineHarness.Create();

        Assert.False(harness.Engine.RestoreView("missing"));
        Assert.Empty(harness.Host.AppliedTransforms);
        Assert.Null(harness.Engine.GetSavedView("missing"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SaveView_Requires_Name(string? name)
    {
        var harness = EngineHarness.Create();

        Assert.Throws<ArgumentException>(() => harness.Engine.SaveView(name!));
    }

    [Fact]
    public void SaveView_Overwrites_Existing_Name()
    {
        var harness = EngineHarness.Create();
        harness.Engine.SaveView("a");
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.SaveView("a", "updated");

        Assert.Single(harness.Engine.GetSavedViews());
        Assert.Equal(harness.Engine.Matrix, harness.Engine.GetSavedView("a")!.Value.Matrix);
        Assert.Equal("updated", harness.Engine.GetSavedView("a")!.Value.Description);
    }

    [Fact]
    public void Saved_View_Names_Delete_And_Clear()
    {
        var harness = EngineHarness.Create();
        harness.Engine.SaveView("a");
        harness.Engine.SaveView("b");
        harness.Engine.SaveView("c");

        Assert.Equal(new[] { "a", "b", "c" }, harness.Engine.GetSavedViewNames().OrderBy(n => n));
        Assert.Equal(3, harness.Engine.GetSavedViews().Count);

        Assert.True(harness.Engine.DeleteSavedView("b"));
        Assert.False(harness.Engine.DeleteSavedView("b"));
        Assert.Equal(new[] { "a", "c" }, harness.Engine.GetSavedViewNames().OrderBy(n => n));

        harness.Engine.ClearSavedViews();

        Assert.Empty(harness.Engine.GetSavedViewNames());
        Assert.Empty(harness.Engine.GetSavedViews());
    }

    [Fact]
    public void SaveView_Is_Case_Sensitive()
    {
        var harness = EngineHarness.Create();
        harness.Engine.SaveView("View");

        Assert.Null(harness.Engine.GetSavedView("view"));
    }
}

public class EngineDiscreteZoomTests
{
    [Theory]
    [InlineData(1.2, 1.0)]
    [InlineData(1.3, 1.5)]
    [InlineData(0.1, 0.25)]
    [InlineData(100, 8.0)]
    [InlineData(1.25, 1.0)] // ties resolve to the first level found
    [InlineData(2.0, 2.0)]
    public void GetNearestDiscreteZoomLevel_Uses_Default_Levels(double target, double expected)
    {
        var harness = EngineHarness.Create();

        Assert.Equal(expected, harness.Engine.GetNearestDiscreteZoomLevel(target));
    }

    [Fact]
    public void GetNearestDiscreteZoomLevel_Without_Levels_Returns_Target()
    {
        var harness = EngineHarness.Create(s => s.DiscreteZoomLevels = null);
        Assert.Equal(1.234, harness.Engine.GetNearestDiscreteZoomLevel(1.234));

        var empty = EngineHarness.Create(s => s.DiscreteZoomLevels = Array.Empty<double>());
        Assert.Equal(1.234, empty.Engine.GetNearestDiscreteZoomLevel(1.234));
    }

    [Fact]
    public void Next_And_Previous_Levels_When_Disabled_Use_ZoomSpeed()
    {
        var harness = EngineHarness.Create();

        Assert.Equal(1.2, harness.Engine.GetNextDiscreteZoomLevel(), 12);
        Assert.Equal(1 / 1.2, harness.Engine.GetPreviousDiscreteZoomLevel(), 12);
    }

    [Fact]
    public void Next_And_Previous_Levels_When_Enabled()
    {
        var harness = EngineHarness.Create(s => s.EnableDiscreteZoomLevels = true);

        Assert.Equal(1.5, harness.Engine.GetNextDiscreteZoomLevel());
        Assert.Equal(0.75, harness.Engine.GetPreviousDiscreteZoomLevel());
    }

    [Fact]
    public void Next_And_Previous_Levels_Saturate_At_Ends()
    {
        var harness = EngineHarness.Create(s => s.EnableDiscreteZoomLevels = true);

        harness.Engine.Zoom(8, 0, 0);
        Assert.Equal(8, harness.Engine.GetNextDiscreteZoomLevel());

        harness.Engine.Zoom(0.25, 0, 0);
        Assert.Equal(0.25, harness.Engine.GetPreviousDiscreteZoomLevel());
    }

    [Fact]
    public void Next_And_Previous_Levels_Handle_Unsorted_Levels()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.EnableDiscreteZoomLevels = true;
            s.DiscreteZoomLevels = new[] { 4.0, 0.5, 2.0, 1.0 };
        });

        Assert.Equal(2.0, harness.Engine.GetNextDiscreteZoomLevel());
        Assert.Equal(0.5, harness.Engine.GetPreviousDiscreteZoomLevel());
    }

    [Fact]
    public void ZoomToLevel_Snaps_To_Nearest_Level_When_Enabled()
    {
        var harness = EngineHarness.Create(s => s.EnableDiscreteZoomLevels = true);

        harness.Engine.ZoomToLevel(2.2, 0, 0, animate: false);

        AssertEx.View(harness.Engine, 2, 0, 0);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ZoomToLevel_Uses_Exact_Level_When_Disabled()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.ZoomToLevel(2.2, 0, 0);

        AssertEx.View(harness.Engine, 2.2, 0, 0);
        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ZoomToLevel_Is_Ignored_When_Zoom_Disabled()
    {
        var harness = EngineHarness.Create(s => s.EnableZoom = false);

        harness.Engine.ZoomToLevel(2, 0, 0);

        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void ZoomToRectangle_Snaps_Zoom_To_Discrete_Level()
    {
        var harness = EngineHarness.Create(s => s.EnableDiscreteZoomLevels = true);

        // Exact zoom would be 400/300 = 1.333, nearest level is 1.5.
        harness.Engine.ZoomToRectangle(new CoreRect(0, 0, 300, 300), animate: false);

        AssertEx.View(harness.Engine, 1.5, 200 - 225, 200 - 225);
    }

    [Fact]
    public void ZoomToRectangleExact_Snaps_Zoom_To_Discrete_Level()
    {
        var harness = EngineHarness.Create(s => s.EnableDiscreteZoomLevels = true);

        harness.Engine.ZoomToRectangleExact(new CoreRect(0, 0, 100, 100), new CoreRect(0, 0, 220, 220), animate: false);

        AssertEx.View(harness.Engine, 2, 110 - 100, 110 - 100);
    }
}
