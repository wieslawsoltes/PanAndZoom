// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Xaml;
using Uno.UI.RuntimeTests;

namespace PanAndZoom.Uno.RuntimeTests;

/// <summary>
/// Host application for the runtime tests (Uno Platform, and native WinUI through
/// tests/PanAndZoom.WinUI.RuntimeTests). When started without the runtime test environment
/// variables the interactive test runner UI is shown.
/// </summary>
public partial class App : Application
{
    public App()
    {
#if PANANDZOOM_WINUI
        WinUITestRunner.Initialize(this);
#endif
        InitializeComponent();
    }

    public static Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window
        {
            Title = "PanAndZoom Runtime Tests"
        };

#if PANANDZOOM_WINUI
        try
        {
#endif
            var testsControl = new UnitTestsControl();
            MainWindow.Content = testsControl;
            MainWindow.Activate();

#if PANANDZOOM_WINUI
            WinUITestRunner.StartIfConfigured(MainWindow, testsControl);
        }
        catch (Exception ex)
        {
            WinUITestRunner.Log($"OnLaunched failed: {ex}");
            throw;
        }
#endif
    }
}
