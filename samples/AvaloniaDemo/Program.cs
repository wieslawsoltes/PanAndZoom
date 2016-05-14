using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Serilog;
using System;
using System.Diagnostics;

namespace AvaloniaDemo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                InitializeLogging();
                AppBuilder.Configure<App>()
                    .UseWin32()
                    .UseDirect2D1()
                    .Start<MainWindow>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private static void InitializeLogging()
        {
#if DEBUG
            SerilogLogger.Initialize(new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
                .CreateLogger());
#endif
        }
    }
}
