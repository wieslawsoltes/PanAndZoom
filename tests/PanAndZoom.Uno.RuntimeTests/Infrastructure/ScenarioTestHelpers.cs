// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.UI.Xaml.Automation;
using SkiaSharp;
using Windows.UI.Input.Preview.Injection;

namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

/// <summary>
/// Scenario helpers used by the tests ported from the Avalonia HeadlessTestingFramework based tests
/// (Appium-like driver, gesture simulator and recording). They are thin helpers over the Uno visual tree,
/// real injected touch input and the runtime test engine screenshots.
/// </summary>
public static class ScenarioTestHelpers
{
    private static uint s_nextTouchId = 10_000;

    #region Visual tree queries

    /// <summary>
    /// Enumerates <paramref name="root"/> (optionally) and all its visual descendants (depth first).
    /// </summary>
    public static IEnumerable<DependencyObject> Descendants(DependencyObject root, bool includeSelf = true)
    {
        if (includeSelf)
        {
            yield return root;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, i)))
            {
                yield return child;
            }
        }
    }

    /// <summary>
    /// Gets the root of the test window visual tree.
    /// </summary>
    public static DependencyObject GetWindowRoot(FrameworkElement element)
    {
        if (element.XamlRoot?.Content is { } content)
        {
            return content;
        }

        DependencyObject current = element;
        while (VisualTreeHelper.GetParent(current) is { } parent)
        {
            current = parent;
        }

        return current;
    }

    /// <summary>
    /// Finds all elements matching a predicate below <paramref name="root"/>.
    /// </summary>
    public static IReadOnlyList<FrameworkElement> FindAll(DependencyObject root, Func<FrameworkElement, bool> predicate)
    {
        return Descendants(root).OfType<FrameworkElement>().Where(predicate).ToList();
    }

    /// <summary>
    /// Finds the first element matching a predicate below <paramref name="root"/>, or null.
    /// </summary>
    public static FrameworkElement? FindFirst(DependencyObject root, Func<FrameworkElement, bool> predicate)
    {
        return Descendants(root).OfType<FrameworkElement>().FirstOrDefault(predicate);
    }

    /// <summary>
    /// Finds an element by its <see cref="FrameworkElement.Name"/> (x:Name), or null.
    /// </summary>
    public static FrameworkElement? FindByName(DependencyObject root, string name)
    {
        return FindFirst(root, e => e.Name == name);
    }

    /// <summary>
    /// Finds an element by its <see cref="AutomationProperties.AutomationIdProperty"/>, or null.
    /// </summary>
    public static FrameworkElement? FindByAutomationId(DependencyObject root, string automationId)
    {
        return FindFirst(root, e => AutomationProperties.GetAutomationId(e) == automationId);
    }

    /// <summary>
    /// Finds all elements whose runtime type name (tag name) matches <paramref name="typeName"/>.
    /// </summary>
    public static IReadOnlyList<FrameworkElement> FindByTypeName(DependencyObject root, string typeName)
    {
        return FindAll(root, e => e.GetType().Name == typeName);
    }

    /// <summary>
    /// Finds the first element whose public CLR property <paramref name="propertyName"/> equals <paramref name="value"/>.
    /// </summary>
    public static FrameworkElement? FindByProperty(DependencyObject root, string propertyName, object? value)
    {
        return FindFirst(root, e => TryGetProperty(e, propertyName, out var actual) && Equals(actual, value));
    }

    /// <summary>
    /// Gets a public CLR property value by name (the most derived declaration wins, e.g. <c>new</c> properties).
    /// </summary>
    public static T GetProperty<T>(object target, string propertyName)
    {
        if (!TryGetProperty(target, propertyName, out var value))
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found on {target.GetType().Name}.");
        }

        return (T)value!;
    }

    /// <summary>
    /// Sets a public CLR property value by name.
    /// </summary>
    public static void SetProperty(object target, string propertyName, object? value)
    {
        var property = FindProperty(target.GetType(), propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {target.GetType().Name}.");
        property.SetValue(target, value);
    }

    private static bool TryGetProperty(object target, string propertyName, out object? value)
    {
        var property = FindProperty(target.GetType(), propertyName);
        if (property == null || property.GetIndexParameters().Length != 0)
        {
            value = null;
            return false;
        }

        try
        {
            value = property.GetValue(target);
            return true;
        }
        catch (Exception)
        {
            value = null;
            return false;
        }
    }

    private static PropertyInfo? FindProperty(Type? type, string propertyName)
    {
        for (; type != null; type = type.BaseType)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (property != null)
            {
                return property;
            }
        }

        return null;
    }

    /// <summary>
    /// Serializes the visual tree to an XML like text (type name and x:Name of every element).
    /// </summary>
    public static string GetVisualTreeSource(DependencyObject root)
    {
        var builder = new StringBuilder();
        Write(root, 0);
        return builder.ToString();

        void Write(DependencyObject element, int depth)
        {
            builder.Append(' ', depth * 2).Append('<').Append(element.GetType().Name);
            if (element is FrameworkElement { Name.Length: > 0 } fe)
            {
                builder.Append(" Name=\"").Append(fe.Name).Append('"');
            }

            var count = VisualTreeHelper.GetChildrenCount(element);
            if (count == 0)
            {
                builder.AppendLine(" />");
                return;
            }

            builder.AppendLine(">");
            for (var i = 0; i < count; i++)
            {
                Write(VisualTreeHelper.GetChild(element, i), depth + 1);
            }

            builder.Append(' ', depth * 2).Append("</").Append(element.GetType().Name).AppendLine(">");
        }
    }

    /// <summary>
    /// Gets a value indicating whether the element is loaded, visible and has a non empty size.
    /// </summary>
    public static bool IsDisplayed(FrameworkElement element)
    {
        return element.IsLoaded
            && element.Visibility == Visibility.Visible
            && element.ActualWidth > 0
            && element.ActualHeight > 0;
    }

    /// <summary>
    /// Gets the bounds of an element in window coordinates.
    /// </summary>
    public static Rect GetWindowBounds(FrameworkElement element)
    {
        var topLeft = ZoomBorderTestHelper.ToWindow(element, new Point(0, 0));
        return new Rect(topLeft.X, topLeft.Y, element.ActualWidth, element.ActualHeight);
    }

    #endregion

    #region Touch input

    /// <summary>
    /// Creates a touch injection session in which contacts can be injected frame by frame (with awaits in between).
    /// </summary>
    public static TouchSession BeginTouch() => new();

    /// <summary>
    /// Allocates a new unique touch contact identifier.
    /// </summary>
    public static uint NextTouchId() => s_nextTouchId++;

    /// <summary>
    /// Taps with a single finger at the provided window position.
    /// </summary>
    public static void TouchTap(Point position)
    {
        using var touch = BeginTouch();
        var id = NextTouchId();
        touch.Inject(TouchSession.Down(id, position));
        touch.Inject(TouchSession.Up(id, position));
    }

    /// <summary>
    /// Taps <paramref name="count"/> times with a single finger at the provided window position.
    /// </summary>
    /// <remarks>
    /// All the taps use the same contact identifier: the Uno gesture recognizer only detects multi taps
    /// (double tap) for consecutive taps of the same pointer.
    /// </remarks>
    public static void TouchMultiTap(Point position, int count)
    {
        using var touch = BeginTouch();
        var id = NextTouchId();
        for (var i = 0; i < count; i++)
        {
            touch.Inject(TouchSession.Down(id, position));
            touch.Inject(TouchSession.Up(id, position));
        }
    }

    /// <summary>
    /// Double taps with a single finger at the provided window position.
    /// </summary>
    public static void TouchDoubleTap(Point position) => TouchMultiTap(position, 2);

    /// <summary>
    /// Presses a finger at the provided window position, keeps it pressed and releases it.
    /// </summary>
    public static async Task TouchHoldAsync(Point position, int holdMilliseconds)
    {
        using var touch = BeginTouch();
        var id = NextTouchId();
        touch.Inject(TouchSession.Down(id, position));
        await Task.Delay(holdMilliseconds);
        touch.Inject(TouchSession.Up(id, position));
    }

    /// <summary>
    /// Moves a single finger along a path (window coordinates). The first point is the press position and the last
    /// point the release position.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="stepDelayMilliseconds">Delay between the moves (0 for none).</param>
    /// <param name="onStep">Optional callback invoked after each move (e.g. to capture a frame).</param>
    public static async Task TouchPathAsync(IReadOnlyList<Point> path, int stepDelayMilliseconds = 0, Func<Task>? onStep = null)
    {
        using var touch = BeginTouch();
        var id = NextTouchId();
        touch.Inject(TouchSession.Down(id, path[0]));

        for (var i = 1; i < path.Count; i++)
        {
            if (stepDelayMilliseconds > 0)
            {
                await Task.Delay(stepDelayMilliseconds);
            }

            touch.Inject(TouchSession.Move(id, path[i]));

            if (onStep != null)
            {
                await onStep();
            }
        }

        touch.Inject(TouchSession.Up(id, path[^1]));
    }

    /// <summary>
    /// Drags a single finger linearly between two window positions.
    /// </summary>
    public static Task TouchDragAsync(Point from, Point to, int steps = 10, int stepDelayMilliseconds = 0, Func<Task>? onStep = null)
    {
        return TouchPathAsync(Enumerable.Range(0, steps + 1).Select(i => Lerp(from, to, i / (double)steps)).ToList(), stepDelayMilliseconds, onStep);
    }

    /// <summary>
    /// Performs a two finger gesture where the finger positions are provided for a normalized time (0..1).
    /// </summary>
    /// <param name="positionsAt">Returns both finger positions (window coordinates) at time t.</param>
    /// <param name="steps">Number of move steps.</param>
    /// <param name="onStep">Optional callback invoked after each move (e.g. to capture a frame).</param>
    public static async Task TwoFingerGestureAsync(Func<double, (Point First, Point Second)> positionsAt, int steps = 10, Func<Task>? onStep = null)
    {
        using var touch = BeginTouch();
        var id1 = NextTouchId();
        var id2 = NextTouchId();

        var (start1, start2) = positionsAt(0);
        touch.Inject(TouchSession.Down(id1, start1));
        touch.Inject(TouchSession.Move(id1, start1), TouchSession.Down(id2, start2));

        var (end1, end2) = (start1, start2);
        for (var step = 1; step <= steps; step++)
        {
            (end1, end2) = positionsAt(step / (double)steps);
            touch.Inject(TouchSession.Move(id1, end1), TouchSession.Move(id2, end2));

            if (onStep != null)
            {
                await onStep();
            }
        }

        touch.Inject(TouchSession.Move(id1, end1), TouchSession.Up(id2, end2));
        touch.Inject(TouchSession.Up(id1, end1));
    }

    /// <summary>
    /// Pinches two horizontally aligned fingers around a window position.
    /// </summary>
    public static Task PinchAsync(Point center, double startDistance, double endDistance, int steps = 10, Func<Task>? onStep = null)
    {
        return TwoFingerGestureAsync(t =>
        {
            var distance = startDistance + (endDistance - startDistance) * t;
            return (new Point(center.X - distance / 2, center.Y), new Point(center.X + distance / 2, center.Y));
        }, steps, onStep);
    }

    /// <summary>
    /// Rotates two fingers placed on a circle around a window position from <paramref name="startAngle"/>
    /// to <paramref name="endAngle"/> (degrees, clockwise on screen).
    /// </summary>
    public static Task RotateAsync(Point center, double radius, double startAngle, double endAngle, int steps = 10, Func<Task>? onStep = null)
    {
        return TwoFingerGestureAsync(t =>
        {
            var angle = (startAngle + (endAngle - startAngle) * t) * Math.PI / 180.0;
            var dx = Math.Cos(angle) * radius;
            var dy = Math.Sin(angle) * radius;
            return (new Point(center.X - dx, center.Y - dy), new Point(center.X + dx, center.Y + dy));
        }, steps, onStep);
    }

    /// <summary>
    /// Translates two fingers (placed horizontally <paramref name="fingerSpacing"/> apart) from one window position to another.
    /// </summary>
    public static Task TwoFingerPanAsync(Point from, Point to, double fingerSpacing = 40, int steps = 10, Func<Task>? onStep = null)
    {
        return TwoFingerGestureAsync(t =>
        {
            var center = Lerp(from, to, t);
            return (new Point(center.X - fingerSpacing / 2, center.Y), new Point(center.X + fingerSpacing / 2, center.Y));
        }, steps, onStep);
    }

    /// <summary>
    /// Linear interpolation between two points.
    /// </summary>
    public static Point Lerp(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    /// <summary>
    /// A touch injection session (see <see cref="BeginTouch"/>).
    /// </summary>
    public sealed class TouchSession : IDisposable
    {
        private const InjectedInputPointerOptions DownOptions = InjectedInputPointerOptions.New | InjectedInputPointerOptions.PointerDown | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.FirstButton;
        private const InjectedInputPointerOptions MoveOptions = InjectedInputPointerOptions.Update | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.FirstButton;
        private const InjectedInputPointerOptions UpOptions = InjectedInputPointerOptions.PointerUp | InjectedInputPointerOptions.FirstButton;

        private readonly InputInjector _injector;

        internal TouchSession()
        {
            _injector = InputInjectorHelper.Current.Injector;
            _injector.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
        }

        /// <summary>
        /// Injects one frame of touch contacts.
        /// </summary>
        public void Inject(params InjectedInputTouchInfo[] contacts) => _injector.InjectTouchInput(contacts);

        /// <summary>
        /// Creates a contact down.
        /// </summary>
        public static InjectedInputTouchInfo Down(uint id, Point position) => Contact(id, position, DownOptions);

        /// <summary>
        /// Creates a contact move.
        /// </summary>
        public static InjectedInputTouchInfo Move(uint id, Point position) => Contact(id, position, MoveOptions);

        /// <summary>
        /// Creates a contact up.
        /// </summary>
        public static InjectedInputTouchInfo Up(uint id, Point position) => Contact(id, position, UpOptions);

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

        /// <inheritdoc/>
        public void Dispose() => _injector.UninitializeTouchInjection();
    }

    #endregion

    #region Screenshots and frames

    /// <summary>
    /// Takes a screenshot of an element using the runtime test engine.
    /// </summary>
    public static async Task<TestBitmap> ScreenShotAsync(FrameworkElement element)
    {
        return await UIHelper.ScreenShot(element);
    }

    /// <summary>
    /// Counts the physical pixels that differ between two screenshots (all pixels when the sizes differ).
    /// </summary>
    public static int CountDifferentPixels(TestBitmap a, TestBitmap b)
    {
        var pa = a.GetRawPixels();
        var pb = b.GetRawPixels();
        if (pa.Length != pb.Length)
        {
            return Math.Max(pa.Length, pb.Length) / 4;
        }

        var count = 0;
        for (var i = 0; i < pa.Length; i += 4)
        {
            if (pa[i] != pb[i] || pa[i + 1] != pb[i + 1] || pa[i + 2] != pb[i + 2] || pa[i + 3] != pb[i + 3])
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Gets the directory used to store recorded frames (<c>artifacts/recordings</c> of the repository when found).
    /// </summary>
    public static string GetRecordingsDirectory(string name)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "PanAndZoom.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? Path.GetTempPath();
        var path = Path.Combine(root, "artifacts", "recordings", "uno", $"{name}_{DateTime.Now:yyyyMMdd_HHmmss_fff}");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Captures screenshots of an element (frames) during an interaction. Replaces the Avalonia screen recorder:
    /// frames are runtime test engine screenshots that can be compared and saved as a PNG sequence.
    /// </summary>
    public sealed class FrameRecorder
    {
        private readonly FrameworkElement _target;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameRecorder"/> class.
        /// </summary>
        public FrameRecorder(FrameworkElement target)
        {
            _target = target;
        }

        /// <summary>
        /// Gets the captured frames.
        /// </summary>
        public List<TestBitmap> Frames { get; } = new();

        /// <summary>
        /// Gets the elapsed time since the recorder was created.
        /// </summary>
        public TimeSpan Duration => _stopwatch.Elapsed;

        /// <summary>
        /// Captures a frame.
        /// </summary>
        public async Task CaptureAsync()
        {
            Frames.Add(await ScreenShotAsync(_target));
        }

        /// <summary>
        /// Saves the frames as a PNG sequence (<c>{baseName}_0000.png</c>, ...).
        /// </summary>
        public IReadOnlyList<string> SavePngSequence(string directory, string baseName = "frame")
        {
            Directory.CreateDirectory(directory);
            var files = new List<string>();
            for (var i = 0; i < Frames.Count; i++)
            {
                var file = Path.Combine(directory, $"{baseName}_{i:D4}.png");
                SavePng(Frames[i], file);
                files.Add(file);
            }

            return files;
        }
    }

    /// <summary>
    /// Saves a screenshot as a PNG file.
    /// </summary>
    public static void SavePng(TestBitmap bitmap, string path)
    {
        var pixels = bitmap.GetRawPixels();
        var width = (int)Math.Round(bitmap.Width * bitmap.ImplicitScaling);
        if (width <= 0 || pixels.Length % (width * 4) != 0)
        {
            width = bitmap.Width;
        }

        var height = pixels.Length / 4 / width;

        // The engine un-multiplies the alpha of the BGRA screenshot pixels.
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        using var skBitmap = new SKBitmap(info);
        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, skBitmap.GetPixels(), width * height * 4);
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    #endregion

    #region FFmpeg

    /// <summary>
    /// Gets a value indicating whether FFmpeg is available on the PATH.
    /// </summary>
    public static bool IsFfmpegAvailable() => GetFfmpegVersion() != null;

    /// <summary>
    /// Gets the first line of <c>ffmpeg -version</c>, or null when FFmpeg is not available.
    /// </summary>
    public static string? GetFfmpegVersion()
    {
        var (exitCode, output) = RunFfmpeg("-version", TimeSpan.FromSeconds(10));
        return exitCode == 0 ? output.Split('\n').FirstOrDefault()?.Trim() : null;
    }

    /// <summary>
    /// Converts a PNG sequence (<c>{baseName}_%04d.png</c>) to an MP4 video.
    /// </summary>
    /// <returns>The video path when the conversion succeeded, otherwise null and the FFmpeg output.</returns>
    public static (string? OutputPath, string Output) ConvertPngSequenceToVideo(string directory, string baseName = "frame", int frameRate = 30, int quality = 23)
    {
        var output = Path.Combine(directory, $"{baseName}.mp4");
        var input = Path.Combine(directory, $"{baseName}_%04d.png");
        var (exitCode, log) = RunFfmpeg(
            $"-y -framerate {frameRate} -i \"{input}\" -c:v libx264 -crf {quality} -pix_fmt yuv420p -vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2\" \"{output}\"",
            TimeSpan.FromSeconds(30));
        return (exitCode == 0 && File.Exists(output) ? output : null, log);
    }

    private static (int ExitCode, string Output) RunFfmpeg(string arguments, TimeSpan timeout)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("ffmpeg", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null)
            {
                return (-1, string.Empty);
            }

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Kill();
                return (-1, "timeout");
            }

            return (process.ExitCode, stdout.Result + stderr.Result);
        }
        catch (Exception ex)
        {
            return (-1, ex.Message);
        }
    }

    #endregion
}
