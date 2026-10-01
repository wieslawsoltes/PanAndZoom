// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.Views;

public sealed partial class DoubleClickZoomView : UserControl
{
    public DoubleClickZoomView()
    {
        InitializeComponent();

        // Same order as the enum items listed in the Avalonia ComboBox.
        DoubleClickModeCombo.ItemsSource = new[]
        {
            DoubleClickZoomMode.ZoomIn,
            DoubleClickZoomMode.ZoomOut,
            DoubleClickZoomMode.ZoomInOut,
            DoubleClickZoomMode.ZoomToFit,
            DoubleClickZoomMode.None
        };

        DataContext = ZoomBorder;
    }
}
