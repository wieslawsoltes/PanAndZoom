// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Matrices;

public class MatrixTransitionTests
{
    private static readonly CoreMatrix s_from = MatrixMath.ScaleAndTranslate(1, 1, 0, 0);
    private static readonly CoreMatrix s_to = MatrixMath.ScaleAndTranslate(3, 3, 100, 200);
    private static readonly TimeSpan s_start = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_duration = TimeSpan.FromMilliseconds(200);

    [Fact]
    public void New_Transition_Is_Idle_With_Identity_Values()
    {
        var transition = new MatrixTransition();

        Assert.False(transition.IsRunning);
        Assert.Equal(CoreMatrix.Identity, transition.From);
        Assert.Equal(CoreMatrix.Identity, transition.To);
        Assert.Equal(TimeSpan.Zero, transition.Duration);
        Assert.Equal(1.0, transition.GetProgress(TimeSpan.Zero));
        Assert.Equal(CoreMatrix.Identity, transition.GetCurrentValue(TimeSpan.Zero));
    }

    [Fact]
    public void Start_Stores_Values_And_Runs()
    {
        var transition = new MatrixTransition();

        transition.Start(s_from, s_to, s_duration, s_start);

        Assert.True(transition.IsRunning);
        Assert.Equal(s_from, transition.From);
        Assert.Equal(s_to, transition.To);
        Assert.Equal(s_duration, transition.Duration);
    }

    [Fact]
    public void GetProgress_Is_Linear_And_Clamped()
    {
        var transition = new MatrixTransition();
        transition.Start(s_from, s_to, s_duration, s_start);

        Assert.Equal(0.0, transition.GetProgress(s_start));
        Assert.Equal(0.25, transition.GetProgress(s_start + TimeSpan.FromMilliseconds(50)), 12);
        Assert.Equal(0.5, transition.GetProgress(s_start + TimeSpan.FromMilliseconds(100)), 12);
        Assert.Equal(1.0, transition.GetProgress(s_start + TimeSpan.FromMilliseconds(200)));
        Assert.Equal(1.0, transition.GetProgress(s_start + TimeSpan.FromSeconds(5)));
        Assert.Equal(0.0, transition.GetProgress(s_start - TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public void GetCurrentValue_At_Start_Returns_From()
    {
        var transition = new MatrixTransition();
        transition.Start(s_from, s_to, s_duration, s_start);

        Assert.Equal(s_from, transition.GetCurrentValue(s_start));
        Assert.True(transition.IsRunning);
    }

    [Fact]
    public void GetCurrentValue_Mid_Transition_Interpolates()
    {
        var transition = new MatrixTransition();
        transition.Start(s_from, s_to, s_duration, s_start);

        var value = transition.GetCurrentValue(s_start + TimeSpan.FromMilliseconds(100));

        AssertEx.Equal(MatrixMath.Interpolate(s_from, s_to, 0.5), value);
        AssertEx.Equal(MatrixMath.ScaleAndTranslate(2, 2, 50, 100), value);
        Assert.True(transition.IsRunning);
    }

    [Fact]
    public void GetCurrentValue_On_Completion_Returns_To_And_Stops()
    {
        var transition = new MatrixTransition();
        transition.Start(s_from, s_to, s_duration, s_start);

        var value = transition.GetCurrentValue(s_start + s_duration);

        Assert.Equal(s_to, value);
        Assert.False(transition.IsRunning);
        Assert.Equal(s_to, transition.GetCurrentValue(s_start));
    }

    [Fact]
    public void Stop_Ends_Transition_And_Jumps_To_Target()
    {
        var transition = new MatrixTransition();
        transition.Start(s_from, s_to, s_duration, s_start);

        transition.Stop();

        Assert.False(transition.IsRunning);
        Assert.Equal(1.0, transition.GetProgress(s_start));
        Assert.Equal(s_to, transition.GetCurrentValue(s_start + TimeSpan.FromMilliseconds(10)));
    }

    [Fact]
    public void Zero_Duration_Does_Not_Run()
    {
        var transition = new MatrixTransition();

        transition.Start(s_from, s_to, TimeSpan.Zero, s_start);

        Assert.False(transition.IsRunning);
        Assert.Equal(1.0, transition.GetProgress(s_start));
        Assert.Equal(s_to, transition.GetCurrentValue(s_start));
    }

    [Fact]
    public void Negative_Duration_Does_Not_Run()
    {
        var transition = new MatrixTransition();

        transition.Start(s_from, s_to, TimeSpan.FromMilliseconds(-10), s_start);

        Assert.False(transition.IsRunning);
        Assert.Equal(s_to, transition.GetCurrentValue(s_start));
    }

    [Fact]
    public void Same_Matrices_Do_Not_Run()
    {
        var transition = new MatrixTransition();

        transition.Start(s_to, s_to, s_duration, s_start);

        Assert.False(transition.IsRunning);
        Assert.Equal(s_to, transition.GetCurrentValue(s_start));
    }

    [Fact]
    public void Restart_Replaces_Previous_Transition()
    {
        var transition = new MatrixTransition();
        transition.Start(s_from, s_to, s_duration, s_start);

        var midValue = transition.GetCurrentValue(s_start + TimeSpan.FromMilliseconds(100));
        var restart = s_start + TimeSpan.FromMilliseconds(100);
        transition.Start(midValue, s_from, s_duration, restart);

        Assert.True(transition.IsRunning);
        Assert.Equal(midValue, transition.From);
        Assert.Equal(s_from, transition.To);
        Assert.Equal(midValue, transition.GetCurrentValue(restart));
        Assert.Equal(s_from, transition.GetCurrentValue(restart + s_duration));
    }
}
