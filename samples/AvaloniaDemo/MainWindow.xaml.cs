using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace AvaloniaDemo
{
    public class MainWindow : Window
    {
        private PanAndZoom panAndZoom;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();

            panAndZoom = this.Find<PanAndZoom>("panAndZoom");
            panAndZoom.KeyDown += PanAndZoom_KeyDown;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void PanAndZoom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.E)
            {
                panAndZoom.Extent();
            }

            if (e.Key == Key.F)
            {
                panAndZoom.Fill();
            }

            if (e.Key == Key.R)
            {
                panAndZoom.Reset();
            }

            if (e.Key == Key.T)
            {
                panAndZoom.ToggleAutoFitMode();
                panAndZoom.AutoFit();
            }
        }
    }
}
