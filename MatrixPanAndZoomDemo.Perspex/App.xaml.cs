//#define SKIA_WIN
//#define SKIA_GTK
using System;
using Perspex;
using Perspex.Controls;
using Perspex.Diagnostics;
using Perspex.Themes.Default;
using Perspex.Markup.Xaml;
#if SKIA_WIN
using Perspex.Win32;
using Perspex.Skia;
#endif
#if SKIA_GTK
using Perspex.Gtk;
using Perspex.Skia;
#endif

namespace MatrixPanAndZoomDemo.Perspex
{
    class App : Application
    {
        public App()
        {
            RegisterServices();
#if SKIA_WIN
            Win32Platform.Initialize();
            SkiaPlatform.Initialize();
#elif SKIA_GTK
            GtkPlatform.Initialize();
            SkiaPlatform.Initialize();
#else
            InitializeSubsystems((int)Environment.OSVersion.Platform);
#endif
            Styles = new DefaultTheme();
            PerspexXamlLoader.Load(this);
        }

        static void Main(string[] args)
        {
            var app = new App();
            var window = new MainWindow();
            window.Show();
            app.Run(window);
        }

        public static void AttachDevTools(Window window)
        {
#if DEBUG
            DevTools.Attach(window);
#endif
        }
    }
}
