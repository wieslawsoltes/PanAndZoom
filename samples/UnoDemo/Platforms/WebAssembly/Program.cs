// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Uno.UI.Hosting;

namespace UnoDemo;

public class Program
{
    public static async Task Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
}
