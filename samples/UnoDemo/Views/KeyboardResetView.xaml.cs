// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.Views;

public sealed partial class KeyboardResetView : UserControl
{
    public KeyboardResetView()
    {
        InitializeComponent();
        DataContext = ZoomBorder;

        // F/U/R/T are advertised by this page but not handled by the ZoomBorder itself.
        DemoKeyboard.AttachStretchShortcuts(ZoomBorder);
    }

    private void ResetMatrix_Click(object sender, RoutedEventArgs e) => ZoomBorder.ResetMatrix();
    private void FitToScreen_Click(object sender, RoutedEventArgs e) => ZoomBorder.Uniform();
    private void FillToScreen_Click(object sender, RoutedEventArgs e) => ZoomBorder.Fill();
    private void AutoFit_Click(object sender, RoutedEventArgs e) => ZoomBorder.AutoFit();
    private void ToggleStretch_Click(object sender, RoutedEventArgs e) => ZoomBorder.ToggleStretchMode();
}
