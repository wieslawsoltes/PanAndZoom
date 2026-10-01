// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.IO;
using Microsoft.UI;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using static PanAndZoom.Uno.RuntimeTests.Infrastructure.ScenarioTestHelpers;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Port of the Avalonia <c>ZoomBorderRecordingTests</c>.
/// </summary>
/// <remarks>
/// The Avalonia tests drive the HeadlessTestingFramework <c>RecordedTouchSimulator</c> (simulated input, recording
/// session with markers and events, PNG/video output). On Uno the same interaction sequences are performed with real
/// injected touch/mouse input, frames are runtime test engine screenshots (<see cref="FrameRecorder"/>), recording
/// events/markers are replaced by the <see cref="ZoomBorder"/> events and state captured around each step, and the
/// assertions verify the resulting <see cref="ZoomBorder"/> state and that captured frames differ visually.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderRecordingTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public async Task Recording_ZoomBorderPinchZoom_CapturesZoomAnimation()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var gestures = TrackGestures(zoomBorder);
        var recorder = new FrameRecorder(zoomBorder);
        await recorder.CaptureAsync();

        var centerPoint = new Point(zoomBorder.ActualWidth / 2, zoomBorder.ActualHeight / 2);
        var zoomStart = zoomBorder.ZoomX; // Marker "ZoomStart"
        await PinchAsync(ToWindow(zoomBorder, centerPoint), 50, 150, steps: 10, onStep: recorder.CaptureAsync);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        var zoomEnd = zoomBorder.ZoomX; // Marker "ZoomEnd"
        await recorder.CaptureAsync();

        // Frames were captured and they show the zoom.
        Assert.IsTrue(recorder.Frames.Count > 0, $"pinch_zoom: Expected frames > 0, got {recorder.Frames.Count}");
        Assert.IsTrue(CountDifferentPixels(recorder.Frames[0], recorder.Frames[^1]) > 0, "The first and last frames should differ.");

        // Gesture events were raised and the zoom increased between the markers.
        Assert.IsTrue(gestures.Started.Contains("Pinch"));
        Assert.IsTrue(gestures.Ended.Contains("Pinch"));
        Assert.IsTrue(zoomEnd > zoomStart, $"ZoomStart={zoomStart} ZoomEnd={zoomEnd}");
        Assert.AreEqual(zoomStart * 3.0, zoomEnd, 0.1);
    }

    [TestMethod]
    public async Task Recording_ZoomBorderPan_CapturesPanMovement()
    {
        var zoomBorder = await CreateAndLoadAsync();

        zoomBorder.ZoomTo(2.0, 200, 150, skipTransitions: true);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        var panEvents = 0;
        zoomBorder.PanStarted += (_, _) => panEvents++;
        zoomBorder.PanEnded += (_, _) => panEvents++;
        var recorder = new FrameRecorder(zoomBorder);
        await recorder.CaptureAsync();

        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // RecordedDrag uses the left mouse button (PanButton = Left).
        InputHelper.MouseDrag(ToWindow(zoomBorder, new Point(200, 150)), ToWindow(zoomBorder, new Point(100, 100)), ButtonName.Left, steps: 10);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        await recorder.CaptureAsync();

        // Input events reached the control (pan started/ended) and the pan was applied.
        Assert.AreEqual(2, panEvents);
        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetX - 100, zoomBorder.OffsetX, 1.0);
        Assert.AreEqual(initialOffsetY - 50, zoomBorder.OffsetY, 1.0);
        Assert.IsTrue(CountDifferentPixels(recorder.Frames[0], recorder.Frames[^1]) > 0);
    }

    [TestMethod]
    public async Task Recording_ZoomBorderTwoFingerPan_CapturesGesture()
    {
        var zoomBorder = await CreateAndLoadAsync();

        zoomBorder.ZoomTo(1.5, 200, 150, skipTransitions: true);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        var gestures = TrackGestures(zoomBorder);
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // The injected fingers move one after the other (sequential pointer moves), which produces a transient
        // rotation/scale between them: use widely spaced fingers and small steps (RecordedTwoFingerPan: 8 steps)
        // so that the transient rotation does not skew the pan.
        await TwoFingerPanAsync(ToWindow(zoomBorder, new Point(200, 150)), ToWindow(zoomBorder, new Point(100, 100)), fingerSpacing: 160, steps: 32);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(gestures.Started.Count > 0);
        Assert.AreEqual(initialOffsetX - 100, zoomBorder.OffsetX, 2.0);
        Assert.AreEqual(initialOffsetY - 50, zoomBorder.OffsetY, 2.0);
    }

    [TestMethod]
    public async Task Recording_ZoomBorderScrollGesture_CapturesScroll()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var gestures = TrackGestures(zoomBorder);
        var recorder = new FrameRecorder(zoomBorder);
        var initialOffsetY = zoomBorder.OffsetY;

        // RecordedScroll(new Vector(0, -20)) is a scroll gesture (scroll direction semantics): the equivalent
        // touch manipulation moves the finger 20 pixels down.
        var center = ToWindow(zoomBorder, new Point(200, 150));
        for (var i = 0; i < 5; i++)
        {
            await TouchDragAsync(center, new Point(center.X, center.Y + 20), steps: 4);
            await ZoomBorderTestHelper.WaitForIdleAsync();
            await recorder.CaptureAsync();
        }

        Assert.AreEqual(5, recorder.Frames.Count);
        Assert.AreEqual(5, gestures.Ended.Count(g => g == "Scroll"));
        Assert.AreEqual(initialOffsetY + 100, zoomBorder.OffsetY, 2.0);
    }

    [TestMethod]
    public async Task Recording_ZoomBorderDoubleTapZoom_CapturesDoubleTap()
    {
        var zoomBorder = await CreateAndLoadAsync(zb => zb.EnableDoubleClickZoom = true);
        var recorder = new FrameRecorder(zoomBorder);

        var doubleTapped = 0;
        zoomBorder.AddHandler(UIElement.DoubleTappedEvent, new DoubleTappedEventHandler((_, _) => doubleTapped++), true);

        await recorder.CaptureAsync();
        var zoomStart = zoomBorder.ZoomX; // Marker "DoubleTapStart"
        TouchDoubleTap(ToWindow(zoomBorder, new Point(200, 150)));
        await ZoomBorderTestHelper.WaitForIdleAsync();
        var zoomEnd = zoomBorder.ZoomX; // Marker "DoubleTapEnd"
        await recorder.CaptureAsync();

        Assert.IsTrue(recorder.Frames.Count > 0, $"double_tap: Expected frames > 0, got {recorder.Frames.Count}");
        Assert.AreEqual(1, doubleTapped);

        // Default double click zoom mode (ZoomInOut): the auto fit zoom (4/3) is below the 1.5 threshold so it zooms in by 2.
        Assert.AreEqual(zoomStart * 2.0, zoomEnd, 1e-6);
        Assert.IsTrue(CountDifferentPixels(recorder.Frames[0], recorder.Frames[^1]) > 0);
    }

    [TestMethod]
    public async Task Recording_ZoomBorderRotation_CapturesRotationGesture()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var gestures = TrackGestures(zoomBorder);
        var recorder = new FrameRecorder(zoomBorder);

        var initialRotation = zoomBorder.Rotation;
        Assert.AreEqual(0.0, initialRotation);

        await recorder.CaptureAsync();
        await RotateAsync(ToWindow(zoomBorder, new Point(200, 150)), 60, 0, 45, steps: 10, onStep: recorder.CaptureAsync);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(gestures.Started.Contains("Pinch"));
        Assert.AreNotEqual(0.0, zoomBorder.Rotation);
        Assert.AreEqual(45.0, Math.Abs(zoomBorder.Rotation), 3.0);
        Assert.IsTrue(CountDifferentPixels(recorder.Frames[0], recorder.Frames[^1]) > 0);
    }

    [TestMethod]
    public async Task Recording_ZoomBorderSwipeGestures_CapturesAllDirections()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var gestures = TrackGestures(zoomBorder);
        var recorder = new FrameRecorder(zoomBorder);

        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        var center = ToWindow(zoomBorder, new Point(200, 150));
        var offsets = new List<Point>();

        // SwipeRight, SwipeLeft, SwipeUp, SwipeDown (80 pixels each, from the center).
        foreach (var (dx, dy) in new[] { (80, 0), (-80, 0), (0, -80), (0, 80) })
        {
            await TouchDragAsync(center, new Point(center.X + dx, center.Y + dy), steps: 8);
            await ZoomBorderTestHelper.WaitForIdleAsync();
            await recorder.CaptureAsync();
            offsets.Add(new Point(zoomBorder.OffsetX, zoomBorder.OffsetY));
        }

        Assert.AreEqual(4, recorder.Frames.Count);
        Assert.IsTrue(gestures.Ended.Count >= 4, "Should have at least 4 gesture events");

        Assert.AreEqual(initialOffsetX + 80, offsets[0].X, 1.0);
        Assert.AreEqual(initialOffsetX, offsets[1].X, 1.0);
        Assert.AreEqual(initialOffsetY - 80, offsets[2].Y, 1.0);
        Assert.AreEqual(initialOffsetY, offsets[3].Y, 1.0);
    }

    [TestMethod]
    public async Task Recording_ZoomBorderComplexInteraction_CapturesFullWorkflow()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var gestures = TrackGestures(zoomBorder);
        var recorder = new FrameRecorder(zoomBorder);
        var center = ToWindow(zoomBorder, new Point(200, 150));

        var panEvents = 0;
        zoomBorder.PanStarted += (_, _) => panEvents++;
        zoomBorder.PanEnded += (_, _) => panEvents++;

        await recorder.CaptureAsync(); // Marker "WorkflowStart"
        var initialZoom = zoomBorder.ZoomX;

        // Step 1: Tap to focus
        TouchTap(center);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Step 2: Pinch zoom in
        await PinchAsync(center, 50, 100, steps: 5, onStep: recorder.CaptureAsync);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        var zoomAfterPinchIn = zoomBorder.ZoomX;

        // Step 3: Pan around (RecordedDrag uses the left mouse button)
        var offsetBeforePan = new Point(zoomBorder.OffsetX, zoomBorder.OffsetY);
        InputHelper.MouseDrag(center, ToWindow(zoomBorder, new Point(150, 100)), ButtonName.Left, steps: 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        var offsetAfterPan = new Point(zoomBorder.OffsetX, zoomBorder.OffsetY);
        await recorder.CaptureAsync();

        // Step 4: Pinch zoom out
        await PinchAsync(center, 100, 50, steps: 5, onStep: recorder.CaptureAsync);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        await recorder.CaptureAsync(); // Marker "WorkflowEnd"

        var eventCount = gestures.Started.Count + gestures.Ended.Count + panEvents;
        Assert.IsTrue(eventCount >= 5, "Should have multiple events recorded");
        Assert.IsTrue(recorder.Frames.Count >= 2);

        Assert.AreEqual(initialZoom * 2.0, zoomAfterPinchIn, 0.05);
        Assert.AreEqual(2, panEvents);
        Assert.AreEqual(offsetBeforePan.X - 50, offsetAfterPan.X, 1.0);
        Assert.AreEqual(offsetBeforePan.Y - 50, offsetAfterPan.Y, 1.0);
        Assert.AreEqual(initialZoom, zoomBorder.ZoomX, 0.05);
    }

    [TestMethod]
    public async Task Recording_OutputFilesCreated_ForPngSequence()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var recorder = new FrameRecorder(zoomBorder);

        await recorder.CaptureAsync();
        await recorder.CaptureAsync();
        await recorder.CaptureAsync();

        // RecordingFormat.PngSequence with BaseFileName = "zoom_frame".
        var files = recorder.SavePngSequence(GetRecordingsDirectory("png_output"), "zoom_frame");

        Assert.AreEqual(3, files.Count);
        foreach (var file in files)
        {
            Assert.IsTrue(file.EndsWith(".png"), "Output files should be PNG");
            Assert.IsTrue(File.Exists(file));
            Assert.IsTrue(new FileInfo(file).Length > 0);
        }

        // No interaction happened: all the frames are identical.
        Assert.AreEqual(0, CountDifferentPixels(recorder.Frames[0], recorder.Frames[2]));
    }

    [TestMethod]
    public async Task Recording_Statistics_ContainsAccurateData()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var recorder = new FrameRecorder(zoomBorder);

        var tapped = 0;
        zoomBorder.AddHandler(UIElement.TappedEvent, new TappedEventHandler((_, _) => tapped++), true);
        var initialZoom = zoomBorder.ZoomX;

        await recorder.CaptureAsync(); // Marker1
        TouchTap(ToWindow(zoomBorder, new Point(100, 100)));
        await ZoomBorderTestHelper.WaitForIdleAsync();
        await recorder.CaptureAsync(); // Marker2

        Assert.IsTrue(recorder.Duration.TotalMilliseconds >= 0, "Duration should be valid");
        Assert.AreEqual(2, recorder.Frames.Count);
        Assert.AreEqual(1, tapped, "The tap should have been received");
        Assert.AreEqual(initialZoom, zoomBorder.ZoomX, 1e-9);
    }

    [TestMethod]
    public async Task Recording_PinchZoom_DiagnosticZoomValues()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var centerPoint = new Point(zoomBorder.ActualWidth / 2, zoomBorder.ActualHeight / 2);

        var initialZoomX = zoomBorder.ZoomX;
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        await PinchAsync(ToWindow(zoomBorder, centerPoint), 50, 150, steps: 10);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        var finalZoomX = zoomBorder.ZoomX;
        var finalOffsetX = zoomBorder.OffsetX;
        var finalOffsetY = zoomBorder.OffsetY;

        Assert.IsTrue(finalZoomX > initialZoomX,
            $"ZoomX should have increased. Initial: {initialZoomX}, Final: {finalZoomX}. " +
            $"InitialOffset: ({initialOffsetX}, {initialOffsetY}), FinalOffset: ({finalOffsetX}, {finalOffsetY}). " +
            $"CenterPoint: {centerPoint}, ZoomBorder size: {zoomBorder.ActualWidth}x{zoomBorder.ActualHeight}");
    }

    [TestMethod]
    public async Task Recording_ConvertPinchZoomToVideo_CreatesVideoFile()
    {
        // The Avalonia test converts previously recorded PNG sequences: record a pinch zoom sequence here.
        var zoomBorder = await CreateAndLoadAsync();
        var recorder = new FrameRecorder(zoomBorder);
        await recorder.CaptureAsync();
        await PinchAsync(ZoomBorderTestHelper.GetWindowCenter(zoomBorder), 50, 150, steps: 10, onStep: recorder.CaptureAsync);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        var directory = GetRecordingsDirectory("pinch_zoom");
        var files = recorder.SavePngSequence(directory);
        Assert.AreEqual(11, files.Count);
        Assert.IsTrue(files.All(File.Exists));

        // Skip the conversion if FFmpeg is not available.
        if (!IsFfmpegAvailable())
        {
            return;
        }

        var (outputPath, output) = ConvertPngSequenceToVideo(directory);

        Assert.IsNotNull(outputPath, $"Video conversion failed.\nFFmpeg output: {output}");
        Assert.IsTrue(File.Exists(outputPath), $"Video file should exist at {outputPath}");
    }

    [TestMethod]
    public async Task Recording_MultipleGestures_CapturesFrames()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var recorder = await RecordMultipleGesturesAsync(zoomBorder);

        Assert.IsTrue(recorder.Frames.Count > 0, $"Expected frames > 0, got {recorder.Frames.Count}");
        Assert.IsTrue(CountDifferentPixels(recorder.Frames[0], recorder.Frames[^1]) > 0);
    }

    [TestMethod]
    public async Task Recording_ConvertMultipleGesturesToVideo_CreatesVideoFiles()
    {
        // The Avalonia test converts previously recorded multi_gesture PNG sequences: record them here.
        var zoomBorder = await CreateAndLoadAsync();
        var recorder = await RecordMultipleGesturesAsync(zoomBorder);

        var directory = GetRecordingsDirectory("multi_gesture");
        var files = recorder.SavePngSequence(directory);
        Assert.AreEqual(recorder.Frames.Count, files.Count);
        Assert.IsTrue(files.All(File.Exists));

        // Skip the conversion if FFmpeg is not available.
        if (!IsFfmpegAvailable())
        {
            return;
        }

        var (outputPath, output) = ConvertPngSequenceToVideo(directory);

        Assert.IsNotNull(outputPath, $"Video conversion failed.\nFFmpeg output: {output}");
        Assert.IsTrue(File.Exists(outputPath), $"Video file should exist at {outputPath}");

        var fileInfo = new FileInfo(outputPath);
        Assert.IsTrue(fileInfo.Length > 1024, $"Video file should be > 1KB, got {fileInfo.Length} bytes");
    }

    [TestMethod]
    public void VideoConverter_IsFfmpegAvailable_ReturnsStatus()
    {
        // Informational: the test passes whether or not FFmpeg is installed.
        var isAvailable = IsFfmpegAvailable();

        if (isAvailable)
        {
            var version = GetFfmpegVersion();
            Assert.IsNotNull(version);
        }

        Console.WriteLine($"[ZoomBorderRecordingTests] FFmpeg available: {isAvailable}");
    }

    #region Helpers

    private static async Task<FrameRecorder> RecordMultipleGesturesAsync(ZoomBorder zoomBorder)
    {
        var gestures = TrackGestures(zoomBorder);
        var recorder = new FrameRecorder(zoomBorder);
        var center = ToWindow(zoomBorder, new Point(zoomBorder.ActualWidth / 2, zoomBorder.ActualHeight / 2));
        var initialZoom = zoomBorder.ZoomX;
        await recorder.CaptureAsync();

        // 1. Zoom in
        await PinchAsync(center, 50, 100, steps: 5, onStep: recorder.CaptureAsync);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.AreEqual(initialZoom * 2.0, zoomBorder.ZoomX, 0.05, "ZoomIn");

        // 2. Pan (RecordedDrag uses the left mouse button)
        var offsetBeforePan = new Point(zoomBorder.OffsetX, zoomBorder.OffsetY);
        InputHelper.MouseDrag(ToWindow(zoomBorder, new Point(200, 150)), ToWindow(zoomBorder, new Point(150, 100)), ButtonName.Left, steps: 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        await recorder.CaptureAsync();
        Assert.AreEqual(offsetBeforePan.X - 50, zoomBorder.OffsetX, 1.0, "Pan");
        Assert.AreEqual(offsetBeforePan.Y - 50, zoomBorder.OffsetY, 1.0, "Pan");

        // 3. Rotate
        await RotateAsync(center, 30, 0, 45, steps: 5, onStep: recorder.CaptureAsync);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.AreNotEqual(0.0, zoomBorder.Rotation, "Rotate");

        // 4. Zoom out
        var zoomBeforeZoomOut = zoomBorder.ZoomX;
        await PinchAsync(center, 100, 50, steps: 5, onStep: recorder.CaptureAsync);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        await recorder.CaptureAsync();
        Assert.IsTrue(zoomBorder.ZoomX < zoomBeforeZoomOut, "ZoomOut");

        Assert.IsTrue(gestures.Ended.Count(g => g == "Pinch") >= 3);
        return recorder;
    }

    private static async Task<ZoomBorder> CreateAndLoadAsync(Action<ZoomBorder>? configure = null)
    {
        var zoomBorder = CreateZoomBorderWithContent();
        configure?.Invoke(zoomBorder);

        // Avalonia: new Window { Width = 400, Height = 300, Content = zoomBorder } -> a 400x300 ZoomBorder.
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        return zoomBorder;
    }

    private static ZoomBorder CreateZoomBorderWithContent()
    {
        var canvas = new Canvas
        {
            Width = 300,
            Height = 200,
            Background = new SolidColorBrush(Colors.LightGray)
        };

        AddShape(canvas, new Rectangle { Width = 50, Height = 50, Fill = new SolidColorBrush(Colors.Red) }, 25, 25);
        AddShape(canvas, new Ellipse { Width = 60, Height = 60, Fill = new SolidColorBrush(Colors.Blue) }, 100, 70);
        AddShape(canvas, new Rectangle { Width = 40, Height = 80, Fill = new SolidColorBrush(Colors.Green) }, 200, 50);

        return new ZoomBorder
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            ClipToBounds = true,
            Background = new SolidColorBrush(Colors.White),
            PanButton = ButtonName.Left, // TouchInputSimulator uses left button
            Child = canvas
        };

        static void AddShape(Canvas canvas, Shape shape, double left, double top)
        {
            Canvas.SetLeft(shape, left);
            Canvas.SetTop(shape, top);
            canvas.Children.Add(shape);
        }
    }

    private static Point ToWindow(ZoomBorder zoomBorder, Point point) => ZoomBorderTestHelper.ToWindow(zoomBorder, point);

    private static (List<string> Started, List<string> Ended) TrackGestures(ZoomBorder zoomBorder)
    {
        var started = new List<string>();
        var ended = new List<string>();
        zoomBorder.GestureStarted += (_, e) => started.Add(e.GestureType);
        zoomBorder.GestureEnded += (_, e) => ended.Add(e.GestureType);
        return (started, ended);
    }

    #endregion
}
