// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Android.App;
using Android.OS;
using Android.Views;

namespace UnoDemo.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);
    }
}
