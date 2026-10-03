// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using UnoDemo.ViewModels;

namespace UnoDemo.Views;

public sealed partial class BoundsCallbacksView : UserControl
{
    private bool _initialized;

    private BoundsCallbacksViewModel? ViewModel => DataContext as BoundsCallbacksViewModel;

    public BoundsCallbacksView()
    {
        InitializeComponent();

        DataContext = new BoundsCallbacksViewModel();

        Loaded += (s, e) =>
        {
            if (ViewModel != null && !_initialized)
            {
                _initialized = true;
                ViewModel.ZoomBorder = ZoomBorder;
                ViewModel.UpdateBounds();

                // Subscribe to events
                ZoomBorder.ZoomChanged += (sender, args) =>
                {
                    ViewModel.LogEvent($"ZoomChanged: Zoom=({args.ZoomX:F2}, {args.ZoomY:F2})");
                    ViewModel.UpdateBounds();
                };

                ZoomBorder.PanStarted += (sender, args) =>
                    ViewModel.LogEvent($"PanStarted: Offset=({args.OffsetX:F2}, {args.OffsetY:F2})");

                ZoomBorder.PanEnded += (sender, args) =>
                    ViewModel.LogEvent($"PanEnded: Offset=({args.OffsetX:F2}, {args.OffsetY:F2})");

                ZoomBorder.ZoomStarted += (sender, args) =>
                    ViewModel.LogEvent($"ZoomStarted: Zoom=({args.ZoomX:F2}, {args.ZoomY:F2})");

                ZoomBorder.ZoomEnded += (sender, args) =>
                    ViewModel.LogEvent($"ZoomEnded: Zoom=({args.ZoomX:F2}, {args.ZoomY:F2})");
            }
        };
    }

    private void RefreshBounds_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.UpdateBounds();
    }
}
