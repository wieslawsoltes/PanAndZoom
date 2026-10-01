// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using UnoDemo.ViewModels;

namespace UnoDemo.Views;

public sealed partial class AccessibilityView : UserControl
{
    private bool _initialized;

    private AccessibilityViewModel? ViewModel => DataContext as AccessibilityViewModel;

    public AccessibilityView()
    {
        InitializeComponent();

        DataContext = new AccessibilityViewModel();

        Loaded += (s, e) =>
        {
            if (ViewModel != null && !_initialized)
            {
                _initialized = true;
                ViewModel.ZoomBorder = ZoomBorder;
                ViewModel.UpdateDescriptions();

                ZoomBorder.ZoomChanged += (sender, args) => ViewModel.UpdateDescriptions();
            }
        };
    }

    private void UpdateDescriptions_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.UpdateDescriptions();
    }
}
