$ErrorActionPreference = "Stop"

. ".\include.ps1"

foreach($pkg in $Packages) 
{
    rm -Force -Recurse .\$pkg -ErrorAction SilentlyContinue
}

rm -Force -Recurse *.nupkg -ErrorAction SilentlyContinue
Copy-Item template PanAndZoom -Recurse
sv avalonia "Avalonia.Controls.PanAndZoom\lib\portable-windows8+net45"
sv wpf "Wpf.Controls.PanAndZoom\lib\net45"

mkdir $avalonia -ErrorAction SilentlyContinue
mkdir $wpf -ErrorAction SilentlyContinue

Copy-Item ..\src\Avalonia.Controls.PanAndZoom\bin\Release\Avalonia.Controls.PanAndZoom.dll $avalonia

Copy-Item ..\src\Wpf.Controls.PanAndZoom\bin\Release\Wpf.Controls.PanAndZoom.dll $wpf

foreach($pkg in $Packages)
{
    (gc PanAndZoom\$pkg.nuspec).replace('#VERSION#', $args[0]) | sc $pkg\$pkg.nuspec
}

foreach($pkg in $Packages)
{
    nuget.exe pack $pkg\$pkg.nuspec
}

foreach($pkg in $Packages)
{
    rm -Force -Recurse .\$pkg
}