// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineViewHistoryTests
{
    [Fact]
    public void Initial_History_Has_One_Entry()
    {
        var harness = EngineHarness.Create();

        Assert.Single(harness.Engine.ViewHistory);
        Assert.Equal(0, harness.Engine.ViewHistoryIndex);
        Assert.False(harness.Engine.CanNavigateBack);
        Assert.False(harness.Engine.CanNavigateForward);
    }

    [Fact]
    public void Operations_Add_History_Entries()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomTo(2, 0, 0);

        Assert.Equal(2, harness.Engine.ViewHistory.Count);
        Assert.Equal(1, harness.Engine.ViewHistoryIndex);
        Assert.True(harness.Engine.CanNavigateBack);
        Assert.False(harness.Engine.CanNavigateForward);
    }

    [Fact]
    public void History_Entry_Records_Matrix_Stretch_And_Timestamp()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.Fill);
        var before = DateTime.UtcNow;

        harness.Engine.ZoomTo(2, 0, 0);

        var state = harness.Engine.ViewHistory[^1];
        Assert.Equal(harness.Engine.Matrix, state.Matrix);
        Assert.Equal(StretchMode.Fill, state.Stretch);
        Assert.InRange(state.Timestamp, before, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, state.Timestamp.Kind);
    }

    [Fact]
    public void Navigate_Back_And_Forward_Restores_Matrices()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);
        harness.Engine.Pan(10, 20);

        harness.Engine.NavigateBack(animate: false);
        AssertEx.View(harness.Engine, 2, 0, 0);
        Assert.True(harness.Host.LastTransform.SkipTransitions);

        harness.Engine.NavigateBack();
        AssertEx.View(harness.Engine, 1, 0, 0);
        Assert.False(harness.Host.LastTransform.SkipTransitions);
        Assert.False(harness.Engine.CanNavigateBack);
        Assert.True(harness.Engine.CanNavigateForward);

        harness.Engine.NavigateForward(animate: false);
        AssertEx.View(harness.Engine, 2, 0, 0);

        harness.Engine.NavigateForward(animate: false);
        AssertEx.View(harness.Engine, 2, 10, 20);
        Assert.False(harness.Engine.CanNavigateForward);
        Assert.Equal(3, harness.Engine.ViewHistory.Count);
    }

    [Fact]
    public void Navigation_Restores_Stretch()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.None);
        harness.Settings.Stretch = StretchMode.Fill;
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.NavigateBack(false);
        Assert.Equal(StretchMode.None, harness.Settings.Stretch);

        harness.Engine.NavigateForward(false);
        Assert.Equal(StretchMode.Fill, harness.Settings.Stretch);
    }

    [Fact]
    public void Navigation_Does_Not_Add_History()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.NavigateBack(false);
        harness.Engine.NavigateForward(false);

        Assert.Equal(2, harness.Engine.ViewHistory.Count);
        Assert.Equal(1, harness.Engine.ViewHistoryIndex);
    }

    [Fact]
    public void Navigate_At_Boundaries_Is_Ignored()
    {
        var harness = EngineHarness.Create();
        var raised = 0;
        harness.Engine.ViewHistoryChanged += (_, _) => raised++;

        harness.Engine.NavigateBack(false);
        harness.Engine.NavigateForward(false);

        Assert.Equal(0, raised);
        Assert.Empty(harness.Host.AppliedTransforms);
    }

    [Fact]
    public void New_Entry_After_Navigating_Back_Truncates_Forward_History()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);
        harness.Engine.ZoomTo(2, 0, 0);
        harness.Engine.NavigateBack(false);
        harness.Engine.NavigateBack(false);
        Assert.True(harness.Engine.CanNavigateForward);

        harness.Engine.Pan(5, 5);

        Assert.Equal(2, harness.Engine.ViewHistory.Count);
        Assert.Equal(1, harness.Engine.ViewHistoryIndex);
        Assert.False(harness.Engine.CanNavigateForward);
        Assert.Equal(CoreMatrix.Identity, harness.Engine.ViewHistory[0].Matrix);
        Assert.Equal(MatrixMath.Translate(5, 5), harness.Engine.ViewHistory[1].Matrix);
    }

    [Fact]
    public void History_Size_Is_Limited()
    {
        var harness = EngineHarness.Create(s => s.ViewHistorySize = 3);

        for (var i = 1; i <= 5; i++)
        {
            harness.Engine.Pan(i, 0);
        }

        Assert.Equal(3, harness.Engine.ViewHistory.Count);
        Assert.Equal(2, harness.Engine.ViewHistoryIndex);
        Assert.Equal(new[] { 3.0, 4.0, 5.0 }, harness.Engine.ViewHistory.Select(s => s.Matrix.M31));
    }

    [Fact]
    public void Disabled_History_Does_Not_Record_Or_Navigate()
    {
        var harness = EngineHarness.Create(s => s.EnableViewHistory = false);

        harness.Engine.ZoomTo(2, 0, 0);
        harness.Engine.Pan(10, 10);
        harness.Engine.AddToViewHistory();

        Assert.Empty(harness.Engine.ViewHistory);
        Assert.False(harness.Engine.CanNavigateBack);
        harness.Engine.NavigateBack(false);
        AssertEx.View(harness.Engine, 2, 10, 10);
    }

    [Fact]
    public void Disabling_History_Disables_Navigation()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);
        Assert.True(harness.Engine.CanNavigateBack);

        harness.Settings.EnableViewHistory = false;

        Assert.False(harness.Engine.CanNavigateBack);
    }

    [Fact]
    public void ClearViewHistory_Removes_All_Entries()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);
        var raised = 0;
        harness.Engine.ViewHistoryChanged += (_, _) => raised++;

        harness.Engine.ClearViewHistory();

        Assert.Empty(harness.Engine.ViewHistory);
        Assert.Equal(-1, harness.Engine.ViewHistoryIndex);
        Assert.False(harness.Engine.CanNavigateBack);
        Assert.False(harness.Engine.CanNavigateForward);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void ViewHistoryChanged_Is_Raised_For_Add_And_Navigation()
    {
        var harness = EngineHarness.Create();
        var raised = 0;
        harness.Engine.ViewHistoryChanged += (_, _) => raised++;

        harness.Engine.ZoomTo(2, 0, 0);
        harness.Engine.NavigateBack(false);
        harness.Engine.NavigateForward(false);

        Assert.Equal(3, raised);
    }

    [Fact]
    public void Which_Operations_Record_History()
    {
        var harness = EngineHarness.Create(element: new CoreSize(200, 200));

        void AssertAdds(bool expected, Action action)
        {
            var before = harness.Engine.ViewHistory.Count;
            action();
            Assert.Equal(expected ? before + 1 : before, harness.Engine.ViewHistory.Count);
        }

        AssertAdds(true, () => harness.Engine.ZoomTo(1.5, 0, 0));
        AssertAdds(false, () => harness.Engine.Zoom(2, 0, 0));
        AssertAdds(false, () => harness.Engine.PanDelta(1, 1));
        AssertAdds(true, () => harness.Engine.Pan(3, 3));
        AssertAdds(false, () => harness.Engine.SetMatrix(MatrixMath.Scale(2, 2)));
        AssertAdds(true, () => harness.Engine.ResetMatrix());
        AssertAdds(true, () => harness.Engine.AutoFit());
        AssertAdds(true, () => harness.Engine.Fill());
        AssertAdds(true, () => harness.Engine.Uniform());
        AssertAdds(true, () => harness.Engine.UniformToFill());
        AssertAdds(true, () => harness.Engine.ZoomIn());
        AssertAdds(true, () => harness.Engine.ZoomOut());
        AssertAdds(true, () => harness.Engine.ZoomToRectangle(new CoreRect(0, 0, 100, 100)));
        AssertAdds(true, () => harness.Engine.ZoomToRectangleExact(new CoreRect(0, 0, 100, 100), new CoreRect(0, 0, 200, 200)));
        AssertAdds(true, () => harness.Engine.CenterOn(new CorePoint(10, 10)));
        AssertAdds(false, () => harness.Engine.CenterOn(new CorePoint(10, 10), 2.0));
        AssertAdds(false, () => harness.Engine.Rotate(10));
        AssertAdds(true, () => harness.Engine.ProcessScrollGestureEnded());
        AssertAdds(true, () => harness.Engine.ProcessPinchEnded());
    }
}
