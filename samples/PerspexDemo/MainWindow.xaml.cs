using Perspex.Controls;
using Perspex.Controls.PanAndZoom;
using Perspex.Input;
using Perspex.Markup.Xaml;

namespace PerspexDemo
{
    public class MainWindow : Window
    {
        private PanAndZoom panAndZoom;

        public MainWindow()
        {
            this.InitializeComponent();
            App.AttachDevTools(this);

            panAndZoom = this.Find<PanAndZoom>("panAndZoom");
            panAndZoom.KeyDown += PanAndZoom_KeyDown;
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
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
