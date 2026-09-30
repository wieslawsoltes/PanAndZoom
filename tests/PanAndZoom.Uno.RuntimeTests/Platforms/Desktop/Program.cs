// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Uno.UI.Hosting;

namespace PanAndZoom.Uno.RuntimeTests;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
