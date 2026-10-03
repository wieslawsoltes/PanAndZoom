// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.System;

namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

/// <summary>
/// Sends mouse events through the Uno macOS (Skia) host native callback
/// (<c>MacOSWindowHost.OnMouseEvent</c>) exactly like <c>libUnoNativeMac</c> does for real
/// AppKit events. This exercises the host conversion of native button masks, which the
/// <see cref="Windows.UI.Input.Preview.Injection.InputInjector"/> bypasses.
/// </summary>
public static unsafe class MacOSNativeMouse
{
    /// <summary>NSEvent.pressedMouseButtons masks.</summary>
    public const uint LeftButton = 1;
    public const uint RightButton = 2;
    public const uint MiddleButton = 4;

    private const int Down = 3;
    private const int Up = 4;
    private const int Moved = 5;

    private static readonly Type? s_hostType = AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType("Uno.UI.Runtime.Skia.MacOS.MacOSWindowHost", throwOnError: false))
        .FirstOrDefault(t => t != null);

    private static uint s_frameId = 1;

    /// <summary>
    /// Gets a value indicating whether the app runs on the Uno macOS Skia host.
    /// </summary>
    public static bool IsAvailable => s_hostType != null && GetHandle() != 0;

    /// <summary>Presses the buttons (native mask) at a window position.</summary>
    public static void Press(Point position, uint buttons) => Send(Down, position, buttons);

    /// <summary>Moves the mouse while the buttons (native mask) are pressed.</summary>
    public static void Move(Point position, uint buttons) => Send(Moved, position, buttons);

    /// <summary>Releases all buttons at a window position.</summary>
    public static void Release(Point position) => Send(Up, position, 0);

    /// <summary>Drags with the provided buttons (native mask) from one window position to another.</summary>
    public static void Drag(Point from, Point to, uint buttons, int steps = 10)
    {
        Send(Moved, from, 0);
        Press(from, buttons);
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (double)steps;
            Move(new Point(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t), buttons);
        }

        Release(to);
    }

    private static void Send(int eventType, Point position, uint buttons)
    {
        var handle = GetHandle();
        var method = s_hostType!.GetMethod("OnMouseEvent", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new NotSupportedException("MacOSWindowHost.OnMouseEvent was not found.");

        var data = new NativeMouseEventData
        {
            EventType = eventType,
            X = position.X,
            Y = position.Y,
            InContact = buttons != 0 ? 1 : 0,
            MouseButtons = buttons,
            KeyModifiers = VirtualKeyModifiers.None,
            PointerDeviceType = 2, // Microsoft.UI.Input.PointerDeviceType.Mouse
            FrameId = s_frameId++,
            Timestamp = (ulong)Environment.TickCount64 * 1000,
            Pid = 1
        };

        var callback = (delegate* unmanaged[Cdecl]<nint, NativeMouseEventData*, int>)method.MethodHandle.GetFunctionPointer();
        callback(handle, &data);
    }

    private static nint GetHandle()
    {
        var windows = s_hostType?.GetField("_windows", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as IDictionary;
        if (windows == null)
        {
            return 0;
        }

        foreach (DictionaryEntry entry in windows)
        {
            return (nint)entry.Key;
        }

        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMouseEventData
    {
        public int EventType;
        public double X;
        public double Y;
        public int InContact;
        public uint MouseButtons;
        public float TiltX;
        public float TiltY;
        public float Pressure;
        public int ScrollingDeltaX;
        public int ScrollingDeltaY;
        public VirtualKeyModifiers KeyModifiers;
        public int PointerDeviceType;
        public uint FrameId;
        public ulong Timestamp;
        public uint Pid;
    }
}
