// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using static PanAndZoom.Uno.RuntimeTests.Infrastructure.ScenarioTestHelpers;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Port of the Avalonia <c>GestureSimulatorSamples</c>.
/// </summary>
/// <remarks>
/// The Avalonia samples raise simulated Avalonia gesture events (Tapped, DoubleTapped, Pinch, ScrollGesture,
/// PullGesture, touchpad magnify/swipe, Holding) through the HeadlessTestingFramework <c>GestureSimulator</c>.
/// On Uno the same interactions are performed with real injected touch/mouse input and verified through the
/// equivalent WinUI events (Tapped, DoubleTapped, RightTapped, Holding, Manipulation*, PointerWheelChanged)
/// and the resulting <see cref="ZoomBorder"/> state.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class GestureSimulatorSamples
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    #region Basic Tap Gestures

    /// <summary>
    /// Sample: Basic tap gesture to select an element
    /// </summary>
    [TestMethod]
    public async Task Sample_TapToSelect()
    {
        var button = new Button { Content = "Click Me", Width = 100, Height = 50, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        await ZoomBorderTestHelper.LoadAsync(button);

        var wasTapped = false;
        var wasClicked = false;
        button.AddHandler(UIElement.TappedEvent, new TappedEventHandler((_, _) => wasTapped = true), true);
        button.Click += (s, e) => wasClicked = true;

        // Act - Tap the button (real touch tap)
        TouchTap(Window(button, 50, 25));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // On WinUI a touch tap on a Button raises both Tapped and Click.
        Assert.IsTrue(wasTapped);
        Assert.IsTrue(wasClicked);
    }

    /// <summary>
    /// Sample: Double-tap to zoom in on a ZoomBorder
    /// </summary>
    [TestMethod]
    public async Task Sample_DoubleTapToZoom()
    {
        var content = new Border
        {
            Width = 200,
            Height = 200,
            Background = new SolidColorBrush(Colors.LightBlue)
        };
        var zoomBorder = CreateZoomBorder(content);

        var doubleTapCount = 0;
        zoomBorder.AddHandler(UIElement.DoubleTappedEvent, new DoubleTappedEventHandler((s, e) => doubleTapCount++), true);

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        var initialZoom = zoomBorder.ZoomX;

        // Act - Double tap at center
        TouchDoubleTap(Window(zoomBorder, 200, 150));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(1, doubleTapCount);

        // Default double click zoom mode (ZoomInOut): the auto fit zoom (1.5) reaches the 1.5 threshold so it resets.
        Assert.AreEqual(1.5, initialZoom, 1e-9);
        Assert.AreEqual(1.0, zoomBorder.ZoomX, 1e-9);
    }

    /// <summary>
    /// Sample: Right-tap to open context menu
    /// </summary>
    [TestMethod]
    public async Task Sample_RightTapForContextMenu()
    {
        var target = new Border
        {
            Width = 200,
            Height = 200,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Colors.LightGreen)
        };

        var rightTapPosition = default(Point?);
        target.RightTapped += (s, e) => rightTapPosition = e.GetPosition(target);

        await ZoomBorderTestHelper.LoadAsync(target);

        // Act - Right tap (right mouse button click) to trigger context menu
        InputHelper.MouseClick(Window(target, 100, 100), ButtonName.Right);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNotNull(rightTapPosition);
        Assert.AreEqual(100.0, rightTapPosition.Value.X, 1.0);
        Assert.AreEqual(100.0, rightTapPosition.Value.Y, 1.0);
    }

    #endregion

    #region Pinch-to-Zoom Samples

    /// <summary>
    /// Sample: Pinch gesture to zoom in
    /// </summary>
    [TestMethod]
    public async Task Sample_PinchToZoomIn()
    {
        var zoomBorder = CreateZoomBorder(new Border { Background = new SolidColorBrush(Colors.Coral) });

        // Avalonia PinchEvent scale -> WinUI manipulation cumulative scale.
        var scales = TrackManipulationScales(zoomBorder);

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        var initialZoom = zoomBorder.ZoomX;

        // Act - Pinch zoom from 1x to 2x (spread fingers apart)
        await PinchAsync(Window(zoomBorder, 200, 150), 100, 200, steps: 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(scales.Count > 0);
        Assert.IsTrue(scales[^1] >= 1.9, $"Final scale {scales[^1]}"); // Final scale near 2.0
        Assert.AreEqual(initialZoom * 2.0, zoomBorder.ZoomX, 0.05);
    }

    /// <summary>
    /// Sample: Pinch gesture to zoom out
    /// </summary>
    [TestMethod]
    public async Task Sample_PinchToZoomOut()
    {
        var zoomBorder = CreateZoomBorder(new Border { Background = new SolidColorBrush(Colors.Coral) });

        var scales = TrackManipulationScales(zoomBorder);

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        var initialZoom = zoomBorder.ZoomX;

        // Act - Pinch zoom from 2x to 1x (pinch fingers together)
        await PinchAsync(Window(zoomBorder, 200, 150), 200, 100, steps: 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(scales.Count > 0);
        Assert.IsTrue(scales[^1] <= 1.1, $"Final scale {scales[^1]}");
        Assert.AreEqual(initialZoom * 0.5, zoomBorder.ZoomX, 0.05);
    }

    /// <summary>
    /// Sample: Rotation gesture with pinch
    /// </summary>
    [TestMethod]
    public async Task Sample_PinchToRotate()
    {
        var target = new Border
        {
            Width = 200,
            Height = 200,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Colors.Purple),
            ManipulationMode = ManipulationModes.Rotate | ManipulationModes.Scale
        };

        // Avalonia PinchEvent (with angle) -> WinUI manipulation deltas with rotation.
        var pinchCount = 0;
        var rotation = 0.0;
        target.ManipulationDelta += (s, e) =>
        {
            pinchCount++;
            rotation = e.Cumulative.Rotation;
        };

        await ZoomBorderTestHelper.LoadAsync(target);

        // Act - Rotate 90 degrees
        await RotateAsync(Window(target, 100, 100), 50, 0, 90, steps: 10);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(pinchCount > 0);
        Assert.AreEqual(90.0, Math.Abs(rotation), 5.0);
    }

    #endregion

    #region Scroll Gesture Samples

    /// <summary>
    /// Sample: Scroll gesture in a ScrollViewer
    /// </summary>
    [TestMethod]
    public async Task Sample_ScrollGesture()
    {
        var content = new StackPanel();
        for (var i = 0; i < 50; i++)
        {
            content.Children.Add(new TextBlock { Text = $"Item {i}", Height = 20 });
        }

        var scrollViewer = new ScrollViewer
        {
            Content = content,
            Width = 300,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        await ZoomBorderTestHelper.LoadAsync(scrollViewer);

        // Act - Scroll down 200 pixels (Avalonia ScrollSequence -> a touch pan moving the finger up).
        await TouchDragAsync(Window(scrollViewer, 150, 250), Window(scrollViewer, 150, 50), steps: 10);
        await ZoomBorderTestHelper.WaitForAsync(() => scrollViewer.VerticalOffset > 100);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(Math.Abs(scrollViewer.VerticalOffset) > 100, $"VerticalOffset={scrollViewer.VerticalOffset}");
    }

    /// <summary>
    /// Sample: Flick gesture with inertia
    /// </summary>
    [TestMethod]
    public async Task Sample_FlickGesture()
    {
        var target = new Border
        {
            Width = 200,
            Height = 200,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Colors.Orange),
            ManipulationMode = ManipulationModes.TranslateY | ManipulationModes.TranslateInertia
        };

        // Avalonia ScrollGesture events -> WinUI manipulation deltas.
        var scrollEventCount = 0;
        var translationY = 0.0;
        target.ManipulationDelta += (s, e) =>
        {
            scrollEventCount++;
            translationY = e.Cumulative.Translation.Y;
        };

        await ZoomBorderTestHelper.LoadAsync(target);

        // Act - Flick upward (50 pixels, fast)
        await TouchDragAsync(Window(target, 100, 150), Window(target, 100, 100), steps: 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(scrollEventCount > 0);
        Assert.IsTrue(translationY < 0, $"TranslationY={translationY}");
    }

    #endregion

    #region Pull-to-Refresh Samples

    /// <summary>
    /// Sample: Pull-to-refresh gesture
    /// </summary>
    [TestMethod]
    public async Task Sample_PullToRefresh()
    {
        var refreshList = new Border
        {
            Width = 300,
            Height = 400,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Colors.LightCyan),
            ManipulationMode = ManipulationModes.TranslateY
        };

        // Avalonia PullGesture / PullGestureEnded -> WinUI ManipulationDelta / ManipulationCompleted.
        var pullDistance = 0.0;
        var pullEnded = false;
        refreshList.ManipulationDelta += (s, e) => pullDistance += e.Delta.Translation.Y;
        refreshList.ManipulationCompleted += (s, e) => pullEnded = true; // In real app: trigger data refresh here

        await ZoomBorderTestHelper.LoadAsync(refreshList);

        // Act - Pull down to refresh (TopToBottom, 100 pixels)
        await TouchDragAsync(Window(refreshList, 150, 20), Window(refreshList, 150, 120), steps: 10);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(pullDistance > 0);
        Assert.IsTrue(pullEnded);
    }

    #endregion

    #region Touchpad Gesture Samples (macOS)

    /// <summary>
    /// Sample: Touchpad magnify gesture (trackpad pinch)
    /// </summary>
    [TestMethod]
    public async Task Sample_TouchpadMagnify()
    {
        // Touchpad magnify cannot be injected on Uno: a precision touchpad pinch is delivered by WinUI as a
        // Ctrl + wheel, which the ZoomBorder handles as a zoom (WheelWithCtrl = Zoom).
        var zoomBorder = CreateZoomBorder(new Border { Width = 200, Height = 200, Background = new SolidColorBrush(Colors.Teal) });
        zoomBorder.Stretch = StretchMode.None;

        var magnifyDelta = 0.0;
        zoomBorder.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler((s, e) =>
        {
            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
            {
                magnifyDelta += e.GetCurrentPoint(zoomBorder).Properties.MouseWheelDelta;
            }
        }), true);

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Magnify gesture (zoom in) in 5 steps
        for (var i = 0; i < 5; i++)
        {
            InputHelper.MouseWheel(Window(zoomBorder, 100, 100), 120, VirtualKeyModifiers.Control);
        }

        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(Math.Abs(magnifyDelta) > 0);
        Assert.AreEqual(Math.Pow(zoomBorder.ZoomSpeed, 5), zoomBorder.ZoomX, 1e-6);
    }

    /// <summary>
    /// Sample: Touchpad swipe for navigation
    /// </summary>
    [TestMethod]
    public async Task Sample_TouchpadSwipe()
    {
        var navigationHost = new Border
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Colors.SlateGray)
        };

        // Touchpad swipe cannot be injected on Uno: a two finger touchpad swipe is delivered as horizontal wheel input.
        var swipeCount = 0;
        navigationHost.PointerWheelChanged += (s, e) =>
        {
            if (e.GetCurrentPoint(navigationHost).Properties.IsHorizontalMouseWheel)
            {
                swipeCount++; // In real app: navigate between pages
            }
        };

        await ZoomBorderTestHelper.LoadAsync(navigationHost);

        // Act - Swipe left (5 steps)
        for (var i = 0; i < 5; i++)
        {
            InputHelper.MouseWheel(Window(navigationHost, 200, 150), -20, horizontal: true);
        }

        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(swipeCount > 0);
    }

    #endregion

    #region Compound Gesture Samples

    /// <summary>
    /// Sample: Double-tap zoom (common mobile pattern)
    /// </summary>
    [TestMethod]
    public async Task Sample_DoubleTapZoomPattern()
    {
        var zoomBorder = CreateZoomBorder(new Border { Background = new SolidColorBrush(Colors.MediumPurple) });
        zoomBorder.Stretch = StretchMode.None;

        var doubleTapRaised = false;
        zoomBorder.AddHandler(UIElement.DoubleTappedEvent, new DoubleTappedEventHandler((s, e) => doubleTapRaised = true), true);
        var pinchEvents = TrackManipulationScales(zoomBorder);

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Double-tap to zoom, then the animated zoom (Avalonia DoubleTapZoom = DoubleTap + pinch 1x -> 2x).
        TouchDoubleTap(Window(zoomBorder, 200, 150));
        await ZoomBorderTestHelper.WaitForIdleAsync();
        var zoomAfterDoubleTap = zoomBorder.ZoomX;

        await PinchAsync(Window(zoomBorder, 200, 150), 100, 200, steps: 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(doubleTapRaised);
        Assert.IsTrue(pinchEvents.Count > 0);
        Assert.AreEqual(2.0, zoomAfterDoubleTap, 1e-9);
        Assert.AreEqual(4.0, zoomBorder.ZoomX, 0.1);
    }

    /// <summary>
    /// Sample: Press and hold for context actions
    /// </summary>
    [TestMethod]
    public async Task Sample_PressAndHold()
    {
        var target = new Border
        {
            Width = 100,
            Height = 100,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Colors.DarkOrange)
        };

        var holdingStates = new List<HoldingState>();
        target.Holding += (s, e) => holdingStates.Add(e.HoldingState);

        await ZoomBorderTestHelper.LoadAsync(target);

        // Act - Press and hold (WinUI raises Holding after its own hold delay, so hold longer than 500ms).
        await TouchHoldAsync(Window(target, 50, 50), 1200);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(2, holdingStates.Count, string.Join(", ", holdingStates));
        Assert.AreEqual(HoldingState.Started, holdingStates[0]);
        Assert.AreEqual(HoldingState.Completed, holdingStates[1]);
    }

    #endregion

    #region Integration with ZoomBorder

    /// <summary>
    /// Sample: Complete pan and zoom gesture workflow with event monitoring
    /// </summary>
    [TestMethod]
    public async Task Sample_CompleteZoomBorderWorkflow()
    {
        // The Avalonia sample uses a plain Border raising simulated gesture events; on Uno the gestures are real
        // touch input, so they are observed on a ZoomBorder (gesture events) to verify the full workflow.
        var control = CreateZoomBorder(new Border { Width = 400, Height = 300, Background = new SolidColorBrush(Colors.DeepSkyBlue) });
        control.Stretch = StretchMode.None;

        var eventsRaised = new List<string>();
        control.GestureStarted += (s, e) => eventsRaised.Add(e.GestureType);
        control.GestureEnded += (s, e) => eventsRaised.Add(e.GestureType + "Ended");
        control.AddHandler(UIElement.DoubleTappedEvent, new DoubleTappedEventHandler((s, e) => eventsRaised.Add("DoubleTap")), true);

        await ZoomBorderTestHelper.LoadAsync(control);
        var center = Window(control, 200, 150);

        // Step 1: Zoom in with pinch gesture
        await PinchAsync(center, 100, 150, steps: 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.AreEqual(1.5, control.ZoomX, 0.05);

        // Step 2: Double-tap to toggle zoom level (1.5 reaches the ZoomInOut threshold -> reset)
        TouchDoubleTap(center);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.AreEqual(1.0, control.ZoomX, 1e-9);

        // Step 3: Scroll/pan the content (scroll delta (50, 50) -> finger moves up-left by 50)
        await TouchDragAsync(center, new Point(center.X - 50, center.Y - 50), steps: 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.AreEqual(-50.0, control.OffsetX, ZoomBorderTestHelper.TouchPanTolerance());
        Assert.AreEqual(-50.0, control.OffsetY, ZoomBorderTestHelper.TouchPanTolerance());

        // Step 4: Zoom out with pinch
        await PinchAsync(center, 150, 120, steps: 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.AreEqual(0.8, control.ZoomX, 0.05);

        // Verify all gestures were raised in sequence
        CollectionAssert.Contains(eventsRaised, "Pinch");
        CollectionAssert.Contains(eventsRaised, "PinchEnded");
        CollectionAssert.Contains(eventsRaised, "DoubleTap");
        CollectionAssert.Contains(eventsRaised, "Scroll");
        CollectionAssert.Contains(eventsRaised, "ScrollEnded");

        // Verify event order (pinch before double-tap before scroll)
        var firstPinchIndex = eventsRaised.IndexOf("Pinch");
        var doubleTapIndex = eventsRaised.IndexOf("DoubleTap");
        var firstScrollIndex = eventsRaised.IndexOf("Scroll", doubleTapIndex);

        Assert.IsTrue(firstPinchIndex < doubleTapIndex, "Pinch should occur before DoubleTap");
        Assert.IsTrue(doubleTapIndex < firstScrollIndex, "DoubleTap should occur before Scroll");
    }

    #endregion

    #region Helpers

    private static ZoomBorder CreateZoomBorder(UIElement child)
    {
        return new ZoomBorder
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = child
        };
    }

    private static List<double> TrackManipulationScales(UIElement element)
    {
        var scales = new List<double>();
        element.AddHandler(UIElement.ManipulationDeltaEvent, new ManipulationDeltaEventHandler((s, e) =>
        {
            if (e.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch && Math.Abs(e.Cumulative.Scale - 1.0) > 1e-9)
            {
                scales.Add(e.Cumulative.Scale);
            }
        }), true);
        return scales;
    }

    private static Point Window(FrameworkElement element, double x, double y) => ZoomBorderTestHelper.ToWindow(element, new Point(x, y));

    #endregion
}
