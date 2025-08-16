using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace AvaloniaDemo;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        ZoomBorder1 = this.Find<ZoomBorder>("ZoomBorder1");
        if (ZoomBorder1 != null)
        {
            ZoomBorder1.KeyDown += ZoomBorder_KeyDown;
            ZoomBorder1.ZoomChanged += ZoomBorder_ZoomChanged;
            
            // Subscribe to new events
            ZoomBorder1.PanStarted += ZoomBorder_PanStarted;
            ZoomBorder1.PanContinued += ZoomBorder_PanContinued;
            ZoomBorder1.PanEnded += ZoomBorder_PanEnded;
            ZoomBorder1.ZoomStarted += ZoomBorder_ZoomStarted;
            ZoomBorder1.ZoomEnded += ZoomBorder_ZoomEnded;
            ZoomBorder1.ZoomDeltaChanged += ZoomBorder_ZoomDeltaChanged;
            ZoomBorder1.MatrixChanged += ZoomBorder_MatrixChanged;
            ZoomBorder1.MatrixReset += ZoomBorder_MatrixReset;
            ZoomBorder1.StretchModeChanged += ZoomBorder_StretchModeChanged;
            ZoomBorder1.AutoFitApplied += ZoomBorder_AutoFitApplied;
            ZoomBorder1.GestureStarted += ZoomBorder_GestureStarted;
            ZoomBorder1.GestureEnded += ZoomBorder_GestureEnded;
        }

        ZoomBorder2 = this.Find<ZoomBorder>("ZoomBorder2");
        if (ZoomBorder2 != null)
        {
            ZoomBorder2.KeyDown += ZoomBorder_KeyDown;
            ZoomBorder2.ZoomChanged += ZoomBorder_ZoomChanged;
            
            // Subscribe to new events
            ZoomBorder2.PanStarted += ZoomBorder_PanStarted;
            ZoomBorder2.PanContinued += ZoomBorder_PanContinued;
            ZoomBorder2.PanEnded += ZoomBorder_PanEnded;
            ZoomBorder2.ZoomStarted += ZoomBorder_ZoomStarted;
            ZoomBorder2.ZoomEnded += ZoomBorder_ZoomEnded;
            ZoomBorder2.ZoomDeltaChanged += ZoomBorder_ZoomDeltaChanged;
            ZoomBorder2.MatrixChanged += ZoomBorder_MatrixChanged;
            ZoomBorder2.MatrixReset += ZoomBorder_MatrixReset;
            ZoomBorder2.StretchModeChanged += ZoomBorder_StretchModeChanged;
            ZoomBorder2.AutoFitApplied += ZoomBorder_AutoFitApplied;
            ZoomBorder2.GestureStarted += ZoomBorder_GestureStarted;
            ZoomBorder2.GestureEnded += ZoomBorder_GestureEnded;
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

    // New event handlers for demonstration
    private void ZoomBorder_PanStarted(object? sender, PanEventArgs e)
    {
        Debug.WriteLine($"[PanStarted] Offset: ({e.OffsetX:F2}, {e.OffsetY:F2}), Delta: ({e.DeltaX:F2}, {e.DeltaY:F2})");
    }

    private void ZoomBorder_PanContinued(object? sender, PanEventArgs e)
    {
        Debug.WriteLine($"[PanContinued] Offset: ({e.OffsetX:F2}, {e.OffsetY:F2}), Delta: ({e.DeltaX:F2}, {e.DeltaY:F2})");
    }

    private void ZoomBorder_PanEnded(object? sender, PanEventArgs e)
    {
        Debug.WriteLine($"[PanEnded] Final Offset: ({e.OffsetX:F2}, {e.OffsetY:F2})");
    }

    private void ZoomBorder_ZoomStarted(object? sender, ZoomEventArgs e)
    {
        Debug.WriteLine($"[ZoomStarted] Zoom: ({e.ZoomX:F2}, {e.ZoomY:F2}), Center: ({e.CenterX:F2}, {e.CenterY:F2}), Delta: {e.ZoomDelta:F2}");
    }

    private void ZoomBorder_ZoomEnded(object? sender, ZoomEventArgs e)
    {
        Debug.WriteLine($"[ZoomEnded] Final Zoom: ({e.ZoomX:F2}, {e.ZoomY:F2}), Delta: {e.ZoomDelta:F2}");
    }

    private void ZoomBorder_ZoomDeltaChanged(object? sender, ZoomEventArgs e)
    {
        Debug.WriteLine($"[ZoomDeltaChanged] Zoom: ({e.ZoomX:F2}, {e.ZoomY:F2}), Delta: {e.ZoomDelta:F2}");
    }

    private void ZoomBorder_MatrixChanged(object? sender, MatrixChangedEventArgs e)
    {
        Debug.WriteLine($"[MatrixChanged] New Matrix: [{e.Matrix.M11:F2}, {e.Matrix.M12:F2}, {e.Matrix.M21:F2}, {e.Matrix.M22:F2}, {e.Matrix.M31:F2}, {e.Matrix.M32:F2}]");
    }

    private void ZoomBorder_MatrixReset(object? sender, MatrixChangedEventArgs e)
    {
        Debug.WriteLine($"[MatrixReset] Reset to identity matrix");
    }

    private void ZoomBorder_StretchModeChanged(object? sender, StretchModeChangedEventArgs e)
    {
        Debug.WriteLine($"[StretchModeChanged] From {e.PreviousStretchMode} to {e.StretchMode}");
    }

    private void ZoomBorder_AutoFitApplied(object? sender, StretchModeChangedEventArgs e)
    {
        Debug.WriteLine($"[AutoFitApplied] Applied {e.StretchMode} stretch mode");
    }

    private void ZoomBorder_GestureStarted(object? sender, GestureEventArgs e)
    {
        Debug.WriteLine($"[GestureStarted] Type: {e.GestureType}, Center: ({e.CenterX:F2}, {e.CenterY:F2}), Delta: {e.Delta:F2}");
    }

    private void ZoomBorder_GestureEnded(object? sender, GestureEventArgs e)
    {
        Debug.WriteLine($"[GestureEnded] Type: {e.GestureType}");
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
                }
            }
        }
    }
}

