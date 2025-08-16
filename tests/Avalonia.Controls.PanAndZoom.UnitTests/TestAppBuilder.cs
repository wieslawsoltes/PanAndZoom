using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Avalonia.Controls.PanAndZoom.UnitTests.TestAppBuilder))]

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}