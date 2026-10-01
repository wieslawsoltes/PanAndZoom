// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Xaml.Controls.Primitives;

namespace UnoDemo.Views;

/// <summary>
/// The "Basic Demo" tab of the Avalonia <c>MainView</c>.
/// </summary>
public sealed partial class BasicDemoView : UserControl
{
    private bool _updatingScrollBars;

    public BasicDemoView()
    {
        InitializeComponent();

        // Avalonia: ItemsSource="{x:Static paz:ZoomBorder.StretchModes}" / "{x:Static paz:ZoomBorder.ButtonNames}".
        PanButtonCombo.ItemsSource = ZoomBorder.ButtonNames;
        StretchCombo.ItemsSource = ZoomBorder.StretchModes;
        StretchList.ItemsSource = ZoomBorder.StretchModes;
        PanButtonList.ItemsSource = ZoomBorder.ButtonNames;

        // Avalonia: DataContext="{Binding #ZoomBorder1}".
        SettingsPanel.DataContext = ZoomBorder1;
        StatePanel.DataContext = ZoomBorder1;

        DemoKeyboard.AttachStretchShortcuts(ZoomBorder1);

        ZoomBorder1.ScrollInvalidated += (_, _) => UpdateScrollBars();
        ZoomBorder1.SizeChanged += (_, _) => UpdateScrollBars();
        HorizontalBar.ValueChanged += ScrollBar_ValueChanged;
        VerticalBar.ValueChanged += ScrollBar_ValueChanged;
    }

    private void UpdateScrollBars()
    {
        _updatingScrollBars = true;
        try
        {
            var extent = ZoomBorder1.Extent;
            var viewport = ZoomBorder1.Viewport;
            var offset = ZoomBorder1.ScrollOffset;

            UpdateScrollBar(HorizontalBar, extent.Width, viewport.Width, offset.X);
            UpdateScrollBar(VerticalBar, extent.Height, viewport.Height, offset.Y);
        }
        finally
        {
            _updatingScrollBars = false;
        }
    }

    private static void UpdateScrollBar(ScrollBar scrollBar, double extent, double viewport, double offset)
    {
        var maximum = Math.Max(0, extent - viewport);
        if (double.IsNaN(maximum) || double.IsInfinity(maximum) || maximum < 0.5)
        {
            scrollBar.Visibility = Visibility.Collapsed;
            return;
        }

        scrollBar.Maximum = maximum;
        scrollBar.ViewportSize = viewport;
        scrollBar.LargeChange = viewport;
        scrollBar.SmallChange = 16;
        scrollBar.Value = Math.Clamp(offset, 0, maximum);
        scrollBar.Visibility = Visibility.Visible;
    }

    private void ScrollBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_updatingScrollBars)
        {
            return;
        }

        var offset = ZoomBorder1.ScrollOffset;
        ZoomBorder1.ScrollOffset = ReferenceEquals(sender, HorizontalBar)
            ? new Point(e.NewValue, offset.Y)
            : new Point(offset.X, e.NewValue);
    }
}
