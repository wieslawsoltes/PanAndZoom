// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using UnoDemo.ViewModels;
using Windows.System;

namespace UnoDemo.Views;

public sealed partial class ViewHistoryView : UserControl
{
    private bool _initialized;

    private ViewHistoryViewModel? ViewModel => DataContext as ViewHistoryViewModel;

    public ViewHistoryView()
    {
        InitializeComponent();

        DataContext = new ViewHistoryViewModel();

        Loaded += (s, e) =>
        {
            if (ViewModel != null && !_initialized)
            {
                _initialized = true;
                ViewModel.ZoomBorder = ZoomBorder;
                ViewModel.UpdateHistoryState();

                ZoomBorder.ZoomChanged += (sender, args) => ViewModel.UpdateHistoryState();
                ZoomBorder.MatrixChanged += (sender, args) => ViewModel.UpdateHistoryState();
            }
        };

        // Add keyboard shortcuts
        ZoomBorder.KeyDown += (s, e) =>
        {
            if (DemoKeyboard.IsControlPressed)
            {
                if (e.Key == VirtualKey.Z)
                {
                    ViewModel?.Undo();
                    e.Handled = true;
                }
                else if (e.Key == VirtualKey.Y)
                {
                    ViewModel?.Redo();
                    e.Handled = true;
                }
            }
        };
    }

    private void Undo_Click(object sender, RoutedEventArgs e) => ViewModel?.Undo();
    private void Redo_Click(object sender, RoutedEventArgs e) => ViewModel?.Redo();
    private void ClearHistory_Click(object sender, RoutedEventArgs e) => ViewModel?.ClearHistory();
}
