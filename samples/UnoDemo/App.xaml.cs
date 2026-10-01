// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Uno.Resizetizer;

namespace UnoDemo;

/// <summary>
/// The PanAndZoom Uno Platform demo application.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();

        UnhandledException += (_, e) =>
        {
            Console.WriteLine($"[UnhandledException] {e.Exception}");
            SelfTest.ReportUnhandledException(e.Exception);
        };
    }

    /// <summary>
    /// Gets the main application window.
    /// </summary>
    public static Window? MainWindow { get; private set; }

    /// <inheritdoc/>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window
        {
            Title = "PanAndZoom"
        };

        var mainPage = new MainPage();
        MainWindow.Content = mainPage;

        mainPage.Loaded += (_, _) => TryResizeWindow(MainWindow, mainPage.XamlRoot?.RasterizationScale ?? 1.0, 1400, 800);
        MainWindow.SetWindowIcon();
        MainWindow.Activate();

        if (SelfTest.IsEnabled)
        {
            SelfTest.Start(mainPage);
        }
    }

    private static bool s_windowResized;

    /// <summary>
    /// Gives the desktop window the size of the Avalonia demo window (plus the navigation pane).
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="scale">The rasterization scale (AppWindow sizes are in physical pixels).</param>
    /// <param name="width">The width in logical pixels.</param>
    /// <param name="height">The height in logical pixels.</param>
    private static void TryResizeWindow(Window window, double scale, int width, int height)
    {
        if (s_windowResized || (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux()))
        {
            return;
        }

        s_windowResized = true;

        try
        {
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32
            {
                Width = (int)Math.Round(width * scale),
                Height = (int)Math.Round(height * scale)
            });
        }
        catch (Exception)
        {
            // Resizing is best effort (not supported by every Uno host).
        }
    }

    /// <summary>
    /// Configures global Uno Platform logging.
    /// </summary>
    public static void InitializeLogging()
    {
#if DEBUG
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup.
        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
            builder.AddConsole();
#else
            builder.AddConsole();
#endif

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
}
