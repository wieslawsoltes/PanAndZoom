// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.Views;

public sealed partial class ZoomToRectView : UserControl
{
    public ZoomToRectView()
    {
        InitializeComponent();
    }

    private void ZoomToRed_Click(object sender, RoutedEventArgs e) => ZoomBorder.ZoomToRectangle(new Rect(50, 100, 200, 150));
    private void ZoomToBlue_Click(object sender, RoutedEventArgs e) => ZoomBorder.ZoomToRectangle(new Rect(300, 100, 200, 150));
    private void ZoomToGreen_Click(object sender, RoutedEventArgs e) => ZoomBorder.ZoomToRectangle(new Rect(550, 100, 200, 150));
    private void ZoomToAll_Click(object sender, RoutedEventArgs e) => ZoomBorder.ZoomToRectangle(new Rect(50, 100, 700, 150));

    private void ZoomToCustom_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(RectX.Text, out var x) &&
            double.TryParse(RectY.Text, out var y) &&
            double.TryParse(RectWidth.Text, out var width) &&
            double.TryParse(RectHeight.Text, out var height))
        {
            ZoomBorder.ZoomToRectangle(new Rect(x, y, width, height));
        }
    }
}
