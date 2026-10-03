// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.Views;

public sealed partial class AdvancedZoomView : UserControl
{
    public AdvancedZoomView()
    {
        InitializeComponent();
        DataContext = ZoomBorder;
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.ZoomIn();
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.ZoomOut();
    }

    private void SetAbsoluteZoom_Click(object sender, RoutedEventArgs e)
    {
        var centerX = ZoomBorder.ActualWidth / 2;
        var centerY = ZoomBorder.ActualHeight / 2;
        ZoomBorder.Zoom(AbsoluteZoomSlider.Value, centerX, centerY);
    }

    private void ZoomToRatio_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(ZoomRatioBox.Text, out var ratio))
        {
            var centerX = ZoomBorder.ActualWidth / 2;
            var centerY = ZoomBorder.ActualHeight / 2;
            ZoomBorder.ZoomTo(ratio, centerX, centerY);
        }
    }

    private void ZoomDelta_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(ZoomDeltaBox.Text, out var delta))
        {
            var centerX = ZoomBorder.ActualWidth / 2;
            var centerY = ZoomBorder.ActualHeight / 2;
            ZoomBorder.ZoomDeltaTo(delta, centerX, centerY);
        }
    }
}
