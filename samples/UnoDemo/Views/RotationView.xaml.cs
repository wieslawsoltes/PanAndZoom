// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using UnoDemo.ViewModels;

namespace UnoDemo.Views;

public sealed partial class RotationView : UserControl
{
    private bool _initialized;

    public RotationViewModel ViewModel { get; } = new();

    public RotationView()
    {
        InitializeComponent();

        DataContext = ViewModel;

        Loaded += (s, e) =>
        {
            if (!_initialized)
            {
                _initialized = true;
                ViewModel.ZoomBorder = ZoomBorder;
                ViewModel.UpdateRotation();

                // Avalonia: ZoomBorder.PropertyChanged filtered on the Rotation property.
                ZoomBorder.RegisterPropertyChangedCallback(ZoomBorder.RotationProperty, (_, _) => ViewModel.UpdateRotation());
            }
        };
    }

    private void Rotate15_Click(object sender, RoutedEventArgs e) => ViewModel.RotateBy(15);
    private void RotateMinus15_Click(object sender, RoutedEventArgs e) => ViewModel.RotateBy(-15);
    private void Rotate45_Click(object sender, RoutedEventArgs e) => ViewModel.RotateBy(45);
    private void RotateMinus45_Click(object sender, RoutedEventArgs e) => ViewModel.RotateBy(-45);
    private void Snap_Click(object sender, RoutedEventArgs e) => ViewModel.Snap();
    private void Reset_Click(object sender, RoutedEventArgs e) => ViewModel.Reset();
    private void UpdateRotation_Click(object sender, RoutedEventArgs e) => ViewModel.UpdateRotation();
}
