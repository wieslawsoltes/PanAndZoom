// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.Views;

public sealed partial class ProgrammaticPanView : UserControl
{
    public ProgrammaticPanView()
    {
        InitializeComponent();
        DataContext = ZoomBorder;
    }

    private void PanTo_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(PanToX.Text, out var x) && double.TryParse(PanToY.Text, out var y))
        {
            // Use CenterOn to center the viewport on the content point
            ZoomBorder.CenterOn(new Point(x, y));
        }
    }

    private void PanDelta_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(PanDeltaX.Text, out var dx) && double.TryParse(PanDeltaY.Text, out var dy))
        {
            ZoomBorder.PanDelta(dx, dy);
        }
    }

    private void PanUp_Click(object sender, RoutedEventArgs e) => ZoomBorder.PanDelta(0, -50);
    private void PanDown_Click(object sender, RoutedEventArgs e) => ZoomBorder.PanDelta(0, 50);
    private void PanLeft_Click(object sender, RoutedEventArgs e) => ZoomBorder.PanDelta(-50, 0);
    private void PanRight_Click(object sender, RoutedEventArgs e) => ZoomBorder.PanDelta(50, 0);
}
