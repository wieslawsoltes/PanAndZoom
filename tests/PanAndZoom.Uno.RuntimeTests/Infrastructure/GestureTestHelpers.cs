// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.UI.Input.Preview.Injection;

namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

/// <summary>
/// A single injected pointer (finger, pen or mouse) that can be pressed, moved and released independently.
/// </summary>
/// <remarks>
/// Uno equivalent of the Avalonia <c>GestureRecognizerTestHelper</c>: every call injects real input through the
/// Uno <see cref="InputInjector"/>, so several instances can be combined into multi touch gestures.
/// Coordinates are in the coordinate space of the element passed to each call.
/// </remarks>
public sealed class TestPointer
{
    private static uint s_nextId = 1000;

    /// <summary>
    /// Creates a new pointer with a unique id.
    /// </summary>
    public TestPointer(PointerDeviceType pointerDeviceType = PointerDeviceType.Touch)
    {
        Id = s_nextId++;
        PointerDeviceType = pointerDeviceType;
    }

    /// <summary>
    /// Gets the injected pointer id (unique per instance).
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the injected pointer device type.
    /// </summary>
    public PointerDeviceType PointerDeviceType { get; }

    /// <summary>
    /// Gets a value indicating whether the pointer is currently pressed (in contact).
    /// </summary>
    public bool IsPressed { get; private set; }

    /// <summary>
    /// Gets the last injected window position.
    /// </summary>
    public Point WindowPosition { get; private set; }

    private static InputInjector Injector => InputInjectorHelper.Current.Injector;

    /// <summary>
    /// Presses the pointer at a position of <paramref name="element"/>.
    /// </summary>
    public void Down(UIElement element, Point point)
    {
        WindowPosition = ZoomBorderTestHelper.ToWindow(element, point);
        IsPressed = true;

        switch (PointerDeviceType)
        {
            case PointerDeviceType.Mouse:
                InputHelper.MouseMoveTo(WindowPosition);
                InputHelper.MouseDown(ButtonName.Left);
                break;
            default:
                Inject(InjectedInputPointerOptions.New | InjectedInputPointerOptions.PointerDown | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.FirstButton);
                break;
        }
    }

    /// <summary>
    /// Moves the (pressed) pointer to a position of <paramref name="element"/>.
    /// </summary>
    public void Move(UIElement element, Point point)
    {
        WindowPosition = ZoomBorderTestHelper.ToWindow(element, point);

        switch (PointerDeviceType)
        {
            case PointerDeviceType.Mouse:
                InputHelper.MouseMoveTo(WindowPosition);
                break;
            default:
                Inject(IsPressed
                    ? InjectedInputPointerOptions.Update | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.FirstButton
                    : InjectedInputPointerOptions.Update | InjectedInputPointerOptions.InRange);
                break;
        }
    }

    /// <summary>
    /// Releases the pointer, optionally at a new position of <paramref name="element"/>.
    /// </summary>
    public void Up(UIElement element, Point? point = null)
    {
        if (point is { } p)
        {
            WindowPosition = ZoomBorderTestHelper.ToWindow(element, p);
        }

        IsPressed = false;

        switch (PointerDeviceType)
        {
            case PointerDeviceType.Mouse:
                InputHelper.MouseMoveTo(WindowPosition);
                InputHelper.MouseUp(ButtonName.Left);
                break;
            default:
                Inject(InjectedInputPointerOptions.PointerUp | InjectedInputPointerOptions.FirstButton);
                break;
        }
    }

    /// <summary>
    /// Cancels the pointer (the platform cancels the contact, e.g. capture lost).
    /// </summary>
    public void Cancel()
    {
        IsPressed = false;

        switch (PointerDeviceType)
        {
            case PointerDeviceType.Mouse:
                InputHelper.MouseUp(ButtonName.Left);
                break;
            default:
                Inject(InjectedInputPointerOptions.Canceled | InjectedInputPointerOptions.PointerUp);
                break;
        }
    }

    /// <summary>
    /// Presses and releases the pointer at a position of <paramref name="element"/>.
    /// </summary>
    public void Tap(UIElement element, Point point)
    {
        Down(element, point);
        Up(element);
    }

    /// <summary>
    /// Presses the pointer, moves it linearly in <paramref name="steps"/> steps and releases it.
    /// </summary>
    public void Drag(UIElement element, Point from, Point to, int steps = 10)
    {
        Down(element, from);
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (double)steps;
            Move(element, new Point(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t));
        }

        Up(element);
    }

    private void Inject(InjectedInputPointerOptions options)
    {
        var pointerInfo = new InjectedInputPointerInfo
        {
            PointerId = Id,
            PixelLocation = new InjectedInputPoint
            {
                PositionX = (int)Math.Round(WindowPosition.X),
                PositionY = (int)Math.Round(WindowPosition.Y)
            },
            PointerOptions = options,
            TimeOffsetInMilliseconds = 1
        };

        if (PointerDeviceType == PointerDeviceType.Pen)
        {
            Injector.InjectPenInput(new InjectedInputPenInfo
            {
                PointerInfo = pointerInfo,
                PenParameters = InjectedInputPenParameters.Pressure,
                Pressure = 0.5
            });
        }
        else
        {
            Injector.InjectTouchInput(new[]
            {
                new InjectedInputTouchInfo
                {
                    PointerInfo = pointerInfo,
                    Contact = new InjectedInputRectangle { Left = 2, Top = 2, Right = 2, Bottom = 2 },
                    Pressure = 1.0
                }
            });
        }
    }
}

/// <summary>
/// Multi touch gesture helpers (Uno equivalent of the Avalonia <c>MultiTouchTestHelperFactory</c>) and
/// helpers that drive the <see cref="ZoomBorder"/> manipulation handlers directly (Uno equivalent of
/// raising Avalonia <c>PinchEventArgs</c>/<c>ScrollGestureEventArgs</c> directly).
/// </summary>
public static class GestureTestHelpers
{
    /// <summary>
    /// Creates two distinct touch pointers.
    /// </summary>
    public static (TestPointer First, TestPointer Second) CreatePair() => (new TestPointer(), new TestPointer());

    /// <summary>
    /// Creates <paramref name="count"/> distinct touch pointers.
    /// </summary>
    public static TestPointer[] Create(int count) => Enumerable.Range(0, count).Select(_ => new TestPointer()).ToArray();

    /// <summary>
    /// Resets the touch and pen injection state (cancels any contact left pressed by a test).
    /// </summary>
    public static void ResetTouch()
    {
        var injector = InputInjectorHelper.Current.Injector;
        injector.UninitializeTouchInjection();
        injector.UninitializePenInjection();
    }

    /// <summary>
    /// Simulates a two finger gesture with real injected touch input. Positions are in <paramref name="element"/> coordinates.
    /// </summary>
    public static void SimulatePinch(UIElement element, Point start1, Point start2, Point end1, Point end2, int steps = 10)
    {
        var (first, second) = CreatePair();
        first.Down(element, start1);
        second.Down(element, start2);

        for (var i = 1; i <= steps; i++)
        {
            var t = i / (double)steps;
            first.Move(element, Lerp(start1, end1, t));
            second.Move(element, Lerp(start2, end2, t));
        }

        second.Up(element);
        first.Up(element);
    }

    /// <summary>
    /// Simulates a horizontal pinch around <paramref name="center"/> with real injected touch input.
    /// </summary>
    public static void SimulatePinchZoom(UIElement element, Point center, double startDistance, double endDistance, int steps = 10)
    {
        SimulatePinch(
            element,
            new Point(center.X - startDistance / 2, center.Y),
            new Point(center.X + startDistance / 2, center.Y),
            new Point(center.X - endDistance / 2, center.Y),
            new Point(center.X + endDistance / 2, center.Y),
            steps);
    }

    /// <summary>
    /// Simulates two fingers moving together (constant spacing) from <paramref name="startCenter"/> to <paramref name="endCenter"/>.
    /// </summary>
    public static void SimulateTwoFingerPan(UIElement element, Point startCenter, Point endCenter, double fingerSpacing = 50, int steps = 10)
    {
        var half = fingerSpacing / 2;
        SimulatePinch(
            element,
            new Point(startCenter.X - half, startCenter.Y),
            new Point(startCenter.X + half, startCenter.Y),
            new Point(endCenter.X - half, endCenter.Y),
            new Point(endCenter.X + half, endCenter.Y),
            steps);
    }

    /// <summary>
    /// Simulates two fingers rotating around <paramref name="center"/> (opposite points of a circle).
    /// </summary>
    public static void SimulateRotation(UIElement element, Point center, double radius, double startAngleDegrees, double endAngleDegrees, int steps = 10)
    {
        var (first, second) = CreatePair();
        first.Down(element, OnCircle(center, radius, startAngleDegrees));
        second.Down(element, OnCircle(center, radius, startAngleDegrees + 180));

        for (var i = 1; i <= steps; i++)
        {
            var angle = startAngleDegrees + (endAngleDegrees - startAngleDegrees) * i / steps;
            first.Move(element, OnCircle(center, radius, angle));
            second.Move(element, OnCircle(center, radius, angle + 180));
        }

        second.Up(element);
        first.Up(element);
    }

    /// <summary>
    /// Drives a single pinch update through the manipulation handler of the control
    /// (Uno equivalent of raising an Avalonia <c>PinchEventArgs</c> directly).
    /// </summary>
    /// <param name="zoomBorder">The control.</param>
    /// <param name="cumulativeScale">The cumulative scale since the manipulation started.</param>
    /// <param name="origin">The pinch origin in control coordinates.</param>
    /// <param name="incrementalScale">The incremental scale (defaults to <paramref name="cumulativeScale"/> for the first update).</param>
    /// <param name="rotation">The incremental rotation in degrees.</param>
    public static void PinchDelta(ZoomBorder zoomBorder, double cumulativeScale, Point origin, double? incrementalScale = null, double rotation = 0)
    {
        zoomBorder.HandleManipulationDelta(origin, new Point(0, 0), incrementalScale ?? cumulativeScale, rotation, cumulativeScale);
    }

    /// <summary>
    /// Starts a manipulation and drives a single pinch update (see <see cref="PinchDelta"/>).
    /// </summary>
    public static void Pinch(ZoomBorder zoomBorder, double scale, Point origin, double rotation = 0)
    {
        zoomBorder.HandleManipulationStarted();
        PinchDelta(zoomBorder, scale, origin, scale, rotation);
    }

    /// <summary>
    /// Drives a single scroll update through the manipulation handler of the control
    /// (Uno equivalent of raising an Avalonia <c>ScrollGestureEventArgs</c> directly).
    /// </summary>
    /// <param name="zoomBorder">The control.</param>
    /// <param name="scrollDelta">The scroll delta (Avalonia scroll gesture semantics, opposite to the finger movement).</param>
    public static void ScrollDelta(ZoomBorder zoomBorder, Point scrollDelta)
    {
        zoomBorder.HandleManipulationDelta(new Point(0, 0), new Point(-scrollDelta.X, -scrollDelta.Y), 1.0, 0.0, 1.0);
    }

    private static Point OnCircle(Point center, double radius, double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180.0;
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }

    private static Point Lerp(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}

/// <summary>
/// Records the gestures processed by a <see cref="ZoomBorder"/> (GestureStarted/GestureEnded) and the raw
/// manipulation events it receives.
/// </summary>
public sealed class GestureRecorder
{
    /// <summary>
    /// Attaches a recorder to the control.
    /// </summary>
    public GestureRecorder(ZoomBorder zoomBorder)
    {
        zoomBorder.GestureStarted += (_, e) => Started.Add(e);
        zoomBorder.GestureEnded += (_, e) => Ended.Add(e);
        zoomBorder.AddHandler(UIElement.ManipulationDeltaEvent, new ManipulationDeltaEventHandler((_, e) => ManipulationDeltas.Add((e.Cumulative.Scale, e.Cumulative.Rotation, e.Cumulative.Translation))), true);
        zoomBorder.AddHandler(UIElement.ManipulationCompletedEvent, new ManipulationCompletedEventHandler((_, _) => ManipulationCompletedCount++), true);
    }

    /// <summary>Gets the GestureStarted events (raised for every processed pinch or scroll update).</summary>
    public List<GestureEventArgs> Started { get; } = new();

    /// <summary>Gets the GestureEnded events.</summary>
    public List<GestureEventArgs> Ended { get; } = new();

    /// <summary>Gets the cumulative values of the received manipulation deltas.</summary>
    public List<(double Scale, double Rotation, Point Translation)> ManipulationDeltas { get; } = new();

    /// <summary>Gets the number of completed manipulations.</summary>
    public int ManipulationCompletedCount { get; private set; }

    /// <summary>Gets the processed pinch updates (Uno equivalent of Avalonia <c>PinchEvent</c>).</summary>
    public IReadOnlyList<GestureEventArgs> Pinches => Started.Where(e => e.GestureType == "Pinch").ToList();

    /// <summary>Gets the processed scroll updates (Uno equivalent of Avalonia <c>ScrollGestureEvent</c>).</summary>
    public IReadOnlyList<GestureEventArgs> Scrolls => Started.Where(e => e.GestureType == "Scroll").ToList();

    /// <summary>Gets the number of ended pinches (Uno equivalent of Avalonia <c>PinchEndedEvent</c>).</summary>
    public int PinchEndedCount => Ended.Count(e => e.GestureType == "Pinch");

    /// <summary>Gets the number of ended scrolls (Uno equivalent of Avalonia <c>ScrollGestureEndedEvent</c>).</summary>
    public int ScrollEndedCount => Ended.Count(e => e.GestureType == "Scroll");

    /// <summary>Gets the cumulative scale of the last processed pinch update (Avalonia <c>PinchEventArgs.Scale</c>).</summary>
    public double LastPinchScale => Pinches[^1].Delta + 1.0;
}
