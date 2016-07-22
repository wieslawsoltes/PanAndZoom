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

            panAndZoom.KeyDown += PanAndZoom_KeyDown;
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
