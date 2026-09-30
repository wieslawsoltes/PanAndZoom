// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Reflection;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

/// <summary>
/// Real input injection helpers (mouse and touch) built on the Uno <see cref="InputInjector"/>.
/// All coordinates are window coordinates (see <see cref="ZoomBorderTestHelper.ToWindow"/>).
/// </summary>
public static class InputHelper
{
    private static uint s_nextTouchId = 100;

    private static InputInjectorHelper Injector => InputInjectorHelper.Current;

    /// <summary>
    /// Releases any pressed mouse button and moves the mouse to the window origin.
    /// </summary>
    public static void Reset()
    {
        Injector.CleanupPointers();
    }

    /// <summary>
    /// Gets the current (injected) mouse position in window coordinates.
    /// </summary>
    public static Point GetMousePosition()
    {
        var mouse = typeof(InputInjector).GetProperty("Mouse", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Injector.Injector)!;
        return (Point)mouse.GetType().GetProperty("Position", BindingFlags.Instance | BindingFlags.Public)!.GetValue(mouse)!;
    }

    /// <summary>
    /// Moves the mouse to the provided window position (rounded to whole pixels) using exact integer steps.
    /// </summary>
    public static void MouseMoveTo(Point position, int? steps = null)
    {
        var current = GetMousePosition();
        var totalX = (int)Math.Round(position.X - current.X);
        var totalY = (int)Math.Round(position.Y - current.Y);
        var count = Math.Max(1, steps ?? 1);

        var movedX = 0;
        var movedY = 0;
        var moves = new List<InjectedInputMouseInfo>();
        for (var i = 1; i <= count; i++)
        {
            var targetX = (int)Math.Round(totalX * (double)i / count);
            var targetY = (int)Math.Round(totalY * (double)i / count);
            var dx = targetX - movedX;
            var dy = targetY - movedY;
            movedX = targetX;
            movedY = targetY;
            if (dx != 0 || dy != 0)
            {
                moves.Add(Injector.Mouse.MoveBy(dx, dy));
            }
        }

        Injector.InjectMouseInput(moves);
    }

    /// <summary>
    /// Presses a mouse button at the current position.
    /// </summary>
    public static void MouseDown(ButtonName button)
    {
        Injector.InjectMouseInput(new InjectedInputMouseInfo
        {
            TimeOffsetInMilliseconds = 1,
            MouseOptions = button switch
            {
                ButtonName.Left => InjectedInputMouseOptions.LeftDown,
                ButtonName.Right => InjectedInputMouseOptions.RightDown,
                _ => InjectedInputMouseOptions.MiddleDown
            }
        });
    }

    /// <summary>
    /// Releases a mouse button at the current position.
    /// </summary>
    public static void MouseUp(ButtonName button)
    {
        Injector.InjectMouseInput(new InjectedInputMouseInfo
        {
            TimeOffsetInMilliseconds = 1,
            MouseOptions = button switch
            {
                ButtonName.Left => InjectedInputMouseOptions.LeftUp,
                ButtonName.Right => InjectedInputMouseOptions.RightUp,
                _ => InjectedInputMouseOptions.MiddleUp
            }
        });
    }

    /// <summary>
    /// Drags the mouse from one window position to another with the provided button pressed.
    /// </summary>
    public static void MouseDrag(Point from, Point to, ButtonName button = ButtonName.Middle, int steps = 10)
    {
        Reset();
        MouseMoveTo(from);
        MouseDown(button);
        MouseMoveTo(to, steps);
        MouseUp(button);
    }

    /// <summary>
    /// Scrolls the mouse wheel at the provided window position.
    /// </summary>
    /// <param name="position">The window position.</param>
    /// <param name="delta">The wheel delta (120 per notch, positive scrolls up / zooms in).</param>
    /// <param name="horizontal">True to inject a horizontal wheel delta.</param>
    public static void MouseWheel(Point position, int delta, bool horizontal = false)
    {
        MouseMoveTo(position);
        Injector.InjectMouseInput(CreateWheel(delta, horizontal));
    }

    /// <summary>
    /// Scrolls the mouse wheel at the provided window position while keyboard modifiers are reported as pressed.
    /// </summary>
    /// <remarks>
    /// Uses the Uno internal modifier-aware injection overload, so the control receives the modifiers through
    /// <c>PointerRoutedEventArgs.KeyModifiers</c> exactly like real input.
    /// </remarks>
    public static void MouseWheel(Point position, int delta, VirtualKeyModifiers modifiers, bool horizontal = false)
    {
        MouseMoveTo(position);

        var method = typeof(InputInjector)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == nameof(InputInjector.InjectMouseInput)
                && m.ReturnType == typeof(void)
                && m.GetParameters() is [{ ParameterType: var type }]
                && type == typeof(IEnumerable<(InjectedInputMouseInfo, VirtualKeyModifiers)>));

        if (method == null)
        {
            throw new NotSupportedException("This version of Uno does not support modifier-aware mouse injection.");
        }

        IEnumerable<(InjectedInputMouseInfo, VirtualKeyModifiers)> input = new[] { (CreateWheel(delta, horizontal), modifiers) };
        method.Invoke(Injector.Injector, new object[] { input });
    }

    private static InjectedInputMouseInfo CreateWheel(int delta, bool horizontal)
    {
        // WinUI reads the wheel delta from MouseData while Uno reads it from DeltaX/DeltaY, so set both.
        return new InjectedInputMouseInfo
        {
            TimeOffsetInMilliseconds = 1,
            MouseData = unchecked((uint)delta),
            DeltaX = horizontal ? delta : 0,
            DeltaY = horizontal ? 0 : delta,
            MouseOptions = horizontal ? InjectedInputMouseOptions.HWheel : InjectedInputMouseOptions.Wheel
        };
    }

    /// <summary>
    /// Clicks the left mouse button at the provided window position.
    /// </summary>
    public static void MouseClick(Point position, ButtonName button = ButtonName.Left)
    {
        MouseMoveTo(position);
        MouseDown(button);
        MouseUp(button);
    }

    /// <summary>
    /// Double clicks the left mouse button at the provided window position.
    /// </summary>
    public static void MouseDoubleClick(Point position)
    {
        Reset();
        MouseClick(position);
        MouseClick(position);
    }

    /// <summary>
    /// Drags a single finger from one window position to another.
    /// </summary>
    public static void TouchDrag(Point from, Point to, int steps = 10)
    {
        var id = s_nextTouchId++;
        Touch(Frames());

        IEnumerable<InjectedInputTouchInfo[]> Frames()
        {
            yield return new[] { Contact(id, from, InjectedInputPointerOptions.New | InjectedInputPointerOptions.PointerDown | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.FirstButton) };

            for (var step = 1; step <= steps; step++)
            {
                var p = Lerp(from, to, step / (double)steps);
                yield return new[] { Contact(id, p, InjectedInputPointerOptions.Update | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.FirstButton) };
            }

            yield return new[] { Contact(id, to, InjectedInputPointerOptions.PointerUp | InjectedInputPointerOptions.FirstButton) };
        }
    }

    /// <summary>
    /// Performs a two finger gesture: both fingers start at <paramref name="start1"/>/<paramref name="start2"/>
    /// and move linearly to <paramref name="end1"/>/<paramref name="end2"/>.
    /// </summary>
    public static void TwoFingerGesture(Point start1, Point start2, Point end1, Point end2, int steps = 10)
    {
        var id1 = s_nextTouchId++;
        var id2 = s_nextTouchId++;
        Touch(Frames());

        IEnumerable<InjectedInputTouchInfo[]> Frames()
        {
            const InjectedInputPointerOptions down = InjectedInputPointerOptions.New | InjectedInputPointerOptions.PointerDown | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.FirstButton;
            const InjectedInputPointerOptions move = InjectedInputPointerOptions.Update | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.FirstButton;
            const InjectedInputPointerOptions up = InjectedInputPointerOptions.PointerUp | InjectedInputPointerOptions.FirstButton;

            yield return new[] { Contact(id1, start1, down) };
            yield return new[] { Contact(id1, start1, move), Contact(id2, start2, down) };

            for (var step = 1; step <= steps; step++)
            {
                var t = step / (double)steps;
                yield return new[] { Contact(id1, Lerp(start1, end1, t), move), Contact(id2, Lerp(start2, end2, t), move) };
            }

            yield return new[] { Contact(id1, end1, move), Contact(id2, end2, up) };
            yield return new[] { Contact(id1, end1, up) };
        }
    }

    /// <summary>
    /// Performs a two finger pinch around a center point.
    /// </summary>
    /// <param name="center">The pinch center in window coordinates.</param>
    /// <param name="startDistance">The initial distance between the fingers.</param>
    /// <param name="endDistance">The final distance between the fingers.</param>
    /// <param name="steps">The number of move steps.</param>
    public static void Pinch(Point center, double startDistance, double endDistance, int steps = 10)
    {
        TwoFingerGesture(
            new Point(center.X - startDistance / 2, center.Y),
            new Point(center.X + startDistance / 2, center.Y),
            new Point(center.X - endDistance / 2, center.Y),
            new Point(center.X + endDistance / 2, center.Y),
            steps);
    }

    private static void Touch(IEnumerable<InjectedInputTouchInfo[]> frames)
    {
        Injector.Injector.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
        try
        {
            foreach (var frame in frames)
            {
                Injector.Injector.InjectTouchInput(frame);
            }
        }
        finally
        {
            Injector.Injector.UninitializeTouchInjection();
        }
    }

    private static InjectedInputTouchInfo Contact(uint id, Point position, InjectedInputPointerOptions options)
    {
        return new InjectedInputTouchInfo
        {
            PointerInfo = new InjectedInputPointerInfo
            {
                PointerId = id,
                PixelLocation = new InjectedInputPoint { PositionX = (int)Math.Round(position.X), PositionY = (int)Math.Round(position.Y) },
                PointerOptions = options,
                TimeOffsetInMilliseconds = 1
            },
            Contact = new InjectedInputRectangle { Left = 2, Top = 2, Right = 2, Bottom = 2 },
            Pressure = 1.0
        };
    }

    private static Point Lerp(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}
