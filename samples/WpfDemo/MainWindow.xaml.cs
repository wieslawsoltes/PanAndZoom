// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Windows;
using System.Windows.Input;

namespace WpfDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            zoomBorder.KeyDown += ZoomBorder_KeyDown;
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
