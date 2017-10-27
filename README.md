# PanAndZoom

[![Gitter](https://badges.gitter.im/wieslawsoltes/PanAndZoom.svg)](https://gitter.im/wieslawsoltes/PanAndZoom?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

[![Build status](https://ci.appveyor.com/api/projects/status/mbwd4i983lkc15c0/branch/master?svg=true)](https://ci.appveyor.com/project/wieslawsoltes/panandzoom/branch/master)
[![Build Status](https://travis-ci.org/wieslawsoltes/PanAndZoom.svg?branch=master)](https://travis-ci.org/wieslawsoltes/PanAndZoom)
[![CircleCI](https://circleci.com/gh/wieslawsoltes/PanAndZoom/tree/master.svg?style=svg)](https://circleci.com/gh/wieslawsoltes/PanAndZoom/tree/master)

[![NuGet](https://img.shields.io/nuget/v/Avalonia.Controls.PanAndZoom.svg)](https://www.nuget.org/packages/Avalonia.Controls.PanAndZoom)
[![MyGet](https://img.shields.io/myget/panandzoom-nightly/vpre/Avalonia.Controls.PanAndZoom.svg?label=myget)](https://www.myget.org/gallery/panandzoom-nightly) 

[![CodeFactor](https://www.codefactor.io/repository/github/wieslawsoltes/panandzoom/badge)](https://www.codefactor.io/repository/github/wieslawsoltes/panandzoom)

PanAndZoom control for WPF and Avalonia

<a href='https://www.youtube.com/watch?v=dM_cRdEuksU' target='_blank'>![](https://i.ytimg.com/vi/dM_cRdEuksU/hqdefault.jpg)<a/>

## NuGet

PanAndZoom is delivered as a NuGet package.

You can find the packages here [NuGet for Avalonia](https://www.nuget.org/packages/Avalonia.Controls.PanAndZoom/) and here [NuGet for WPF](https://www.nuget.org/packages/Wpf.Controls.PanAndZoom/) or by using nightly build feed:
* Add `https://www.myget.org/F/panandzoom-nightly/api/v2` to your package sources
* Update your package using `PanAndZoom` feed

You can install the package for `Avalonia` based projects like this:

`Install-Package Avalonia.Controls.PanAndZoom -Pre`

You can install the package for `WPF` based projects like this:

`Install-Package Wpf.Controls.PanAndZoom -Pre`

### Package Dependencies

* [Avalonia](https://www.nuget.org/packages/Avalonia/)
* [System.Reactive](https://www.nuget.org/packages/System.Reactive/)
* [System.Reactive.Core](https://www.nuget.org/packages/System.Reactive.Core/)
* [System.Reactive.Interfaces](https://www.nuget.org/packages/System.Reactive.Interfaces/)
* [System.Reactive.Linq](https://www.nuget.org/packages/System.Reactive.Linq/)
* [System.Reactive.PlatformServices](https://www.nuget.org/packages/System.Reactive.PlatformServices/)
* [Serilog](https://www.nuget.org/packages/Serilog/)
* [Splat](https://www.nuget.org/packages/Splat/)
* [Sprache](https://www.nuget.org/packages/Sprache/)
* [System.ValueTuple](https://www.nuget.org/packages/System.ValueTuple/)

### Package Sources

* https://api.nuget.org/v3/index.json
* https://www.myget.org/F/panandzoom-nightly/api/v2

## Resources

* [GitHub source code repository.](https://github.com/wieslawsoltes/PanAndZoom)

## Using PanAndZoom

### Avalonia

`MainWindow.xaml`
```XAML
<Window x:Class="AvaloniaDemo.MainWindow"
        xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:paz="clr-namespace:Avalonia.Controls.PanAndZoom;assembly=Avalonia.Controls.PanAndZoom"
        UseLayoutRounding="True"
        Title="PanAndZoom Avalonia Demo" Height="640" Width="640">
    <Grid RowDefinitions="Auto,12,Auto,12,*,12" ColumnDefinitions="50,*,50">
        <StackPanel Orientation="Vertical" 
                    HorizontalAlignment="Center" Grid.Row="0" Grid.Column="1">
            <TextBlock Text="F - Fill"/>
            <TextBlock Text="U - Uniform"/>
            <TextBlock Text="R - Reset"/>
            <TextBlock Text="T - Toggle Stretch Mode"/>
            <TextBlock Text="Mouse Wheel - Zoom to Point"/>
            <TextBlock Text="Mouse Left Button Down - Pan"/>
        </StackPanel>
        <StackPanel Orientation="Horizontal" 
                    HorizontalAlignment="Center" Grid.Row="2" Grid.Column="1">
            <TextBlock Text="PanButton:" VerticalAlignment="Center"/>
            <DropDown Items="{x:Static paz:ZoomBorder.ButtonNames}" 
                      SelectedItem="{Binding #zoomBorder.PanButton, Mode=TwoWay}" 
                      Margin="2">
            </DropDown>
            <TextBlock Text="Stretch:" VerticalAlignment="Center"/>
            <DropDown Items="{x:Static paz:ZoomBorder.StretchModes}" 
                      SelectedItem="{Binding #zoomBorder.Stretch, Mode=TwoWay}" 
                      Margin="2">
            </DropDown>
            <TextBlock Text="ZoomSpeed:" VerticalAlignment="Center"/>
            <TextBox Text="{Binding #zoomBorder.ZoomSpeed, Mode=TwoWay}" 
                     TextAlignment="Center" Width="50" Margin="2"/>
            <CheckBox IsChecked="{Binding #zoomBorder.EnableInput}" 
                      Content="EnableInput" VerticalAlignment="Center"/>
        </StackPanel>
        <paz:ZoomBorder Name="zoomBorder" Stretch="None" ZoomSpeed="1.2" 
                        Background="SlateBlue" ClipToBounds="True" Focusable="True"
                        VerticalAlignment="Stretch" HorizontalAlignment="Stretch" 
                        Grid.Row="4" Grid.Column="1">
            <Canvas Background="LightGray" Width="300" Height="300">
                <Rectangle Canvas.Left="100" Canvas.Top="100" Width="50" Height="50" Fill="Red"/>
                <StackPanel Canvas.Left="100" Canvas.Top="200">
                    <TextBlock Text="Text1" Width="100" Background="Red" Foreground="WhiteSmoke"/>
                    <TextBlock Text="Text2" Width="100" Background="Red" Foreground="WhiteSmoke"/>
                </StackPanel>
            </Canvas>
        </paz:ZoomBorder>
    </Grid>
</Window>
```

`MainWindow.xaml.cs`
```C#
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace AvaloniaDemo
{
    public class MainWindow : Window
    {
        private ZoomBorder zoomBorder;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();

            zoomBorder = this.Find<ZoomBorder>("zoomBorder");
            zoomBorder.KeyDown += ZoomBorder_KeyDown;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ZoomBorder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F)
            {
                zoomBorder.Fill();
            }

            if (e.Key == Key.U)
            {
                zoomBorder.Uniform();
            }

            if (e.Key == Key.R)
            {
                zoomBorder.Reset();
            }

            if (e.Key == Key.T)
            {
                zoomBorder.ToggleStretchMode();
                zoomBorder.AutoFit();
            }
        }
    }
}
```

### WPF

`MainWindow.xaml`
```XAML
<Window x:Class="WpfDemo.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:paz="clr-namespace:Wpf.Controls.PanAndZoom;assembly=Wpf.Controls.PanAndZoom"
        WindowStartupLocation="CenterScreen"
        UseLayoutRounding="True" SnapsToDevicePixels="True" TextOptions.TextFormattingMode="Display"
        Title="PanAndZoom WPF Demo" Height="640" Width="640">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="12"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="12"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="12"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="50"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="50"/>
        </Grid.ColumnDefinitions>
        <StackPanel Orientation="Vertical" 
                    HorizontalAlignment="Center" Grid.Row="0" Grid.Column="1">
            <TextBlock Text="F - Fill"/>
            <TextBlock Text="U - Uniform"/>
            <TextBlock Text="R - Reset"/>
            <TextBlock Text="T - Toggle Stretch Mode"/>
            <TextBlock Text="Mouse Wheel - Zoom to Point"/>
            <TextBlock Text="Mouse Left Button Down - Pan"/>
        </StackPanel>
        <StackPanel Orientation="Horizontal" 
                    HorizontalAlignment="Center" Grid.Row="2" Grid.Column="1">
            <TextBlock Text="PanButton:" VerticalAlignment="Center"/>
            <ComboBox ItemsSource="{Binding ElementName=zoomBorder, Path=ButtonNames}" 
                      SelectedItem="{Binding ElementName=zoomBorder, Path=PanButton}" 
                      Margin="2">
            </ComboBox>
            <TextBlock Text="Stretch:" VerticalAlignment="Center"/>
            <ComboBox ItemsSource="{Binding ElementName=zoomBorder, Path=StretchModes}" 
                      SelectedItem="{Binding ElementName=zoomBorder, Path=Stretch}" 
                      Margin="2">
            </ComboBox>
            <TextBlock Text="ZoomSpeed:" VerticalAlignment="Center"/>
            <TextBox Text="{Binding ElementName=zoomBorder, Path=ZoomSpeed}" 
                     TextAlignment="Center" Width="50" Margin="2"/>
            <CheckBox IsChecked="{Binding ElementName=zoomBorder, Path=EnableInput}" 
                      Content="EnableInput" VerticalAlignment="Center"/>
        </StackPanel>
        <paz:ZoomBorder Name="zoomBorder" Stretch="None" ZoomSpeed="1.2" 
                        Background="SlateBlue" ClipToBounds="True" Focusable="True"
                        VerticalAlignment="Stretch" HorizontalAlignment="Stretch" 
                        Grid.Row="4" Grid.Column="1">
            <Canvas Background="LightGray" Width="300" Height="300">
                <Rectangle Canvas.Left="100" Canvas.Top="100" Width="50" Height="50" Fill="Red"/>
                <StackPanel Canvas.Left="100" Canvas.Top="200">
                    <TextBlock Text="Text1" Width="100" Background="Red" Foreground="WhiteSmoke"/>
                    <TextBlock Text="Text2" Width="100" Background="Red" Foreground="WhiteSmoke"/>
                </StackPanel>
            </Canvas>
        </paz:ZoomBorder>
    </Grid>
</Window>
```

`MainWindow.xaml.cs`
```C#
using System.Windows;
using System.Windows.Input;

namespace WpfDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            zoomBorder.KeyDown += ZoomBorder_KeyDown;
        }

        private void ZoomBorder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F)
            {
                zoomBorder.Fill();
            }

            if (e.Key == Key.U)
            {
                zoomBorder.Uniform();
            }

            if (e.Key == Key.R)
            {
                zoomBorder.Reset();
            }

            if (e.Key == Key.T)
            {
                zoomBorder.ToggleStretchMode();
                zoomBorder.AutoFit();
            }
        }
    }
}
```
### Getting zoom ratio

To get current zoom ratio use `ZoomX` and `ZoomY` properties. 

### Getting pan offset

To get current pan offset use `OffsetX` and `OffsetY` properties. 

### Constrain zoom ratio

To constrain zoom ratio use `MinZoomX`, `MaxZoomX`, `MinZoomY` and `MaxZoomY` properties. 

### Constrain pan offset

To constrain pan offset use `MinOffsetX`, `MaxOffsetX`, `MinOffsetY` and `MaxOffsetY` properties. 

### Enable or disable constrains

To enable or disable contrains use `EnableConstrains` flag.

## License

PanAndZoom is licensed under the [MIT license](LICENSE.TXT).