// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using UnoDemo.ViewModels;

namespace UnoDemo.Views;

public sealed partial class VisibilityChecksView : UserControl
{
    private VisibilityChecksViewModel? ViewModel => DataContext as VisibilityChecksViewModel;

    public VisibilityChecksView()
    {
        InitializeComponent();
        DataContext = new VisibilityChecksViewModel();

        Loaded += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.ZoomBorder = ZoomBorder;
            }
        };
    }

    private void CheckPoint_Click(object sender, RoutedEventArgs e) => ViewModel?.CheckPointVisibility();
    private void CheckRect_Click(object sender, RoutedEventArgs e) => ViewModel?.CheckRectVisibility();
    private void GetPortion_Click(object sender, RoutedEventArgs e) => ViewModel?.GetVisiblePortion();
}
