// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using UnoDemo.ViewModels;

namespace UnoDemo.Views;

public sealed partial class SavedViewsView : UserControl
{
    private bool _initialized;

    private SavedViewsViewModel? ViewModel => DataContext as SavedViewsViewModel;

    public SavedViewsView()
    {
        InitializeComponent();

        DataContext = new SavedViewsViewModel();

        Loaded += (s, e) =>
        {
            if (ViewModel != null && !_initialized)
            {
                _initialized = true;
                ViewModel.ZoomBorder = ZoomBorder;
                ViewModel.RefreshViewList();
            }
        };
    }

    private void SaveView_Click(object sender, RoutedEventArgs e) => ViewModel?.SaveView();
    private void RestoreView_Click(object sender, RoutedEventArgs e) => ViewModel?.RestoreView();
    private void DeleteView_Click(object sender, RoutedEventArgs e) => ViewModel?.DeleteView();
    private void ClearAllViews_Click(object sender, RoutedEventArgs e) => ViewModel?.ClearAllViews();
}
