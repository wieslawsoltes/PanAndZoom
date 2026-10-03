// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using UnoDemo.Views;

namespace UnoDemo;

/// <summary>
/// Main page hosting all demos (the Uno equivalent of the Avalonia <c>MainView</c> tab control).
/// </summary>
public sealed partial class MainPage : Page
{
    /// <summary>
    /// Gets the demos in the same order as the tabs of the Avalonia demo.
    /// </summary>
    public static IReadOnlyList<DemoInfo> Demos { get; } =
    [
        new("Commands", () => new CommandsView()),
        new("Coordinate Conversion", () => new CoordinateConversionView()),
        new("Bounds & Callbacks", () => new BoundsCallbacksView()),
        new("Keyboard & Reset", () => new KeyboardResetView()),
        new("Zoom-to-Rectangle", () => new ZoomToRectView()),
        new("Inertia & Gestures", () => new InertiaGesturesView()),
        new("View History", () => new ViewHistoryView()),
        new("Dynamic Zoom Limits", () => new DynamicZoomLimitsView()),
        new("Grid & Snap", () => new GridSnapView()),
        new("Rotation", () => new RotationView()),
        new("Accessibility", () => new AccessibilityView()),
        new("State Serialization", () => new StateSerializationView()),
        new("Discrete Zoom", () => new DiscreteZoomView()),
        new("Double Click Zoom", () => new DoubleClickZoomView()),
        new("Mouse Wheel", () => new WheelBehaviorView()),
        new("Bounds Mode", () => new BoundsModeView()),
        new("Saved Views", () => new SavedViewsView()),
        new("Advanced Zoom", () => new AdvancedZoomView()),
        new("Center Controls", () => new CenterControlsView()),
        new("Programmatic Pan", () => new ProgrammaticPanView()),
        new("Animation", () => new AnimationView()),
        new("Visibility Checks", () => new VisibilityChecksView()),
        new("Resize Behavior", () => new ResizeBehaviorView()),
        new("Gesture Fine Control", () => new GestureFineControlView()),
        new("Basic Demo", () => new BasicDemoView()),
    ];

    // Views are created on first selection and kept alive afterwards, like the Avalonia TabControl
    // keeps the tab content, so the demo state survives switching between demos.
    private readonly Dictionary<DemoInfo, FrameworkElement> _views = new();

    public MainPage()
    {
        InitializeComponent();

        foreach (var demo in Demos)
        {
            Navigation.MenuItems.Add(new NavigationViewItem
            {
                Content = demo.Name,
                Tag = demo
            });
        }

        Navigation.SelectedItem = Navigation.MenuItems[0];
    }

    /// <summary>
    /// Selects the demo at the specified index and returns its view.
    /// </summary>
    /// <param name="index">The demo index in <see cref="Demos"/>.</param>
    /// <returns>The demo view.</returns>
    public FrameworkElement ShowDemo(int index)
    {
        var item = Navigation.MenuItems[index];
        if (!ReferenceEquals(Navigation.SelectedItem, item))
        {
            Navigation.SelectedItem = item;
        }

        return GetOrCreateView(Demos[index]);
    }

    private FrameworkElement GetOrCreateView(DemoInfo demo)
    {
        if (!_views.TryGetValue(demo, out var view))
        {
            view = demo.CreateView();
            _views[demo] = view;
        }

        if (!ReferenceEquals(DemoHost.Content, view))
        {
            DemoHost.Content = view;
        }

        return view;
    }

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: DemoInfo demo })
        {
            GetOrCreateView(demo);
        }
    }
}

/// <summary>
/// Describes a demo page.
/// </summary>
/// <param name="Name">The demo name (the Avalonia tab header).</param>
/// <param name="CreateView">Creates the demo view.</param>
public sealed record DemoInfo(string Name, Func<FrameworkElement> CreateView);
