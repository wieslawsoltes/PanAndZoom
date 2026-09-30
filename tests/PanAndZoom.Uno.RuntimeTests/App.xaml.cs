// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Xaml;
using Uno.UI.RuntimeTests;

namespace PanAndZoom.Uno.RuntimeTests;

/// <summary>
/// Host application for the runtime tests. When started without the runtime test environment
/// variables the interactive test runner UI is shown.
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    public static Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window
        {
            Title = "PanAndZoom Uno Runtime Tests"
        };

        MainWindow.Content = new UnitTestsControl();
        MainWindow.Activate();
    }
}
