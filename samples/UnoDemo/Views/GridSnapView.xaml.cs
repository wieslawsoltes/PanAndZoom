// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using UnoDemo.ViewModels;

namespace UnoDemo.Views;

public sealed partial class GridSnapView : UserControl
{
    private bool _initialized;

    private GridSnapViewModel? ViewModel => DataContext as GridSnapViewModel;

    public GridSnapView()
    {
        InitializeComponent();

        DataContext = new GridSnapViewModel();

        // The Avalonia demo binds the settings to the view model DataContext (where they do not exist);
        // here they are bound to the ZoomBorder, and snapping is refreshed whenever they change.
        ZoomBorder.RegisterPropertyChangedCallback(ZoomBorder.GridSizeProperty, (_, _) => ViewModel?.UpdateSnapping());
        ZoomBorder.RegisterPropertyChangedCallback(ZoomBorder.EnableSnapToGridProperty, (_, _) => ViewModel?.UpdateSnapping());

        Loaded += (s, e) =>
        {
            if (ViewModel != null && !_initialized)
            {
                _initialized = true;
                ViewModel.ZoomBorder = ZoomBorder;
                ViewModel.UpdateSnapping();
            }
        };
    }

    private void UpdateSnapping_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.UpdateSnapping();
    }
}
