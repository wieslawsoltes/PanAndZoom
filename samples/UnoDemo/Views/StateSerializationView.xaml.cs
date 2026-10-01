// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using UnoDemo.ViewModels;

namespace UnoDemo.Views;

public sealed partial class StateSerializationView : UserControl
{
    private StateSerializationViewModel? ViewModel => DataContext as StateSerializationViewModel;

    public StateSerializationView()
    {
        InitializeComponent();

        DataContext = new StateSerializationViewModel();

        Loaded += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.ZoomBorder = ZoomBorder;
            }
        };
    }

    private void Export_Click(object sender, RoutedEventArgs e) => ViewModel?.ExportState();
    private void Import_Click(object sender, RoutedEventArgs e) => ViewModel?.ImportState();
    private void Reset_Click(object sender, RoutedEventArgs e) => ViewModel?.ResetState();
}
