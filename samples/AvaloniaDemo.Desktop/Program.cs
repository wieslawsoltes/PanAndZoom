using Avalonia;
using System;
using System.Diagnostics;

namespace AvaloniaDemo;

internal class Program
{
    private static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions { UseCompositor = true })
            .With(new X11PlatformOptions { UseCompositor = true })
            .With(new AvaloniaNativePlatformOptions { UseCompositor = true })
            .LogToTrace();
}
