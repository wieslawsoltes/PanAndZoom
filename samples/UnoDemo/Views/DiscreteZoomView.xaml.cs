// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.Views;

public sealed partial class DiscreteZoomView : UserControl
{
    public DiscreteZoomView()
    {
        InitializeComponent();

        // Avalonia: DiscreteZoomLevels="0.25,0.5,0.75,1.0,1.5,2.0,3.0,4.0"
        ZoomBorder.DiscreteZoomLevels = [0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 3.0, 4.0];

        DataContext = ZoomBorder;
    }

    private double CenterX => ZoomBorder.ActualWidth / 2;

    private double CenterY => ZoomBorder.ActualHeight / 2;

    private void ZoomNext_Click(object sender, RoutedEventArgs e)
    {
        var nextLevel = ZoomBorder.GetNextDiscreteZoomLevel();
        ZoomBorder.ZoomToLevel(nextLevel, CenterX, CenterY);
    }

    private void ZoomPrevious_Click(object sender, RoutedEventArgs e)
    {
        var prevLevel = ZoomBorder.GetPreviousDiscreteZoomLevel();
        ZoomBorder.ZoomToLevel(prevLevel, CenterX, CenterY);
    }

    private void Zoom25_Click(object sender, RoutedEventArgs e) => ZoomBorder.ZoomToLevel(0.25, CenterX, CenterY);
    private void Zoom50_Click(object sender, RoutedEventArgs e) => ZoomBorder.ZoomToLevel(0.5, CenterX, CenterY);
    private void Zoom100_Click(object sender, RoutedEventArgs e) => ZoomBorder.ZoomToLevel(1.0, CenterX, CenterY);
    private void Zoom200_Click(object sender, RoutedEventArgs e) => ZoomBorder.ZoomToLevel(2.0, CenterX, CenterY);
}
