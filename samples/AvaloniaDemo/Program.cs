// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
                    .UsePlatformDetect()
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
