// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.Views;

public sealed partial class AnimationView : UserControl
{
    public AnimationView()
    {
        InitializeComponent();
        DataContext = ZoomBorder;
    }

    private void ApplyDuration_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.AnimationDuration = TimeSpan.FromMilliseconds(DurationSlider.Value);
    }

    private void ZoomInAnimated_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.ZoomIn(skipTransitions: false);
    }

    private void ZoomOutAnimated_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.ZoomOut(skipTransitions: false);
    }

    private void PanRightAnimated_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.PanDelta(100, 0, skipTransitions: false);
    }

    private void ResetAnimated_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.ResetMatrix(skipTransitions: false);
    }

    private void ZoomInInstant_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.ZoomIn(skipTransitions: true);
    }

    private void ResetInstant_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.ResetMatrix(skipTransitions: true);
    }
}
