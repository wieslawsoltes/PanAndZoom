// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.Views;

public sealed partial class DynamicZoomLimitsView : UserControl
{
    public DynamicZoomLimitsView()
    {
        InitializeComponent();
        DataContext = ZoomBorder;
    }
}
