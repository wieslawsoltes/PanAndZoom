// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Collections;
using System.Reflection;

namespace PanAndZoom.Core.UnitTests.Settings;

public class ZoomBorderDefaultsTests
{
    public static TheoryData<string> SettingNames()
    {
        var data = new TheoryData<string>();
        foreach (var property in typeof(IPanAndZoomSettings).GetProperties())
        {
            data.Add(property.Name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SettingNames))]
    public void PanAndZoomSettings_Default_Equals_ZoomBorderDefaults(string name)
    {
        var settings = new PanAndZoomSettings();
        var settingsValue = typeof(PanAndZoomSettings).GetProperty(name)!.GetValue(settings);
        var defaultValue = GetDefault(name);

        if (defaultValue is IEnumerable expectedSequence and not string)
        {
            var actualSequence = Assert.IsAssignableFrom<IEnumerable>(settingsValue);
            Assert.Equal(expectedSequence.Cast<object>(), actualSequence.Cast<object>());
        }
        else
        {
            Assert.Equal(defaultValue, settingsValue);
        }
    }

    [Fact]
    public void Every_Setting_Has_A_Default()
    {
        foreach (var property in typeof(IPanAndZoomSettings).GetProperties())
        {
            Assert.True(HasDefault(property.Name), $"ZoomBorderDefaults has no member named {property.Name}.");
        }
    }

    [Fact]
    public void PanAndZoomSettings_Implements_Every_Setting_As_Writable_Property()
    {
        foreach (var property in typeof(IPanAndZoomSettings).GetProperties())
        {
            var implementation = typeof(PanAndZoomSettings).GetProperty(property.Name);
            Assert.NotNull(implementation);
            Assert.True(implementation!.CanWrite, $"{property.Name} is not writable.");
        }
    }

    [Fact]
    public void Key_Default_Values()
    {
        Assert.Equal(ButtonName.Middle, ZoomBorderDefaults.PanButton);
        Assert.Equal(1.2, ZoomBorderDefaults.ZoomSpeed);
        Assert.Equal(1.0, ZoomBorderDefaults.PowerFactor);
        Assert.Equal(0.5, ZoomBorderDefaults.TransitionThreshold);
        Assert.Equal(StretchMode.Uniform, ZoomBorderDefaults.Stretch);
        Assert.True(ZoomBorderDefaults.EnableConstrains);
        Assert.Equal(double.NegativeInfinity, ZoomBorderDefaults.MinZoomX);
        Assert.Equal(double.PositiveInfinity, ZoomBorderDefaults.MaxZoomX);
        Assert.Equal(TimeSpan.FromMilliseconds(300), ZoomBorderDefaults.AnimationDuration);
        Assert.False(ZoomBorderDefaults.EnableAnimations);
        Assert.Equal(DoubleClickZoomMode.ZoomInOut, ZoomBorderDefaults.DoubleClickZoomMode);
        Assert.Equal(2.0, ZoomBorderDefaults.DoubleClickZoomFactor);
        Assert.Equal(1.5, ZoomBorderDefaults.DoubleClickZoomThreshold);
        Assert.Equal(ContentBoundsMode.Unrestricted, ZoomBorderDefaults.BoundsMode);
        Assert.Equal(new CoreThickness(0), ZoomBorderDefaults.BoundsPadding);
        Assert.Equal(ResizeBehaviorMode.None, ZoomBorderDefaults.ResizeBehavior);
        Assert.Equal(WheelBehaviorMode.Zoom, ZoomBorderDefaults.WheelBehavior);
        Assert.Equal(WheelBehaviorMode.Zoom, ZoomBorderDefaults.WheelWithCtrl);
        Assert.Equal(WheelBehaviorMode.PanHorizontal, ZoomBorderDefaults.WheelWithShift);
        Assert.Equal(50.0, ZoomBorderDefaults.KeyboardPanStep);
        Assert.Equal(1.1, ZoomBorderDefaults.KeyboardZoomStep);
        Assert.Equal(50, ZoomBorderDefaults.ViewHistorySize);
        Assert.Equal(new[] { 0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 8.0 }, ZoomBorderDefaults.DiscreteZoomLevels);
        Assert.Equal(TimeSpan.FromSeconds(2), ZoomBorderDefaults.ZoomIndicatorAutoHideDuration);
        Assert.Equal(-180.0, ZoomBorderDefaults.MinRotation);
        Assert.Equal(180.0, ZoomBorderDefaults.MaxRotation);
        Assert.Equal(1, ZoomBorderDefaults.MinimumTouchPoints);
        Assert.Equal(2, ZoomBorderDefaults.MaximumTouchPoints);
        Assert.Equal(TimeSpan.Zero, ZoomBorderDefaults.GestureRecognitionDelay);
    }

    [Fact]
    public void DiscreteZoomLevels_Returns_A_New_Array_Each_Time()
    {
        var first = ZoomBorderDefaults.DiscreteZoomLevels;
        first[0] = 42;

        Assert.NotSame(first, ZoomBorderDefaults.DiscreteZoomLevels);
        Assert.Equal(0.25, ZoomBorderDefaults.DiscreteZoomLevels[0]);
    }

    [Fact]
    public void Settings_Instances_Do_Not_Share_DiscreteZoomLevels()
    {
        var a = new PanAndZoomSettings();
        var b = new PanAndZoomSettings();

        a.DiscreteZoomLevels![0] = 42;

        Assert.Equal(0.25, b.DiscreteZoomLevels![0]);
    }

    private static bool HasDefault(string name)
    {
        var type = typeof(ZoomBorderDefaults);
        return type.GetField(name, BindingFlags.Public | BindingFlags.Static) != null
            || type.GetProperty(name, BindingFlags.Public | BindingFlags.Static) != null;
    }

    private static object? GetDefault(string name)
    {
        var type = typeof(ZoomBorderDefaults);
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
        if (field != null)
        {
            return field.GetValue(null);
        }

        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(property);
        return property!.GetValue(null);
    }
}

public class ZoomBorderCommandTests
{
    [Fact]
    public void Constructor_Throws_For_Null_Execute()
    {
        Assert.Throws<ArgumentNullException>(() => new ZoomBorderCommand(null!));
    }

    [Fact]
    public void CanExecute_Defaults_To_True()
    {
        var command = new ZoomBorderCommand(() => { });

        Assert.True(command.CanExecute(null));
        Assert.True(command.CanExecute("parameter"));
    }

    [Fact]
    public void CanExecute_Uses_Predicate()
    {
        var allowed = false;
        var command = new ZoomBorderCommand(() => { }, () => allowed);

        Assert.False(command.CanExecute(null));

        allowed = true;

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void Execute_Invokes_Action()
    {
        var count = 0;
        var command = new ZoomBorderCommand(() => count++);

        command.Execute(null);
        command.Execute("ignored");

        Assert.Equal(2, count);
    }

    [Fact]
    public void RaiseCanExecuteChanged_Raises_Event_With_Sender()
    {
        var command = new ZoomBorderCommand(() => { });
        object? sender = null;
        EventArgs? args = null;
        command.CanExecuteChanged += (s, e) =>
        {
            sender = s;
            args = e;
        };

        command.RaiseCanExecuteChanged();

        Assert.Same(command, sender);
        Assert.Same(EventArgs.Empty, args);
    }

    [Fact]
    public void RaiseCanExecuteChanged_Without_Subscribers_Does_Not_Throw()
    {
        var command = new ZoomBorderCommand(() => { });

        command.RaiseCanExecuteChanged();
    }
}

public class ZoomChangedEventArgsTests
{
    [Fact]
    public void Constructor_Sets_Values()
    {
        var args = new ZoomChangedEventArgs(1.5, 2.5, -10, 20);

        Assert.Equal(1.5, args.ZoomX);
        Assert.Equal(2.5, args.ZoomY);
        Assert.Equal(-10, args.OffsetX);
        Assert.Equal(20, args.OffsetY);
    }
}
