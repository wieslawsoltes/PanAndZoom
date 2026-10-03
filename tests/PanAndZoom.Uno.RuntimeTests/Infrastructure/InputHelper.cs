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

#if PANANDZOOM_WINUI
    // The native WinUI app uses its own injector: the engine InputInjectorHelper.CleanupPointers()
    // releases all buttons in a single event, which Windows rejects (X buttons need MouseData).
    private static InputInjector? s_injector;
    private static readonly HashSet<ButtonName> s_pressedButtons = new();

    /// <summary>
    /// Gets the input injector shared by the test infrastructure.
    /// </summary>
    public static InputInjector RawInjector => s_injector ??= InputInjector.TryCreate()
        ?? throw new InvalidOperationException("Input injection is not available.");
#else
    /// <summary>
    /// Gets the input injector shared by the test infrastructure.
    /// </summary>
    public static InputInjector RawInjector => Injector.Injector;
#endif

#if PANANDZOOM_WINUI
    private static readonly HashSet<Windows.System.VirtualKey> s_pressedKeys = new();
    private static Point? s_mousePosition;
#endif

    /// <summary>
    /// Releases modifier keys left pressed by <see cref="MouseWheel(Point, int, VirtualKeyModifiers, bool)"/> (Windows only).
    /// </summary>
    /// <param name="except">Keys to keep pressed.</param>
    public static void ReleaseModifierKeys(IEnumerable<Windows.System.VirtualKey>? except = null)
    {
#if PANANDZOOM_WINUI
        var keep = except?.ToHashSet() ?? new HashSet<Windows.System.VirtualKey>();
        var release = s_pressedKeys.Where(k => !keep.Contains(k)).ToList();
        if (release.Count == 0)
        {
            return;
        }

        var keyUps = release.Select(k => new InjectedInputKeyboardInfo { VirtualKey = (ushort)k, KeyOptions = InjectedInputKeyOptions.KeyUp }).ToList();
        WinUIInputPump.Post(() => RawInjector.InjectKeyboardInput(keyUps));
        s_pressedKeys.ExceptWith(release);
#endif
    }

    private static bool s_touchInitialized;
    private static bool s_penInitialized;

    /// <summary>
    /// Initializes touch injection if needed (Windows requires it before injecting touch input).
    /// </summary>
    public static void EnsureTouchInjection()
    {
#if PANANDZOOM_WINUI
        WinUIInputPump.Post(EnsureTouchInjectionNow);
#else
        EnsureTouchInjectionNow();
#endif
    }

    internal static void EnsureTouchInjectionNow()
    {
        if (!s_touchInitialized)
        {
            RawInjector.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
            s_touchInitialized = true;
        }
    }

    // Windows requires pen injection to be initialized before injecting pen input.
    private static void EnsurePenInjectionNow()
    {
        if (!s_penInitialized)
        {
            RawInjector.InitializePenInjection(InjectedInputVisualizationMode.Default);
            s_penInitialized = true;
        }
    }

    /// <summary>
    /// Injects a touch frame. All touch injection of the test infrastructure goes through this method.
    /// </summary>
    /// <remarks>
    /// Uno accepts frames describing only the contacts that changed. On native WinUI the frame is queued and
    /// merged with the active contacts by <see cref="WinUIInputPump"/>, and a time offset longer than a frame
    /// (for example a held tap) is turned into a real delay.
    /// </remarks>
    public static void InjectTouch(IEnumerable<InjectedInputTouchInfo> frame)
    {
#if PANANDZOOM_WINUI
        var frameList = frame.ToList();
        var offset = frameList.Count == 0 ? 0u : frameList.Max(info => info.PointerInfo.TimeOffsetInMilliseconds);
        WinUIInputPump.PostDelay((int)offset);
        WinUIInputPump.PostTouch(InjectionCoordinates.Prepare(frameList).ToList());
#else
        EnsureTouchInjectionNow();
        RawInjector.InjectTouchInput(frame);
#endif
    }

    /// <summary>
    /// Injects a pen frame.
    /// </summary>
    public static void InjectPen(InjectedInputPenInfo info)
    {
#if PANANDZOOM_WINUI
        WinUIInputPump.Post(() =>
        {
            EnsurePenInjectionNow();
            RawInjector.InjectPenInput(info);
        });
#else
        EnsurePenInjectionNow();
        RawInjector.InjectPenInput(info);
#endif
    }

    /// <summary>
    /// Uninitializes touch and pen injection immediately (cancels any contact left pressed and, on native
    /// WinUI, drops the queued input). Safe to call when not initialized.
    /// </summary>
    public static void ResetTouchAndPen()
    {
#if PANANDZOOM_WINUI
        WinUIInputPump.Reset();
        InjectionCoordinates.ResetPointerIds();
#endif
        UninitializeTouchInjectionNow();
        UninitializePenInjectionNow();
    }

    /// <summary>
    /// Uninitializes touch injection (after the queued input on native WinUI). Safe to call when not initialized.
    /// </summary>
    public static void UninitializeTouchInjection()
    {
#if PANANDZOOM_WINUI
        WinUIInputPump.Post(UninitializeTouchInjectionNow);
#else
        UninitializeTouchInjectionNow();
#endif
    }

    private static void UninitializeTouchInjectionNow()
    {
        try
        {
            RawInjector.UninitializeTouchInjection();
        }
        catch (Exception)
        {
            // Not initialized.
        }

        s_touchInitialized = false;
#if PANANDZOOM_WINUI
        WinUIInputPump.ForgetTouchContacts();
#endif
    }

    private static void UninitializePenInjectionNow()
    {
        try
        {
            RawInjector.UninitializePenInjection();
        }
        catch (Exception)
        {
            // Not initialized.
        }

        s_penInitialized = false;
    }

    private static void InjectMouse(IEnumerable<InjectedInputMouseInfo> input)
    {
#if PANANDZOOM_WINUI
        // One injected mouse event per frame (Windows coalesces bursts of input).
        foreach (var info in input)
        {
            WinUIInputPump.Post(() => RawInjector.InjectMouseInput(new[] { info }));
        }
#else
        Injector.InjectMouseInput(input);
#endif
    }

    private static void InjectMouse(InjectedInputMouseInfo input) => InjectMouse(new[] { input });

    /// <summary>
    /// Releases any pressed mouse button and moves the mouse to the window origin.
    /// </summary>
    public static void Reset()
    {
#if PANANDZOOM_WINUI
        foreach (var button in s_pressedButtons.ToArray())
        {
            MouseUp(button);
        }

        ReleaseModifierKeys();
        ReleaseStuckModifierKeys();
#else
        Injector.CleanupPointers();
#endif
    }

#if PANANDZOOM_WINUI
    private static readonly Windows.System.VirtualKey[] s_modifierKeys =
    {
        Windows.System.VirtualKey.LeftControl, Windows.System.VirtualKey.RightControl,
        Windows.System.VirtualKey.LeftShift, Windows.System.VirtualKey.RightShift,
        Windows.System.VirtualKey.LeftMenu, Windows.System.VirtualKey.RightMenu,
        Windows.System.VirtualKey.LeftWindows, Windows.System.VirtualKey.RightWindows
    };

    // The Windows keyboard state is global: a modifier key left down by anything else on the desktop
    // (another test run, a remote session) would change how the control interprets wheel input.
    private static void ReleaseStuckModifierKeys()
    {
        var stuck = s_modifierKeys.Where(key => (GetAsyncKeyState((int)key) & 0x8000) != 0).ToList();
        if (stuck.Count == 0)
        {
            return;
        }

        PanAndZoom.Uno.RuntimeTests.WinUITestRunner.Log($"Releasing modifier keys left down on the desktop: {string.Join(", ", stuck)}.");
        var keyUps = stuck.Select(k => new InjectedInputKeyboardInfo { VirtualKey = (ushort)k, KeyOptions = InjectedInputKeyOptions.KeyUp }).ToList();
        WinUIInputPump.Post(() => RawInjector.InjectKeyboardInput(keyUps));
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
#endif

    /// <summary>
    /// Gets the current (injected) mouse position in window coordinates.
    /// </summary>
    public static Point GetMousePosition()
    {
#if PANANDZOOM_WINUI
        // While input is queued the cursor has not reached its injected position yet.
        return s_mousePosition is { } position && !WinUIInputPump.IsIdle
            ? position
            : InjectionCoordinates.GetCursorWindowPosition();
#else
        var mouse = typeof(InputInjector).GetProperty("Mouse", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Injector.Injector)!;
        return (Point)mouse.GetType().GetProperty("Position", BindingFlags.Instance | BindingFlags.Public)!.GetValue(mouse)!;
#endif
    }

    /// <summary>
    /// Moves the mouse to the provided window position (rounded to whole pixels) using exact integer steps.
    /// </summary>
    public static void MouseMoveTo(Point position, int? steps = null)
    {
#if PANANDZOOM_WINUI
        // Relative moves are subject to the Windows pointer acceleration, use absolute moves.
        var start = GetMousePosition();
        var count = Math.Max(1, steps ?? 1);
        var absoluteMoves = new List<InjectedInputMouseInfo>();
        for (var i = 1; i <= count; i++)
        {
            var t = i / (double)count;
            absoluteMoves.Add(InjectionCoordinates.CreateAbsoluteMove(new Point(
                Math.Round(start.X + (position.X - start.X) * t),
                Math.Round(start.Y + (position.Y - start.Y) * t))));
        }

        InjectMouse(absoluteMoves);
        s_mousePosition = new Point(Math.Round(position.X), Math.Round(position.Y));
        return;
#else
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

        InjectMouse(moves);
#endif
    }

    /// <summary>
    /// Presses a mouse button at the current position.
    /// </summary>
    public static void MouseDown(ButtonName button)
    {
        InjectMouse(new InjectedInputMouseInfo
        {
            TimeOffsetInMilliseconds = 1,
            MouseOptions = button switch
            {
                ButtonName.Left => InjectedInputMouseOptions.LeftDown,
                ButtonName.Right => InjectedInputMouseOptions.RightDown,
                _ => InjectedInputMouseOptions.MiddleDown
            }
        });

#if PANANDZOOM_WINUI
        s_pressedButtons.Add(button);
#endif
    }

    /// <summary>
    /// Releases a mouse button at the current position.
    /// </summary>
    public static void MouseUp(ButtonName button)
    {
        InjectMouse(new InjectedInputMouseInfo
        {
            TimeOffsetInMilliseconds = 1,
            MouseOptions = button switch
            {
                ButtonName.Left => InjectedInputMouseOptions.LeftUp,
                ButtonName.Right => InjectedInputMouseOptions.RightUp,
                _ => InjectedInputMouseOptions.MiddleUp
            }
        });

#if PANANDZOOM_WINUI
        s_pressedButtons.Remove(button);
#endif
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
        InjectMouse(CreateWheel(delta, horizontal));
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

#if PANANDZOOM_WINUI
        // Windows injects real modifier keys. WinUI reads the modifier state when it dispatches the
        // wheel event (asynchronously), so the keys stay pressed until the input settled: they are
        // released by ReleaseModifierKeys (called by ZoomBorderTestHelper.WaitForIdleAsync and Reset).
        var keys = new List<Windows.System.VirtualKey>();
        if ((modifiers & VirtualKeyModifiers.Control) != 0) keys.Add(Windows.System.VirtualKey.LeftControl);
        if ((modifiers & VirtualKeyModifiers.Shift) != 0) keys.Add(Windows.System.VirtualKey.LeftShift);
        if ((modifiers & VirtualKeyModifiers.Menu) != 0) keys.Add(Windows.System.VirtualKey.LeftMenu);

        ReleaseModifierKeys(except: keys);
        var newKeys = keys.Where(k => !s_pressedKeys.Contains(k)).ToList();
        if (newKeys.Count > 0)
        {
            // Windows rejects an empty keyboard input list.
            var keyDowns = newKeys.Select(k => new InjectedInputKeyboardInfo { VirtualKey = (ushort)k }).ToList();
            WinUIInputPump.Post(() => RawInjector.InjectKeyboardInput(keyDowns));
        }

        s_pressedKeys.UnionWith(newKeys);
        InjectMouse(CreateWheel(delta, horizontal));
#else
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
#endif
    }

    private static InjectedInputMouseInfo CreateWheel(int delta, bool horizontal)
    {
        // WinUI reads the wheel delta from MouseData while Uno reads it from DeltaX/DeltaY, so set both.
        return new InjectedInputMouseInfo
        {
            TimeOffsetInMilliseconds = 1,
            MouseData = unchecked((uint)delta),
#if PANANDZOOM_WINUI
            // Windows reads the wheel delta from MouseData only.
            DeltaX = 0,
            DeltaY = 0,
#else
            DeltaX = horizontal ? delta : 0,
            DeltaY = horizontal ? 0 : delta,
#endif
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
        try
        {
            foreach (var frame in frames)
            {
                InjectTouch(frame);
            }
        }
        finally
        {
            UninitializeTouchInjection();
        }
    }

    private static InjectedInputTouchInfo Contact(uint id, Point position, InjectedInputPointerOptions options)
    {
        return new InjectedInputTouchInfo
        {
            PointerInfo = new InjectedInputPointerInfo
            {
                PointerId = InjectionCoordinates.ToInjectedPointerId(id),
                PixelLocation = InjectionCoordinates.ToInjectedPoint(position),
                PointerOptions = options,
                TimeOffsetInMilliseconds = 1
            },
            Contact = new InjectedInputRectangle { Left = 2, Top = 2, Right = 2, Bottom = 2 },
            Pressure = 1.0
        };
    }

    private static Point Lerp(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}
