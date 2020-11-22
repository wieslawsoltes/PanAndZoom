using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace AvaloniaDemo
{
    public class MainWindow : Window
    {
        private readonly ZoomBorder? _zoomBorder;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();

            _zoomBorder = this.Find<ZoomBorder>("ZoomBorder");
            
            if (_zoomBorder != null)
            {
                _zoomBorder.KeyDown += ZoomBorder_KeyDown;
                
                _zoomBorder.ZoomChanged += ZoomBorder_ZoomChanged;
            }

            DataContext = _zoomBorder;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ZoomBorder_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F:
                    _zoomBorder?.Fill();
                    break;
                case Key.U:
                    _zoomBorder?.Uniform();
                    break;
                case Key.R:
                    _zoomBorder?.Reset();
                    break;
                case Key.T:
                    _zoomBorder?.ToggleStretchMode();
                    _zoomBorder?.AutoFit();
                    break;
            }
        }

        private void ZoomBorder_ZoomChanged(object sender, ZoomChangedEventArgs e)
        {
            Debug.WriteLine($"[ZoomChanged] {e.ZoomX} {e.ZoomY} {e.OffsetX} {e.OffsetY}");
        }
    }
}
