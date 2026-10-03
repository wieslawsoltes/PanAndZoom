// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
#if !PANANDZOOM_WINUI
global using Microsoft.Extensions.Logging;
#endif
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using PanAndZoom;
global using Point = Windows.Foundation.Point;
global using Rect = Windows.Foundation.Rect;
global using Size = Windows.Foundation.Size;
