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

                // The Windows input state is global: do not leave contacts, buttons or keys pressed on the desktop.
                Infrastructure.InputHelper.ResetTouchAndPen();
                Infrastructure.InputHelper.Reset();
                await Infrastructure.WinUIInputPump.WhenDrainedAsync();
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
            Log($"The test window lost the foreground (to {DescribeWindow(foreground)}), bringing it back.");
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

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            SetForegroundWindow(hwnd);

            // Windows only lets the foreground process change the foreground window. When another process
            // owns it, share its input state for the call (keyboard and horizontal wheel input goes to the
            // foreground window, so a topmost window is not enough).
            var foreground = GetForegroundWindow();
            if (foreground != hwnd && foreground != IntPtr.Zero)
            {
                var foregroundThread = GetWindowThreadProcessId(foreground, out _);
                var currentThread = GetCurrentThreadId();
                if (foregroundThread != currentThread && AttachThreadInput(currentThread, foregroundThread, true))
                {
                    try
                    {
                        BringWindowToTop(hwnd);
                        SetForegroundWindow(hwnd);
                    }
                    finally
                    {
                        AttachThreadInput(currentThread, foregroundThread, false);
                    }
                }
            }

            if (GetForegroundWindow() != hwnd)
            {
                Log($"Could not bring the test window to front, the foreground window is {DescribeWindow(GetForegroundWindow())}.");
            }
        }
        catch (Exception ex)
        {
            Log($"Could not bring the test window to front: {ex.Message}");
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private static string DescribeWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return "no window";
        }

        try
        {
            var title = new StringBuilder(256);
            GetWindowText(hwnd, title, title.Capacity);
            GetWindowThreadProcessId(hwnd, out var processId);
            using var process = System.Diagnostics.Process.GetProcessById((int)processId);
            return $"\"{title}\" of {process.ProcessName}";
        }
        catch (Exception)
        {
            return $"window 0x{hwnd:X}";
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint attach, uint attachTo, bool attached);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;
    private const uint ES_DISPLAY_REQUIRED = 0x00000002;

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint flags);
}
#endif
