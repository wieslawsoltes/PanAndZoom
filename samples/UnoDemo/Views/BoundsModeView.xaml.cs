// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Xaml.Controls.Primitives;

namespace UnoDemo.Views;

public sealed partial class BoundsModeView : UserControl
{
    public BoundsModeView()
    {
        InitializeComponent();
        DataContext = ZoomBorder;
    }

    private void BoundsMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ZoomBorder == null) return;

        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ZoomBorder.BoundsMode = tag switch
            {
                "Unrestricted" => ContentBoundsMode.Unrestricted,
                "KeepContentVisible" => ContentBoundsMode.KeepContentVisible,
                "FillViewport" => ContentBoundsMode.FillViewport,
                "KeepCentered" => ContentBoundsMode.KeepCentered,
                _ => ContentBoundsMode.Unrestricted
            };
        }
    }

    private void BoundsPadding_Changed(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (ZoomBorder == null) return;
        var padding = e.NewValue;
        ZoomBorder.BoundsPadding = new Thickness(padding);
    }

    private void MinVisible_Changed(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (ZoomBorder == null) return;
        ZoomBorder.MinimumVisibleContentPercentage = e.NewValue;
    }

    private void OffsetLimits_Changed(object sender, RoutedEventArgs e)
    {
        if (ZoomBorder == null) return;

        if (EnableOffsetLimits.IsChecked == true)
        {
            if (double.TryParse(MinOffsetXBox.Text, out var minX))
                ZoomBorder.MinOffsetX = minX;
            if (double.TryParse(MaxOffsetXBox.Text, out var maxX))
                ZoomBorder.MaxOffsetX = maxX;
        }
        else
        {
            ZoomBorder.MinOffsetX = double.NegativeInfinity;
            ZoomBorder.MaxOffsetX = double.PositiveInfinity;
        }
    }
}
