using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;

namespace AvaloniaDemo;

public partial class MainView : UserControl
{
    private const string RedImageBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAIAAAACUFjqAAAAEklEQVR4nGP8z4APMOGVHbHSAEEsAROxCnMTAAAAAElFTkSuQmCC";
    private const string BlueImageBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAIAAAACUFjqAAAAFElEQVR4nGNkYPjPgBsw4ZEbwdIAPy4BE1xg8ZcAAAAASUVORK5CYII=";

    private bool _toggle;
    public MainView()
    {
        InitializeComponent();

        if (ZoomBorder1 != null)
        {
            ZoomBorder1.KeyDown += ZoomBorder_KeyDown;
            ZoomBorder1.ZoomChanged += ZoomBorder_ZoomChanged;
        }

        if (ZoomBorder2 != null)
        {
            ZoomBorder2.KeyDown += ZoomBorder_KeyDown;
            ZoomBorder2.ZoomChanged += ZoomBorder_ZoomChanged;
        }

        if (ZoomBorder3 != null)
        {
            ZoomBorder3.KeyDown += ZoomBorder_KeyDown;
            ZoomBorder3.ZoomChanged += ZoomBorder_ZoomChanged;
        }

        if (SwapImage != null)
        {
            SwapImage.Source = LoadBitmap(RedImageBase64);
        }

        DataContext = ZoomBorder1;
    }

    private void ZoomBorder_KeyDown(object? sender, KeyEventArgs e)
    {
        var zoomBorder = this.DataContext as ZoomBorder;
            
        switch (e.Key)
        {
            case Key.F:
                zoomBorder?.Fill();
                break;
            case Key.U:
                zoomBorder?.Uniform();
                break;
            case Key.R:
                zoomBorder?.ResetMatrix();
                break;
            case Key.T:
                zoomBorder?.ToggleStretchMode();
                zoomBorder?.AutoFit();
                break;
        }
    }

    private void ZoomBorder_ZoomChanged(object sender, ZoomChangedEventArgs e)
    {
        Debug.WriteLine($"[ZoomChanged] {e.ZoomX} {e.ZoomY} {e.OffsetX} {e.OffsetY}");
    }

    private void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tabControl)
        {
            if (tabControl.SelectedItem is TabItem tabItem)
            {
                if (tabItem.Tag is string tag)
                {
                    if (tag == "1")
                    {
                        DataContext = ZoomBorder1;
                    }
                    else if (tag == "2")
                    {
                        DataContext = ZoomBorder2;
                    }
                    else if (tag == "3")
                    {
                        DataContext = ZoomBorder3;
                    }
                }
            }
        }
    }

    private void SwapImage_Click(object? sender, RoutedEventArgs e)
    {
        if (SwapImage == null)
            return;

        if (SwapImage.Source is IDisposable disposable)
        {
            disposable.Dispose();
        }

        SwapImage.Source = LoadBitmap(_toggle ? BlueImageBase64 : RedImageBase64);
        _toggle = !_toggle;
    }

    private static Bitmap LoadBitmap(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        return new Bitmap(new MemoryStream(bytes));
    }
}

