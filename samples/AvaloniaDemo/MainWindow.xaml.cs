// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace AvaloniaDemo
{
    public class MainWindow : Window
    {
        private ZoomBorder zoomBorder;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();

            zoomBorder = this.Find<ZoomBorder>("zoomBorder");
            zoomBorder.KeyDown += ZoomBorder_KeyDown;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ZoomBorder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F)
            {
                zoomBorder.Fill();
            }

            if (e.Key == Key.U)
            {
                zoomBorder.Uniform();
            }

            if (e.Key == Key.R)
            {
                zoomBorder.Reset();
            }

            if (e.Key == Key.T)
            {
                zoomBorder.ToggleStretchMode();
                zoomBorder.AutoFit();
            }
        }
    }
}
