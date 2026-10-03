// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
#if PANANDZOOM_WINUI
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.UI.Xaml;
using Uno.UI.RuntimeTests;

namespace PanAndZoom.Uno.RuntimeTests;

/// <summary>
/// In-memory replacement for <c>ApplicationData.Current.LocalSettings.Values</c> (which requires package
/// identity) used by the runtime test engine UI in the unpackaged WinUI test app.
/// </summary>
internal static class UnpackagedSettings
{
    public static IDictionary<string, object> Values { get; } = new Dictionary<string, object>();
}

/// <summary>
/// Headless runner for the native WinUI test app.
/// </summary>
/// <remarks>
/// The embedded runner of Uno.UI.RuntimeTests.Engine relies on <c>Window.Current</c>, which is
/// not available for WinUI 3 desktop apps, so the WinUI test app uses this runner, configured with
/// its own environment variables (so the embedded runner stays idle):
/// <c>PANANDZOOM_WINUI_TESTS</c> (<c>true</c> or a filter) and
/// <c>PANANDZOOM_WINUI_TESTS_OUTPUT</c> (NUnit XML results file).
/// </remarks>
internal static class WinUITestRunner
{
    private static bool s_started;

    private static string? OutputPath => Environment.GetEnvironmentVariable("PANANDZOOM_WINUI_TESTS_OUTPUT");

    public static void Initialize(Application application)
    {
        application.UnhandledException += (_, e) => Log($"Unhandled exception: {e.Exception}");
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log($"Unhandled domain exception: {e.ExceptionObject}");
    }

    public static void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            return;
        }

        try
        {
            File.AppendAllText(OutputPath + ".log", $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch (Exception)
        {
            // Logging is best effort.
        }
    }

    public static void StartIfConfigured(Window window, UnitTestsControl testsControl)
    {
        var run = Environment.GetEnvironmentVariable("PANANDZOOM_WINUI_TESTS");
        var outputPath = Environment.GetEnvironmentVariable("PANANDZOOM_WINUI_TESTS_OUTPUT");
        if (string.IsNullOrWhiteSpace(run) || string.IsNullOrWhiteSpace(outputPath) || run.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Log($"Runner configured (filter: {run}).");
        UnitTestsUIContentHelper.CurrentTestWindow = window;
        s_window = window;
        testsControl.Loaded += async (_, _) =>
        {
            if (s_started)
            {
                return;
            }

            s_started = true;
            var exitCode = 0;
            try
            {
                var config = new UnitTestEngineConfig
                {
                    Filter = run.Equals("true", StringComparison.OrdinalIgnoreCase) ? null : run
                };

                BringToFront(window);
                Log("Running tests.");
                await testsControl.RunTests(CancellationToken.None, config);
                await File.WriteAllTextAsync(outputPath, testsControl.NUnitTestResultsDocument, Encoding.UTF8);
                Log("Results written.");
            }
            catch (Exception ex)
            {
                Log($"Runtime tests failed to run: {ex}");
                exitCode = 1;
            }

            Environment.Exit(exitCode);
        };
    }

    private static Window? s_window;

    /// <summary>
    /// Brings the test window back to the foreground when another window took it (injected input
    /// goes to the foreground window). Called before every test.
    /// </summary>
    public static void EnsureForeground()
    {
        if (s_window == null)
        {
            return;
        }

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(s_window);
        var foreground = GetForegroundWindow();
        if (foreground != hwnd)
        {
            Log($"The test window lost the foreground (to window 0x{foreground:X}), bringing it back.");
            BringToFront(s_window);
        }
    }

    /// <summary>
    /// Injected mouse and touch input is delivered to the window under the cursor, so keep the test
    /// window maximized, on top and in the foreground while the tests run.
    /// </summary>
    private static void BringToFront(Window window)
    {
        // Injected input is not delivered while the display sleeps: keep the display on during the run.
        SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);

        try
        {
            if (window.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = true;
                presenter.Maximize();
            }

            SetForegroundWindow(WinRT.Interop.WindowNative.GetWindowHandle(window));
        }
        catch (Exception ex)
        {
            Log($"Could not bring the test window to front: {ex.Message}");
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;
    private const uint ES_DISPLAY_REQUIRED = 0x00000002;

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint flags);
}
#endif
