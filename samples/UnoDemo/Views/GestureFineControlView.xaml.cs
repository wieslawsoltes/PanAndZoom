// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.Views;

public sealed partial class GestureFineControlView : UserControl
{
    public GestureFineControlView()
    {
        InitializeComponent();
        DataContext = ZoomBorder;
    }

    private void GestureZoom_Changed(object sender, RoutedEventArgs e)
    {
        ZoomBorder.EnableGestureZoom = GestureZoomCheck.IsChecked ?? false;
    }

    private void GestureTranslation_Changed(object sender, RoutedEventArgs e)
    {
        ZoomBorder.EnableGestureTranslation = GestureTranslationCheck.IsChecked ?? false;
    }

    private void BothEnabled_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.EnableGestureZoom = true;
        ZoomBorder.EnableGestureTranslation = true;
        GestureZoomCheck.IsChecked = true;
        GestureTranslationCheck.IsChecked = true;
    }

    private void ZoomOnly_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.EnableGestureZoom = true;
        ZoomBorder.EnableGestureTranslation = false;
        GestureZoomCheck.IsChecked = true;
        GestureTranslationCheck.IsChecked = false;
    }

    private void PanOnly_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.EnableGestureZoom = false;
        ZoomBorder.EnableGestureTranslation = true;
        GestureZoomCheck.IsChecked = false;
        GestureTranslationCheck.IsChecked = true;
    }

    private void BothDisabled_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.EnableGestureZoom = false;
        ZoomBorder.EnableGestureTranslation = false;
        GestureZoomCheck.IsChecked = false;
        GestureTranslationCheck.IsChecked = false;
    }
}
