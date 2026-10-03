// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Globalization;
using System.Windows.Input;

namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineStateTests
{
    [Fact]
    public void ExportState_Captures_Matrix_And_Settings()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.Stretch = StretchMode.Fill;
            s.ZoomSpeed = 1.5;
            s.EnablePan = false;
            s.EnableZoom = true;
            s.MinZoomX = 0.1;
            s.MaxZoomX = 10;
            s.MinZoomY = 0.2;
            s.MaxZoomY = 20;
            s.EnableConstrains = true;
            s.EnableAnimations = true;
            s.AnimationDuration = TimeSpan.FromMilliseconds(123);
        });
        harness.Engine.ZoomTo(2, 10, 10, true);
        harness.Engine.Rotate(15, false);
        var before = DateTime.UtcNow;

        var state = harness.Engine.ExportState();

        Assert.Equal(harness.Engine.Matrix, state.Matrix);
        Assert.Equal(StretchMode.Fill, state.Stretch);
        Assert.Equal(1.5, state.ZoomSpeed);
        Assert.False(state.EnablePan);
        Assert.True(state.EnableZoom);
        Assert.Equal(15, state.Rotation);
        Assert.Equal(0.1, state.MinZoomX);
        Assert.Equal(10, state.MaxZoomX);
        Assert.Equal(0.2, state.MinZoomY);
        Assert.Equal(20, state.MaxZoomY);
        Assert.True(state.EnableConstrains);
        Assert.True(state.EnableAnimations);
        Assert.Equal(TimeSpan.FromMilliseconds(123), state.AnimationDuration);
        Assert.InRange(state.Timestamp, before, DateTime.UtcNow);
    }

    [Fact]
    public void ImportState_Restores_Settings_And_Matrix()
    {
        var source = EngineHarness.Create(s =>
        {
            s.Stretch = StretchMode.UniformToFill;
            s.ZoomSpeed = 2;
            s.EnableZoom = false;
            s.MaxZoomX = 5;
            s.MaxZoomY = 5;
        });
        source.SetMatrix(3, -20, 40);
        source.Engine.Rotate(10);
        var state = source.Engine.ExportState();

        var target = EngineHarness.Create();
        target.Engine.ImportState(state, animate: false);

        Assert.Equal(source.Engine.Matrix, target.Engine.Matrix);
        AssertEx.View(target.Engine, 3, -20, 40);
        Assert.Equal(StretchMode.UniformToFill, target.Settings.Stretch);
        Assert.Equal(2, target.Settings.ZoomSpeed);
        Assert.False(target.Settings.EnableZoom);
        Assert.True(target.Settings.EnablePan);
        Assert.Equal(10, target.Settings.Rotation);
        Assert.Equal(5, target.Settings.MaxZoomX);
        Assert.Equal(5, target.Settings.MaxZoomY);
        Assert.True(target.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ImportState_Applies_Imported_Constraints()
    {
        var target = EngineHarness.Create();
        var state = new CoreZoomBorderState
        {
            Matrix = MatrixMath.ScaleAndTranslate(4, 4, 0, 0),
            Stretch = StretchMode.None,
            ZoomSpeed = 1.2,
            EnablePan = true,
            EnableZoom = true,
            MinZoomX = 0.5,
            MaxZoomX = 2,
            MinZoomY = 0.5,
            MaxZoomY = 2,
            EnableConstrains = true,
            AnimationDuration = TimeSpan.FromMilliseconds(100)
        };

        target.Engine.ImportState(state);

        AssertEx.View(target.Engine, 2, 0, 0);
        Assert.False(target.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ImportState_Null_Does_Nothing()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ImportState(null);

        Assert.Empty(harness.Host.AppliedTransforms);
        Assert.Equal(StretchMode.Uniform, harness.Settings.Stretch);
    }
}

public class EngineAccessibilityTests
{
    [Fact]
    public void UpdateAccessibilityDescriptions_Formats_Zoom_And_Pan()
    {
        var harness = EngineHarness.Create();
        harness.SetMatrix(1.5, -12.4, 30.6);

        WithInvariantCulture(() => harness.Engine.UpdateAccessibilityDescriptions());

        Assert.Equal("Zoom level: 150%", harness.Settings.ZoomLevelDescription);
        Assert.Equal("Pan position: X=-12, Y=31", harness.Settings.PanPositionDescription);
    }

    [Fact]
    public void GetAccessibilityDescription_Combines_Descriptions()
    {
        var harness = EngineHarness.Create();
        harness.SetMatrix(2, 10, 20);

        var description = WithInvariantCulture(() => harness.Engine.GetAccessibilityDescription());

        Assert.Equal("Zoom level: 200%. Pan position: X=10, Y=20", description);
    }

    [Fact]
    public void Descriptions_Default_To_Empty()
    {
        var harness = EngineHarness.Create();

        Assert.Equal(string.Empty, harness.Settings.ZoomLevelDescription);
        Assert.Equal(string.Empty, harness.Settings.PanPositionDescription);
    }

    [Fact]
    public void GetZoomIndicatorText_Uses_Format()
    {
        var harness = EngineHarness.Create();
        harness.SetMatrix(1.5, 0, 0);

        Assert.Equal(string.Format(ZoomBorderDefaults.ZoomIndicatorFormat, 1.5), harness.Engine.GetZoomIndicatorText());

        harness.Settings.ZoomIndicatorFormat = "x{0:0.00}";
        Assert.Equal(WithInvariantCulture(() => "x1.50"), WithInvariantCulture(() => harness.Engine.GetZoomIndicatorText()));
    }

    private static T WithInvariantCulture<T>(Func<T> action)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private static void WithInvariantCulture(Action action)
    {
        WithInvariantCulture(() =>
        {
            action();
            return 0;
        });
    }
}

public class EngineSnapToGridTests
{
    [Fact]
    public void SnapToGrid_Disabled_Returns_Value()
    {
        var harness = EngineHarness.Create();

        Assert.Equal(37, harness.Engine.SnapToGrid(37));
        Assert.Equal(new CorePoint(37, 63), harness.Engine.SnapToGrid(new CorePoint(37, 63)));
    }

    [Fact]
    public void SnapToGrid_Enabled_Rounds_To_Grid()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.EnableSnapToGrid = true;
            s.GridSize = 50;
        });

        Assert.Equal(50, harness.Engine.SnapToGrid(37));
        Assert.Equal(0, harness.Engine.SnapToGrid(24));
        Assert.Equal(-50, harness.Engine.SnapToGrid(-30));
        Assert.Equal(new CorePoint(50, 50), harness.Engine.SnapToGrid(new CorePoint(37, 63)));
        Assert.Equal(new CoreRect(0, 50, 100, 100), harness.Engine.SnapToGrid(new CoreRect(10, 40, 80, 120)));
    }

    [Fact]
    public void SnapToGrid_Ignores_Non_Positive_Grid_Size()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.EnableSnapToGrid = true;
            s.GridSize = 0;
        });

        Assert.Equal(37, harness.Engine.SnapToGrid(37));
    }
}

public class EngineZoomIndicatorTests
{
    [Fact]
    public void Indicator_Hidden_By_Default()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomIn();

        Assert.False(harness.Engine.IsZoomIndicatorVisible);
        Assert.Empty(harness.Host.ZoomIndicatorVisibilityChanges);
        Assert.Empty(harness.Host.ZoomIndicatorTimerStarts);
    }

    [Fact]
    public void Indicator_Shown_And_Timer_Started_On_Every_Update()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.ShowZoomIndicator = true;
            s.ZoomIndicatorAutoHideDuration = TimeSpan.FromSeconds(3);
        });

        harness.Engine.ZoomIn();
        harness.Engine.PanDelta(1, 1);

        Assert.True(harness.Engine.IsZoomIndicatorVisible);
        Assert.Equal(new[] { true, true }, harness.Host.ZoomIndicatorVisibilityChanges);
        Assert.Equal(new[] { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3) }, harness.Host.ZoomIndicatorTimerStarts);
    }

    [Fact]
    public void Indicator_Is_Shown_After_Transform_Is_Applied()
    {
        var harness = EngineHarness.Create(s => s.ShowZoomIndicator = true);

        harness.Engine.ZoomIn();

        var log = harness.Host.CallLog;
        Assert.True(log.IndexOf(nameof(TestHost.ApplyTransform)) < log.IndexOf(nameof(TestHost.OnZoomIndicatorVisibilityChanged)));
        Assert.True(log.IndexOf(nameof(TestHost.OnZoomIndicatorVisibilityChanged)) < log.IndexOf(nameof(TestHost.StartZoomIndicatorTimer)));
    }

    [Fact]
    public void HideZoomIndicator_Hides_Once()
    {
        var harness = EngineHarness.Create(s => s.ShowZoomIndicator = true);
        harness.Engine.ZoomIn();
        harness.Host.ClearRecords();

        harness.Engine.HideZoomIndicator();
        harness.Engine.HideZoomIndicator();

        Assert.False(harness.Engine.IsZoomIndicatorVisible);
        Assert.Equal(new[] { false }, harness.Host.ZoomIndicatorVisibilityChanges);
    }

    [Fact]
    public void HideZoomIndicator_When_Hidden_Does_Nothing()
    {
        var harness = EngineHarness.Create();

        harness.Engine.HideZoomIndicator();

        Assert.Empty(harness.Host.ZoomIndicatorVisibilityChanges);
    }
}

public class EngineCommandTests
{
    [Fact]
    public void Commands_Are_Cached()
    {
        var engine = EngineHarness.Create().Engine;

        Assert.Same(engine.ZoomInCommand, engine.ZoomInCommand);
        Assert.Same(engine.ZoomOutCommand, engine.ZoomOutCommand);
        Assert.Same(engine.ResetCommand, engine.ResetCommand);
        Assert.Same(engine.FitCommand, engine.FitCommand);
        Assert.Same(engine.FillCommand, engine.FillCommand);
        Assert.Same(engine.UniformCommand, engine.UniformCommand);
        Assert.Same(engine.UniformToFillCommand, engine.UniformToFillCommand);
        Assert.Same(engine.NavigateBackCommand, engine.NavigateBackCommand);
        Assert.Same(engine.NavigateForwardCommand, engine.NavigateForwardCommand);
        Assert.Same(engine.ToggleStretchCommand, engine.ToggleStretchCommand);
    }

    [Fact]
    public void CanExecute_With_Element()
    {
        var engine = EngineHarness.Create().Engine;

        Assert.True(engine.ZoomInCommand.CanExecute(null));
        Assert.True(engine.ZoomOutCommand.CanExecute(null));
        Assert.True(engine.ResetCommand.CanExecute(null));
        Assert.True(engine.FitCommand.CanExecute(null));
        Assert.True(engine.FillCommand.CanExecute(null));
        Assert.True(engine.UniformCommand.CanExecute(null));
        Assert.True(engine.UniformToFillCommand.CanExecute(null));
        Assert.True(engine.ToggleStretchCommand.CanExecute(null));
        Assert.False(engine.NavigateBackCommand.CanExecute(null));
        Assert.False(engine.NavigateForwardCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_Without_Element()
    {
        var engine = EngineHarness.Create(attach: false).Engine;

        Assert.False(engine.ZoomInCommand.CanExecute(null));
        Assert.False(engine.ZoomOutCommand.CanExecute(null));
        Assert.True(engine.ResetCommand.CanExecute(null));
        Assert.False(engine.FitCommand.CanExecute(null));
        Assert.False(engine.FillCommand.CanExecute(null));
        Assert.False(engine.UniformCommand.CanExecute(null));
        Assert.False(engine.UniformToFillCommand.CanExecute(null));
        Assert.True(engine.ToggleStretchCommand.CanExecute(null));
    }

    [Fact]
    public void Zoom_Commands_Require_EnableZoom()
    {
        var harness = EngineHarness.Create();

        harness.Settings.EnableZoom = false;

        Assert.False(harness.Engine.ZoomInCommand.CanExecute(null));
        Assert.False(harness.Engine.ZoomOutCommand.CanExecute(null));
    }

    [Fact]
    public void Zoom_Commands_Execute_Without_Transitions_When_Animations_Disabled()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ZoomInCommand.Execute(null);
        AssertEx.View(harness.Engine, 1.2, -40, -40);
        Assert.True(harness.Host.LastTransform.SkipTransitions);

        harness.Engine.ZoomOutCommand.Execute(null);
        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void Zoom_Commands_Execute_With_Transitions_When_Animations_Enabled()
    {
        var harness = EngineHarness.Create(s => s.EnableAnimations = true);

        harness.Engine.ZoomInCommand.Execute(null);

        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Stretch_Commands_Execute()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.None, new CoreSize(400, 300), new CoreSize(200, 100));

        harness.Engine.FillCommand.Execute(null);
        Assert.Equal(new CoreMatrix(2, 0, 0, 3, -100, -100), harness.Engine.Matrix);

        harness.Engine.UniformCommand.Execute(null);
        Assert.Equal(new CoreMatrix(2, 0, 0, 2, -100, -50), harness.Engine.Matrix);

        harness.Engine.UniformToFillCommand.Execute(null);
        Assert.Equal(new CoreMatrix(3, 0, 0, 3, -200, -100), harness.Engine.Matrix);

        harness.Engine.ResetCommand.Execute(null);
        Assert.Equal(CoreMatrix.Identity, harness.Engine.Matrix);

        harness.Settings.Stretch = StretchMode.Uniform;
        harness.Engine.FitCommand.Execute(null);
        Assert.Equal(new CoreMatrix(2, 0, 0, 2, -100, -50), harness.Engine.Matrix);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void ToggleStretchCommand_Cycles_Stretch()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.UniformToFill);

        harness.Engine.ToggleStretchCommand.Execute(null);

        Assert.Equal(StretchMode.None, harness.Settings.Stretch);
    }

    [Fact]
    public void Navigation_Commands_Follow_History()
    {
        var harness = EngineHarness.Create();
        var back = harness.Engine.NavigateBackCommand;
        var forward = harness.Engine.NavigateForwardCommand;
        var backChanged = 0;
        var forwardChanged = 0;
        back.CanExecuteChanged += (_, _) => backChanged++;
        forward.CanExecuteChanged += (_, _) => forwardChanged++;

        harness.Engine.ZoomTo(2, 0, 0);

        Assert.True(back.CanExecute(null));
        Assert.False(forward.CanExecute(null));
        Assert.Equal(1, backChanged);
        Assert.Equal(1, forwardChanged);

        back.Execute(null);

        AssertEx.View(harness.Engine, 1, 0, 0);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
        Assert.False(back.CanExecute(null));
        Assert.True(forward.CanExecute(null));
        Assert.Equal(2, backChanged);

        forward.Execute(null);

        AssertEx.View(harness.Engine, 2, 0, 0);
        Assert.Equal(3, forwardChanged);

        harness.Engine.ClearViewHistory();
        Assert.Equal(4, backChanged);
        Assert.False(back.CanExecute(null));
    }

    [Fact]
    public void Element_Attach_And_Detach_Raise_CanExecuteChanged_For_All_Created_Commands()
    {
        var harness = EngineHarness.Create(attach: false);
        var engine = harness.Engine;
        var commands = new ICommand[]
        {
            engine.ZoomInCommand,
            engine.ZoomOutCommand,
            engine.ResetCommand,
            engine.FitCommand,
            engine.FillCommand,
            engine.UniformCommand,
            engine.UniformToFillCommand,
            engine.NavigateBackCommand,
            engine.NavigateForwardCommand,
            engine.ToggleStretchCommand
        };
        var counts = new int[commands.Length];
        for (var i = 0; i < commands.Length; i++)
        {
            var index = i;
            commands[i].CanExecuteChanged += (_, _) => counts[index]++;
        }

        harness.Host.HasElement = true;
        engine.OnElementAttached();

        Assert.All(counts, c => Assert.Equal(1, c));
        Assert.True(engine.ZoomInCommand.CanExecute(null));

        harness.Detach();

        Assert.All(counts, c => Assert.Equal(2, c));
        Assert.False(engine.ZoomInCommand.CanExecute(null));
    }

    [Fact]
    public void RaiseCommandsCanExecuteChanged_Without_Created_Commands_Does_Not_Throw()
    {
        var engine = EngineHarness.Create().Engine;

        engine.RaiseCommandsCanExecuteChanged();
        engine.RaiseNavigationCommandsCanExecuteChanged();
    }
}
