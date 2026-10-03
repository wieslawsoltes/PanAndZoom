// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Xaml.Controls.Primitives;

namespace UnoDemo.Views;

public sealed partial class CenterControlsView : UserControl
{
    public CenterControlsView()
    {
        InitializeComponent();
        DataContext = ZoomBorder;
    }

    private void CenterPoint1_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.CenterOn(new Point(100, 100));
    }

    private void CenterPoint2_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.CenterOn(new Point(300, 200));
    }

    private void CenterPoint3_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.CenterOn(new Point(500, 300));
    }

    private void CenterPointZoom_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(CenterZoomBox.Text, out var zoom))
        {
            ZoomBorder.CenterOn(new Point(100, 100), zoom);
        }
    }

    private void CenterRect1_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.CenterOn(new Rect(50, 350, 250, 150));
    }

    private void CenterRect2_Click(object sender, RoutedEventArgs e)
    {
        ZoomBorder.CenterOn(new Rect(450, 350, 300, 200));
    }

    private void CenterPadding_Changed(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (ZoomBorder == null) return;
        var padding = e.NewValue;
        ZoomBorder.CenterPadding = new Thickness(padding);
    }
}
