// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Reflection;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Pointer = Avalonia.Input.Pointer;

namespace Avalonia.HeadlessTestingFramework;

/// <summary>
/// Helper class for triggering gesture recognizers in headless tests.
/// This class creates persistent Pointer instances that can be tracked by gesture recognizers.
/// Uses reflection to access internal Avalonia APIs for proper gesture recognizer integration.
/// Based on Avalonia's TouchTestHelper pattern.
/// </summary>
/// <remarks>
/// <para>
/// <strong>CRITICAL:</strong> When testing gesture events (Pinch, Scroll, etc.), 
/// handlers must be registered BEFORE calling <c>window.Show()</c>.
/// </para>
/// <example>
/// <code>
/// // CORRECT pattern:
/// var control = new MyControl();
/// control.AddHandler(InputElement.PinchEvent, handler);  // Register FIRST
/// var window = new Window { Content = control };
/// window.Show();  // Show AFTER
/// 
/// // WRONG pattern (events won't fire):
/// var window = new Window { Content = control };
/// window.Show();  // Show FIRST
/// control.AddHandler(InputElement.PinchEvent, handler);  // Too late!
/// </code>
/// </example>
/// </remarks>
public class GestureRecognizerTestHelper
{
    private readonly Pointer _pointer;
    private ulong _nextStamp = 1;
    
    // Cached reflection members for performance
    private static readonly PropertyInfo? s_capturedGestureRecognizerProperty;
    private static readonly MethodInfo? s_captureGestureRecognizerMethod;
    private static readonly MethodInfo? s_pointerMovedInternalMethod;
    private static readonly MethodInfo? s_pointerReleasedInternalMethod;
    private static readonly PropertyInfo? s_gestureRecognizerTargetProperty;
    
    static GestureRecognizerTestHelper()
    {
        // Cache reflection lookups
        var pointerType = typeof(Pointer);
        s_capturedGestureRecognizerProperty = pointerType.GetProperty(
            "CapturedGestureRecognizer", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        s_captureGestureRecognizerMethod = pointerType.GetMethod(
            "CaptureGestureRecognizer", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        
        var gestureRecognizerType = typeof(GestureRecognizer);
        s_pointerMovedInternalMethod = gestureRecognizerType.GetMethod(
            "PointerMovedInternal", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        s_pointerReleasedInternalMethod = gestureRecognizerType.GetMethod(
            "PointerReleasedInternal", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        s_gestureRecognizerTargetProperty = gestureRecognizerType.GetProperty(
            "Target",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    /// <summary>
    /// Gets the pointer associated with this helper.
    /// </summary>
    public Pointer Pointer => _pointer;

    /// <summary>
    /// Gets the currently captured input element.
    /// </summary>
    public IInputElement? Captured => _pointer.Captured;

    /// <summary>
    /// Gets the currently captured gesture recognizer (via reflection).
    /// </summary>
    public GestureRecognizer? CapturedGestureRecognizer
    {
        get
        {
            if (s_capturedGestureRecognizerProperty == null) return null;
            return s_capturedGestureRecognizerProperty.GetValue(_pointer) as GestureRecognizer;
        }
    }

    /// <summary>
    /// Creates a new gesture recognizer test helper with a touch pointer.
    /// </summary>
    public GestureRecognizerTestHelper()
    {
        _pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
    }

    /// <summary>
    /// Creates a new gesture recognizer test helper with a specified pointer type.
    /// </summary>
    /// <param name="pointerType">The type of pointer to create.</param>
    public GestureRecognizerTestHelper(PointerType pointerType)
    {
        _pointer = new Pointer(Pointer.GetNextFreeId(), pointerType, true);
    }

    private ulong Timestamp() => _nextStamp++;

    /// <summary>
    /// Simulates a pointer down event at the specified position.
    /// </summary>
    /// <param name="target">The target control.</param>
    /// <param name="position">The position of the pointer.</param>
    /// <param name="modifiers">Key modifiers.</param>
    public void Down(Interactive target, Point position = default, KeyModifiers modifiers = default)
    {
        Down(target, target, position, modifiers);
    }

    /// <summary>
    /// Simulates a pointer down event at the specified position with a specific source.
    /// </summary>
    /// <param name="target">The target control.</param>
    /// <param name="source">The source of the event.</param>
    /// <param name="position">The position of the pointer.</param>
    /// <param name="modifiers">Key modifiers.</param>
    public void Down(Interactive target, Interactive source, Point position = default, KeyModifiers modifiers = default)
    {
        _pointer.Capture((IInputElement)target);
        var (root, rootPosition) = ResolveRootPosition(target, position);
        source.RaiseEvent(new PointerPressedEventArgs(
            source, 
            _pointer, 
            root,
            rootPosition,
            Timestamp(),
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
            modifiers));
    }

    /// <summary>
    /// Simulates a pointer move event to the specified position.
    /// </summary>
    /// <param name="target">The target control.</param>
    /// <param name="position">The new position of the pointer.</param>
    /// <param name="modifiers">Key modifiers.</param>
    public void Move(Interactive target, Point position, KeyModifiers modifiers = default)
    {
        Move(target, target, position, modifiers);
    }

    /// <summary>
    /// Simulates a pointer move event to the specified position with a specific source.
    /// </summary>
    /// <param name="target">The target control.</param>
    /// <param name="source">The source of the event.</param>
    /// <param name="position">The new position of the pointer.</param>
    /// <param name="modifiers">Key modifiers.</param>
    public void Move(Interactive target, Interactive source, Point position, KeyModifiers modifiers = default)
    {
        // If a gesture recognizer has captured the pointer, send the event directly to it
        // (mimicking Avalonia's MouseDevice.MouseMove behavior)
        var capturedRecognizer = CapturedGestureRecognizer;
        if (capturedRecognizer != null && s_pointerMovedInternalMethod != null)
        {
            // When gesture recognizer has captured, the source should be the recognizer's Target
            var recognizerTarget = (s_gestureRecognizerTargetProperty?.GetValue(capturedRecognizer) as Interactive) ?? source;
            var root = ((Visual)target).GetPresentationSource()?.RootVisual as Visual ?? (Visual)target;
            
            // Transform position from target coordinates to root coordinates
            // The PointerEventArgs expects position in root visual coordinate space
            var rootPosition = position;
            if (target is Visual targetVisual && root != targetVisual)
            {
                var transform = targetVisual.TransformToVisual(root);
                if (transform.HasValue)
                {
                    rootPosition = transform.Value.Transform(position);
                }
            }
            
            var e = new PointerEventArgs(
                InputElement.PointerMovedEvent, 
                recognizerTarget, 
                _pointer, 
                root,
                rootPosition,
                Timestamp(), 
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), 
                modifiers);
            
            s_pointerMovedInternalMethod.Invoke(capturedRecognizer, new object[] { e });
        }
        else
        {
            var (root, rootPosition) = ResolveRootPosition(target, position);
            var e = new PointerEventArgs(
                InputElement.PointerMovedEvent, 
                source, 
                _pointer, 
                root,
                rootPosition,
                Timestamp(), 
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), 
                modifiers);
            
            target.RaiseEvent(e);
        }
    }

    /// <summary>
    /// Simulates a pointer up event.
    /// </summary>
    /// <param name="target">The target control.</param>
    /// <param name="position">The position of the pointer.</param>
    /// <param name="modifiers">Key modifiers.</param>
    public void Up(Interactive target, Point position = default, KeyModifiers modifiers = default)
    {
        Up(target, target, position, modifiers);
    }

    /// <summary>
    /// Simulates a pointer up event with a specific source.
    /// </summary>
    /// <param name="target">The target control.</param>
    /// <param name="source">The source of the event.</param>
    /// <param name="position">The position of the pointer.</param>
    /// <param name="modifiers">Key modifiers.</param>
    public void Up(Interactive target, Interactive source, Point position = default, KeyModifiers modifiers = default)
    {
        var (root, rootPosition) = ResolveRootPosition(target, position);
        var e = new PointerReleasedEventArgs(
            source, 
            _pointer, 
            root,
            rootPosition,
            Timestamp(),
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), 
            modifiers, 
            MouseButton.Left);

        // If a gesture recognizer has captured the pointer, send the event directly to it
        var capturedRecognizer = CapturedGestureRecognizer;
        if (capturedRecognizer != null && s_pointerReleasedInternalMethod != null)
        {
            s_pointerReleasedInternalMethod.Invoke(capturedRecognizer, new object[] { e });
        }
        else
        {
            source.RaiseEvent(e);
        }

        Cancel();
    }

    private static (Visual Root, Point Position) ResolveRootPosition(Interactive target, Point position)
    {
        var targetVisual = (Visual)target;
        var root = targetVisual.GetPresentationSource()?.RootVisual as Visual ?? targetVisual;
        var rootPosition = targetVisual.TransformToVisual(root)?.Transform(position) ?? position;
        return (root, rootPosition);
    }

    /// <summary>
    /// Simulates a tap (down and up) at the specified position.
    /// </summary>
    /// <param name="target">The target control.</param>
    /// <param name="position">The position of the tap.</param>
    /// <param name="modifiers">Key modifiers.</param>
    public void Tap(Interactive target, Point position = default, KeyModifiers modifiers = default)
    {
        Tap(target, target, position, modifiers);
    }

    /// <summary>
    /// Simulates a tap (down and up) at the specified position with a specific source.
    /// </summary>
    /// <param name="target">The target control.</param>
    /// <param name="source">The source of the event.</param>
    /// <param name="position">The position of the tap.</param>
    /// <param name="modifiers">Key modifiers.</param>
    public void Tap(Interactive target, Interactive source, Point position = default, KeyModifiers modifiers = default)
    {
        Down(target, source, position, modifiers);
        Up(target, source, position, modifiers);
    }

    /// <summary>
    /// Cancels the current gesture, releasing the pointer capture.
    /// </summary>
    public void Cancel()
    {
        // Use reflection to clear the captured gesture recognizer
        if (s_captureGestureRecognizerMethod != null)
        {
            s_captureGestureRecognizerMethod.Invoke(_pointer, new object?[] { null });
        }
        
        _pointer.Capture(null);
        _pointer.IsGestureRecognitionSkipped = false;
    }

    /// <summary>
    /// Performs a drag gesture from one point to another.
    /// </summary>
    /// <param name="target">The target control.</param>
    /// <param name="start">The starting position.</param>
    /// <param name="end">The ending position.</param>
    /// <param name="steps">The number of intermediate steps.</param>
    public void Drag(Interactive target, Point start, Point end, int steps = 10)
    {
        Down(target, start);

        var deltaX = (end.X - start.X) / steps;
        var deltaY = (end.Y - start.Y) / steps;

        for (int i = 1; i <= steps; i++)
        {
            var currentPoint = new Point(
                start.X + (deltaX * i),
                start.Y + (deltaY * i)
            );
            Move(target, currentPoint);
        }

        Up(target, end);
    }
}
