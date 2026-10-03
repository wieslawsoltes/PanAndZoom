// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.Views;

public sealed partial class InertiaGesturesView : UserControl
{
    public InertiaGesturesView()
    {
        InitializeComponent();
        DataContext = ZoomBorder;
    }
}
