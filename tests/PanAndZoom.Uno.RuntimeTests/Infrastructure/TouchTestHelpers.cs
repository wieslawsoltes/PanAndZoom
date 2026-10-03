// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.UI.Input.Preview.Injection;

namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

/// <summary>
/// Swipe directions used by <see cref="TouchTestHelpers.Swipe"/>.
/// </summary>
public enum TouchSwipeDirection
{
    Left,
    Right,
    Up,
    Down
}

/// <summary>
/// Real touch injection helpers expressed in <see cref="ZoomBorder"/> coordinates.
/// These are the Uno equivalents of the Avalonia <c>TouchInputSimulator</c> helpers: instead of raising
/// synthetic gesture events they inject real touch contacts, so the WinUI manipulation engine produces
/// the pinch/scroll gestures handled by the control.
/// </summary>
public static class TouchTestHelpers
{
    private static uint s_nextTouchId = 20000;

    internal static uint NextTouchId() => s_nextTouchId++;

    /// <summary>
    /// Gets the center of the control in control coordinates.
    /// </summary>
    public static Point Center(FrameworkElement element) => new(element.ActualWidth / 2, element.ActualHeight / 2);

    /// <summary>
    /// Performs a real two finger horizontal pinch around <paramref name="center"/> (control coordinates)
    /// whose finger distance goes from <paramref name="startDistance"/> to <paramref name="endDistance"/>.
    /// </summary>
    public static void Pinch(UIElement target, Point center, double startDistance, double endDistance, int steps = 10)
    {
        InputHelper.Pinch(ZoomBorderTestHelper.ToWindow(target, center), startDistance, endDistance, steps);
    }

    /// <summary>
    /// Performs a real two finger pinch with the provided cumulative <paramref name="scale"/> around
    /// <paramref name="center"/> (control coordinates).
    /// </summary>
    public static void PinchScale(UIElement target, double scale, Point center, double startDistance = 100, int steps = 10)
    {
        Pinch(target, center, startDistance, startDistance * scale, steps);
    }

    /// <summary>
    /// Performs a real one finger drag that produces a scroll gesture with the provided delta.
    /// </summary>
    /// <remarks>
    /// The Avalonia scroll gesture delta uses scroll semantics (positive scrolls content left/up), while
    /// a finger drags the content with it, so the finger moves by the negated delta.
    /// </remarks>
    public static void ScrollGesture(UIElement target, double deltaX, double deltaY, Point? start = null, int steps = 10)
    {
        var from = start ?? (target is FrameworkElement fe ? Center(fe) : new Point(100, 100));
        Drag(target, from, new Point(from.X - deltaX, from.Y - deltaY), steps);
    }

    /// <summary>
    /// Performs a real one finger drag between two control positions.
    /// </summary>
    public static void Drag(UIElement target, Point from, Point to, int steps = 10)
    {
        InputHelper.TouchDrag(ZoomBorderTestHelper.ToWindow(target, from), ZoomBorderTestHelper.ToWindow(target, to), Math.Max(1, steps));
    }

    /// <summary>
    /// Performs a real one finger swipe (a drag with ~60fps steps over <paramref name="duration"/> milliseconds).
    /// </summary>
    public static void Swipe(UIElement target, Point start, TouchSwipeDirection direction, double distance = 100, int duration = 200)
    {
        var end = direction switch
        {
            TouchSwipeDirection.Left => new Point(start.X - distance, start.Y),
            TouchSwipeDirection.Right => new Point(start.X + distance, start.Y),
            TouchSwipeDirection.Up => new Point(start.X, start.Y - distance),
            _ => new Point(start.X, start.Y + distance)
        };

        using var session = new TouchInjectionSession(target);
        var id = session.TouchDown(start);
        var steps = Math.Max(1, duration / 16);
        for (var i = 1; i <= steps; i++)
        {
            session.AdvanceTime(16);
            session.TouchMove(id, Lerp(start, end, i / (double)steps));
        }

        session.TouchUp(id);
    }

    /// <summary>
    /// Performs a real one finger tap.
    /// </summary>
    public static void Tap(UIElement target, Point position, int holdTime = 50)
    {
        using var session = new TouchInjectionSession(target);
        session.Tap(position, holdTime);
    }

    /// <summary>
    /// Performs a real one finger double tap.
    /// </summary>
    public static void DoubleTap(UIElement target, Point position, int tapInterval = 100)
    {
        using var session = new TouchInjectionSession(target);
        session.Tap(position);
        session.AdvanceTime(tapInterval);
        session.Tap(position);
    }

    /// <summary>
    /// Performs a real two finger pan: both fingers (separated horizontally by <paramref name="fingerSpacing"/>)
    /// move from <paramref name="start"/> to <paramref name="end"/> (control coordinates).
    /// </summary>
    public static void TwoFingerPan(UIElement target, Point start, Point end, double fingerSpacing = 50, int steps = 10)
    {
        var half = fingerSpacing / 2;
        TwoFingerPath(target, t =>
        {
            var c = Lerp(start, end, t);
            return (new Point(c.X - half, c.Y), new Point(c.X + half, c.Y));
        }, steps);
    }

    /// <summary>
    /// Performs a real two finger rotation around <paramref name="center"/> (control coordinates).
    /// </summary>
    public static void Rotate(UIElement target, Point center, double radius, double startAngle, double endAngle, int steps = 10)
    {
        TwoFingerPath(target, t =>
        {
            var angle = (startAngle + (endAngle - startAngle) * t) * Math.PI / 180.0;
            return (
                new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle)),
                new Point(center.X + radius * Math.Cos(angle + Math.PI), center.Y + radius * Math.Sin(angle + Math.PI)));
        }, steps);
    }

    /// <summary>
    /// Performs a real two finger gesture where the finger positions (control coordinates) are provided by
    /// <paramref name="path"/> for t in [0, 1].
    /// </summary>
    public static void TwoFingerPath(UIElement target, Func<double, (Point First, Point Second)> path, int steps = 10)
    {
        using var session = new TouchInjectionSession(target);
        var (start1, start2) = path(0);
        var id1 = session.TouchDown(start1);
        var id2 = session.TouchDown(start2);

        for (var step = 1; step <= steps; step++)
        {
            var (p1, p2) = path(step / (double)steps);
            session.MoveMany((id1, p1), (id2, p2));
        }

        session.TouchUp(id2);
        session.TouchUp(id1);
    }

    /// <summary>
    /// Drives a complete manipulation directly through the control manipulation handlers.
    /// Used for values that cannot be produced by real touch contacts inside the test window.
    /// </summary>
    public static void ManipulationTranslate(ZoomBorder zoomBorder, Point translation)
    {
        zoomBorder.HandleManipulationStarted();
        zoomBorder.HandleManipulationDelta(Center(zoomBorder), translation, 1.0, 0.0, 1.0);
        zoomBorder.HandleManipulationCompleted();
    }

    /// <summary>
    /// Runs <paramref name="action"/> and returns the thrown exception (or null).
    /// </summary>
    public static Exception? Record(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    internal static Point Lerp(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}

/// <summary>
/// A real touch injection session with individually controlled contacts (the Uno equivalent of the
/// Avalonia <c>TouchInputSimulator</c> TouchDown/TouchMove/TouchUp API). Positions are in
/// <see cref="Target"/> coordinates. Disposing the session releases all active contacts.
/// </summary>
public sealed class TouchInjectionSession : IDisposable
{
    private const InjectedInputPointerOptions DownOptions = InjectedInputPointerOptions.New | InjectedInputPointerOptions.PointerDown | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.FirstButton;
    private const InjectedInputPointerOptions MoveOptions = InjectedInputPointerOptions.Update | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.FirstButton;
    private const InjectedInputPointerOptions UpOptions = InjectedInputPointerOptions.PointerUp | InjectedInputPointerOptions.FirstButton;

    private readonly Dictionary<uint, Point> _active = new();
    private ulong _timestamp;
    private uint _pendingTime;
    private bool _disposed;

    public TouchInjectionSession(UIElement target)
    {
        Target = target;
        InputHelper.EnsureTouchInjection();
    }

    /// <summary>
    /// Gets the element used as the coordinate space for contact positions.
    /// </summary>
    public UIElement Target { get; }

    /// <summary>
    /// Gets the active contacts (id and last position in target coordinates).
    /// </summary>
    public IReadOnlyDictionary<uint, Point> ActiveTouchPoints => _active;

    /// <summary>
    /// Gets the injected time in milliseconds (sum of the injected frame time offsets plus pending time).
    /// </summary>
    public ulong Timestamp => _timestamp + _pendingTime;

    /// <summary>
    /// Delays the next injected frame by <paramref name="milliseconds"/>.
    /// </summary>
    public void AdvanceTime(int milliseconds)
    {
        _pendingTime += (uint)milliseconds;
    }

    /// <summary>
    /// Puts a new finger down.
    /// </summary>
    public uint TouchDown(Point position)
    {
        var id = TouchTestHelpers.NextTouchId();
        _active[id] = position;
        Inject(Contact(id, position, DownOptions));
        return id;
    }

    /// <summary>
    /// Moves an active finger.
    /// </summary>
    public void TouchMove(uint id, Point position)
    {
        EnsureActive(id);
        _active[id] = position;
        Inject(Contact(id, position, MoveOptions));
    }

    /// <summary>
    /// Moves several active fingers in one injected frame.
    /// </summary>
    public void MoveMany(params (uint Id, Point Position)[] moves)
    {
        var contacts = new List<InjectedInputTouchInfo>();
        foreach (var (id, position) in moves)
        {
            EnsureActive(id);
            _active[id] = position;
            contacts.Add(Contact(id, position, MoveOptions));
        }

        Inject(contacts.ToArray());
    }

    /// <summary>
    /// Lifts an active finger.
    /// </summary>
    public void TouchUp(uint id)
    {
        EnsureActive(id);
        var position = _active[id];
        _active.Remove(id);
        Inject(Contact(id, position, UpOptions));
    }

    /// <summary>
    /// Taps (down, hold, up) at a position.
    /// </summary>
    public void Tap(Point position, int holdTime = 50)
    {
        var id = TouchDown(position);
        AdvanceTime(holdTime);
        TouchUp(id);
    }

    /// <summary>
    /// Lifts all active fingers and resets the injected time.
    /// </summary>
    public void Reset()
    {
        foreach (var id in _active.Keys.ToList())
        {
            TouchUp(id);
        }

        _timestamp = 0;
        _pendingTime = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            foreach (var id in _active.Keys.ToList())
            {
                TouchUp(id);
            }
        }
        finally
        {
            InputHelper.UninitializeTouchInjection();
        }
    }

    private void EnsureActive(uint id)
    {
        if (!_active.ContainsKey(id))
        {
            throw new InvalidOperationException($"Touch point {id} is not active.");
        }
    }

    private void Inject(params InjectedInputTouchInfo[] contacts)
    {
        // Each frame is offset by the advanced time (or 1ms when no time was advanced) so timestamps are increasing.
        var offset = _pendingTime > 0 ? _pendingTime : 1u;
        _pendingTime = 0;
        _timestamp += offset;
        for (var i = 0; i < contacts.Length; i++)
        {
            contacts[i].PointerInfo = contacts[i].PointerInfo with { TimeOffsetInMilliseconds = offset };
        }

        InputHelper.InjectTouch(contacts);
    }

    private InjectedInputTouchInfo Contact(uint id, Point position, InjectedInputPointerOptions options)
    {
        var window = ZoomBorderTestHelper.ToWindow(Target, position);
        return new InjectedInputTouchInfo
        {
            PointerInfo = new InjectedInputPointerInfo
            {
                PointerId = InjectionCoordinates.ToInjectedPointerId(id),
                PixelLocation = InjectionCoordinates.ToInjectedPoint(window),
                PointerOptions = options,
                TimeOffsetInMilliseconds = 1
            },
            Contact = new InjectedInputRectangle { Left = 2, Top = 2, Right = 2, Bottom = 2 },
            Pressure = 1.0
        };
    }
}

/// <summary>
/// Records the touch pointer events received by an element (the Uno equivalent of counting the
/// pointer events raised by the Avalonia <c>TouchInputSimulator</c>).
/// </summary>
public sealed class TouchPointerRecorder : IDisposable
{
    private readonly UIElement _target;
    private readonly Dictionary<uint, Point> _active = new();

    public TouchPointerRecorder(UIElement target)
    {
        _target = target;
        _target.PointerPressed += OnPressed;
        _target.PointerMoved += OnMoved;
        _target.PointerReleased += OnReleased;
        _target.PointerCanceled += OnCanceled;
    }

    public int PressedCount { get; private set; }

    public int MovedCount { get; private set; }

    public int ReleasedCount { get; private set; }

    public int CanceledCount { get; private set; }

    /// <summary>
    /// Gets the touch pointers currently pressed on the element and their last position (element coordinates).
    /// </summary>
    public IReadOnlyDictionary<uint, Point> ActivePointers => _active;

    /// <summary>
    /// Gets the recorded events (kind, pointer id, position, timestamp in microseconds).
    /// </summary>
    public List<(string Kind, uint Id, Point Position, ulong Timestamp)> Events { get; } = new();

    public int TotalCount => PressedCount + MovedCount + ReleasedCount + CanceledCount;

    public void Dispose()
    {
        _target.PointerPressed -= OnPressed;
        _target.PointerMoved -= OnMoved;
        _target.PointerReleased -= OnReleased;
        _target.PointerCanceled -= OnCanceled;
    }

    private bool Record(string kind, PointerRoutedEventArgs e, out uint id, out Point position)
    {
        id = e.Pointer.PointerId;
        position = default;
        if (e.Pointer.PointerDeviceType != PointerDeviceType.Touch)
        {
            return false;
        }

        id = InjectionCoordinates.ToLogicalPointerId(id, kind == "Pressed", e.GetCurrentPoint(null).Position);

        var point = e.GetCurrentPoint(_target);
        position = point.Position;
        Events.Add((kind, id, position, point.Timestamp));
        return true;
    }

    private void OnPressed(object sender, PointerRoutedEventArgs e)
    {
        if (Record("Pressed", e, out var id, out var position))
        {
            PressedCount++;
            _active[id] = position;
        }
    }

    private void OnMoved(object sender, PointerRoutedEventArgs e)
    {
        if (Record("Moved", e, out var id, out var position))
        {
            MovedCount++;
            if (_active.ContainsKey(id))
            {
                _active[id] = position;
            }
        }
    }

    private void OnReleased(object sender, PointerRoutedEventArgs e)
    {
        if (Record("Released", e, out var id, out _))
        {
            ReleasedCount++;
            _active.Remove(id);
        }
    }

    private void OnCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (Record("Canceled", e, out var id, out _))
        {
            CanceledCount++;
            _active.Remove(id);
        }
    }
}
