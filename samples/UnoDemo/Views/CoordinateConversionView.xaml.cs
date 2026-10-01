// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using UnoDemo.ViewModels;

namespace UnoDemo.Views;

public sealed partial class CoordinateConversionView : UserControl
{
    private bool _initialized;

    private CoordinateConversionViewModel? ViewModel => DataContext as CoordinateConversionViewModel;

    public CoordinateConversionView()
    {
        InitializeComponent();

        DataContext = new CoordinateConversionViewModel();

        Loaded += (s, e) =>
        {
            if (ViewModel != null && !_initialized)
            {
                _initialized = true;
                ViewModel.ZoomBorder = ZoomBorder;
                ViewModel.UpdateConversions();

                ZoomBorder.ZoomChanged += (sender, args) => ViewModel.UpdateConversions();
            }
        };
    }

    private void UpdateConversions_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.UpdateConversions();
    }
}
