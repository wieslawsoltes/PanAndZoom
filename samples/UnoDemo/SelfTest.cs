// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Diagnostics;
using System.Threading;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace UnoDemo;

/// <summary>
/// Smoke test that opens every demo page, checks it and exits the application
/// (exit code 0 when all pages pass, 1 otherwise).
/// </summary>
/// <remarks>
/// Enabled with the <c>UNODEMO_SELFTEST=1</c> environment variable, e.g.
/// <c>UNODEMO_SELFTEST=1 dotnet run -f net10.0-desktop</c>. Every page logs <c>[SelfTest] &lt;page&gt; OK</c>
/// or the failure reason to the standard output.
/// </remarks>
internal static class SelfTest
{
    private static readonly TimeSpan s_loadTimeout = TimeSpan.FromSeconds(10);
    private static int s_unhandledExceptions;

    /// <summary>
    /// Gets a value indicating whether the self test is enabled (<c>UNODEMO_SELFTEST=1</c>).
    /// Set <c>UNODEMO_SELFTEST_LOG</c> to a file path to also write the log to a file.
    /// </summary>
    public static bool IsEnabled => Environment.GetEnvironmentVariable("UNODEMO_SELFTEST") == "1";

    /// <summary>
    /// Records an unhandled exception (fails the self test).
    /// </summary>
    /// <param name="exception">The exception.</param>
    public static void ReportUnhandledException(Exception exception)
    {
        if (IsEnabled)
        {
            Interlocked.Increment(ref s_unhandledExceptions);
            Log($"Unhandled exception: {exception}");
        }
    }

    /// <summary>
    /// Starts the self test once the main page is loaded.
    /// </summary>
    /// <param name="mainPage">The main page.</param>
    public static void Start(MainPage mainPage)
    {
        mainPage.Loaded += async (_, _) =>
        {
            var exitCode = 1;
            try
            {
                exitCode = await RunAsync(mainPage);
            }
            catch (Exception ex)
            {
                Log($"Self test crashed: {ex}");
            }

            Console.Out.Flush();
            Environment.Exit(exitCode);
        };
    }

    private static async Task<int> RunAsync(MainPage mainPage)
    {
        Log($"Starting ({MainPage.Demos.Count} demos, root={mainPage.XamlRoot?.Size.Width:F0}x{mainPage.XamlRoot?.Size.Height:F0}, scale={mainPage.XamlRoot?.RasterizationScale})");
        var stopwatch = Stopwatch.StartNew();
        var failures = 0;

        for (var i = 0; i < MainPage.Demos.Count; i++)
        {
            var name = MainPage.Demos[i].Name;
            try
            {
                var view = mainPage.ShowDemo(i);
                await WaitForLoadedAsync(view);
                // Give the dispatcher a few frames to run layout, deferred auto fit and bindings.
                await Task.Delay(250);

                var details = CheckView(view);
                Log($"{name} OK ({details})");
            }
            catch (Exception ex)
            {
                failures++;
                Log($"{name} FAILED: {ex}");
            }
        }

        if (s_unhandledExceptions > 0)
        {
            failures += s_unhandledExceptions;
            Log($"{s_unhandledExceptions} unhandled exception(s) were raised");
        }

        Log($"Completed {MainPage.Demos.Count} demos in {stopwatch.Elapsed.TotalSeconds:F1}s, {failures} failure(s)");
        return failures == 0 ? 0 : 1;
    }

    private static string CheckView(FrameworkElement view)
    {
        if (view.ActualWidth <= 0 || view.ActualHeight <= 0)
        {
            throw new InvalidOperationException($"The view has no size ({view.ActualWidth}x{view.ActualHeight}).");
        }

        var elements = Descendants(view).ToList();

        // Every page hosts at least one laid out ZoomBorder with a child.
        var zoomBorders = elements.OfType<ZoomBorder>().ToList();
        if (zoomBorders.Count == 0)
        {
            throw new InvalidOperationException("No ZoomBorder found.");
        }

        foreach (var zoomBorder in zoomBorders)
        {
            if (zoomBorder.ActualWidth <= 0 || zoomBorder.ActualHeight <= 0)
            {
                throw new InvalidOperationException($"ZoomBorder '{zoomBorder.Name}' has no size.");
            }

            if (zoomBorder.Child is not FrameworkElement { ActualWidth: > 0, ActualHeight: > 0 })
            {
                throw new InvalidOperationException($"ZoomBorder '{zoomBorder.Name}' child is missing or not laid out.");
            }

            var matrix = zoomBorder.Matrix;
            if (double.IsNaN(matrix.M11) || double.IsNaN(matrix.OffsetX) || double.IsNaN(matrix.OffsetY))
            {
                throw new InvalidOperationException($"ZoomBorder '{zoomBorder.Name}' matrix is invalid ({matrix}).");
            }
        }

        // Command bindings (e.g. {Binding ZoomInCommand, ElementName=ZoomBorder}) must resolve.
        var commandBindings = 0;
        foreach (var button in elements.OfType<ButtonBase>())
        {
            if (button.GetBindingExpression(ButtonBase.CommandProperty) is not null)
            {
                commandBindings++;
                if (button.Command is null)
                {
                    throw new InvalidOperationException($"Command binding of button '{button.Content}' did not resolve.");
                }
            }
        }

        // Formatted texts must not contain unformatted composite format items.
        var texts = 0;
        string? formattedSample = null;
        foreach (var textBlock in elements.OfType<TextBlock>())
        {
            texts++;
            var text = GetText(textBlock);
            if (text.Contains("{0", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"TextBlock shows an unformatted value: '{text}'.");
            }

            // Formatted values are rendered as "label" + "{x:Bind value}" runs: an empty run means the binding failed.
            var runs = textBlock.Inlines.OfType<Run>().ToList();
            if (runs.Count > 1 && runs.Any(r => string.IsNullOrEmpty(r.Text)))
            {
                throw new InvalidOperationException($"TextBlock has an empty bound run: '{text}'.");
            }

            if (runs.Count > 1)
            {
                formattedSample ??= text.Trim();
            }
        }

        var zoomBorder0 = zoomBorders[0];
        return $"ZoomBorders={zoomBorders.Count}, size={zoomBorder0.ActualWidth:F0}x{zoomBorder0.ActualHeight:F0}, " +
               $"zoom={zoomBorder0.ZoomX:F3}, offset=({zoomBorder0.OffsetX:F1}, {zoomBorder0.OffsetY:F1}), " +
               $"commandBindings={commandBindings}, textBlocks={texts}" +
               (formattedSample is null ? "" : $", sample='{formattedSample}'");
    }

    private static string GetText(TextBlock textBlock)
    {
        if (!string.IsNullOrEmpty(textBlock.Text))
        {
            return textBlock.Text;
        }

        return string.Concat(textBlock.Inlines.OfType<Run>().Select(r => r.Text));
    }

    private static async Task WaitForLoadedAsync(FrameworkElement view)
    {
        if (view.IsLoaded)
        {
            return;
        }

        var tcs = new TaskCompletionSource();
        void OnLoaded(object sender, RoutedEventArgs e) => tcs.TrySetResult();
        view.Loaded += OnLoaded;
        try
        {
            if (view.IsLoaded)
            {
                return;
            }

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(s_loadTimeout));
            if (completed != tcs.Task)
            {
                throw new TimeoutException($"The view was not loaded within {s_loadTimeout.TotalSeconds}s.");
            }
        }
        finally
        {
            view.Loaded -= OnLoaded;
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;

            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static void Log(string message)
    {
        var line = $"[SelfTest] {message}";
        Console.WriteLine(line);

        // GUI subsystem apps (WinUI) have no console: optionally mirror the log to a file.
        if (Environment.GetEnvironmentVariable("UNODEMO_SELFTEST_LOG") is { Length: > 0 } logPath)
        {
            try
            {
                System.IO.File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch (Exception)
            {
                // Logging to the file is best effort.
            }
        }
    }
}
