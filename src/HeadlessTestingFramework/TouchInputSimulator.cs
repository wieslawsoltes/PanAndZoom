// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.HeadlessTestingFramework;

/// <summary>
/// A comprehensive touch input simulator for headless testing of Avalonia controls.
/// Provides methods to simulate touch events, multi-touch gestures, pinch/zoom, scroll gestures,
/// and touchpad magnify events.
/// </summary>
public class TouchInputSimulator
{
    private readonly Dictionary<int, TouchPoint> _activeTouchPoints = new();
    private ulong _timestamp;
    private int _nextTouchId;

    /// <summary>
    /// Represents an active touch point.
    /// </summary>
    public class TouchPoint
    {
        /// <summary>
        /// Unique identifier for this touch point.
        /// </summary>
        public int Id { get; }
        
        /// <summary>
        /// Current position of the touch point.
        /// </summary>
        public Point Position { get; set; }
        
        /// <summary>
        /// Pressure of the touch (0.0 to 1.0).
        /// </summary>
        public float Pressure { get; set; }
        
        /// <summary>
        /// Whether the touch point is currently active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Creates a new touch point.
        /// </summary>
        public TouchPoint(int id, Point position, float pressure = 1.0f)
        {
            Id = id;
            Position = position;
            Pressure = pressure;
            IsActive = true;
        }
    }

    /// <summary>
    /// Gets the current timestamp.
    /// </summary>
    public ulong Timestamp => _timestamp;

    /// <summary>
    /// Gets the active touch points.
    /// </summary>
    public IReadOnlyDictionary<int, TouchPoint> ActiveTouchPoints => _activeTouchPoints;

    /// <summary>
    /// Creates a new touch input simulator.
    /// </summary>
    public TouchInputSimulator()
    {
        _timestamp = 0;
        _nextTouchId = 0;
    }

    /// <summary>
    /// Advances the internal timestamp by the specified number of milliseconds.
    /// </summary>
    public void AdvanceTime(int milliseconds)
    {
        _timestamp += (ulong)milliseconds;
    }

    /// <summary>
    /// Resets the simulator to its initial state.
    /// </summary>
    public void Reset()
    {
        _activeTouchPoints.Clear();
        _timestamp = 0;
        _nextTouchId = 0;
    }

    #region Touch Events

    /// <summary>
    /// Simulates a touch press (finger down) event.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="position">Position of the touch point.</param>
    /// <param name="pressure">Touch pressure (0.0 to 1.0).</param>
    /// <returns>The touch point ID.</returns>
    public int TouchDown(Interactive target, Point position, float pressure = 1.0f)
    {
        var touchId = _nextTouchId++;
        var touchPoint = new TouchPoint(touchId, position, pressure);
        _activeTouchPoints[touchId] = touchPoint;

        var args = CreatePointerPressedEventArgs(target, position, touchId);
        target.RaiseEvent(args);

        return touchId;
    }

    /// <summary>
    /// Simulates a touch move event.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="touchId">The ID of the touch point to move.</param>
    /// <param name="newPosition">The new position of the touch point.</param>
    public void TouchMove(Interactive target, int touchId, Point newPosition)
    {
        if (!_activeTouchPoints.TryGetValue(touchId, out var touchPoint))
            throw new InvalidOperationException($"Touch point {touchId} is not active.");

        touchPoint.Position = newPosition;

        var args = CreatePointerMovedEventArgs(target, newPosition, touchId);
        target.RaiseEvent(args);
    }

    /// <summary>
    /// Simulates a touch release (finger up) event.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="touchId">The ID of the touch point to release.</param>
    public void TouchUp(Interactive target, int touchId)
    {
        if (!_activeTouchPoints.TryGetValue(touchId, out var touchPoint))
            throw new InvalidOperationException($"Touch point {touchId} is not active.");

        var args = CreatePointerReleasedEventArgs(target, touchPoint.Position, touchId);
        target.RaiseEvent(args);

        touchPoint.IsActive = false;
        _activeTouchPoints.Remove(touchId);
    }

    /// <summary>
    /// Simulates a complete tap (press and release) at the specified position.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="position">Position of the tap.</param>
    /// <param name="holdTime">Time in milliseconds to hold before releasing.</param>
    public void Tap(Interactive target, Point position, int holdTime = 50)
    {
        var touchId = TouchDown(target, position);
        AdvanceTime(holdTime);
        TouchUp(target, touchId);
    }

    /// <summary>
    /// Simulates a double tap at the specified position.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="position">Position of the double tap.</param>
    /// <param name="tapInterval">Time in milliseconds between taps.</param>
    public void DoubleTap(Interactive target, Point position, int tapInterval = 100)
    {
        Tap(target, position);
        AdvanceTime(tapInterval);
        Tap(target, position);
    }

    #endregion

    #region Gesture Events

    /// <summary>
    /// Simulates a pinch gesture event.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="scale">Scale factor (> 1 for zoom in, &lt; 1 for zoom out).</param>
    /// <param name="scaleOrigin">Origin point of the pinch gesture.</param>
    /// <param name="angleDelta">Rotation angle delta in radians.</param>
    /// <param name="distance">Total distance between fingers.</param>
    public void PinchGesture(Interactive target, double scale, Point scaleOrigin, double angleDelta = 0.0, double distance = 0.0)
    {
        var args = new PinchEventArgs(scale, scaleOrigin, angleDelta, distance)
        {
            RoutedEvent = InputElement.PinchEvent,
            Source = target
        };

        target.RaiseEvent(args);
    }

    /// <summary>
    /// Simulates a pinch gesture ended event.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    public void PinchGestureEnded(Interactive target)
    {
        var args = new PinchEndedEventArgs()
        {
            RoutedEvent = InputElement.PinchEndedEvent,
            Source = target
        };

        target.RaiseEvent(args);
    }

    /// <summary>
    /// Simulates a scroll gesture event (for trackpad scrolling).
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="delta">Scroll delta vector.</param>
    /// <param name="gestureId">Optional gesture ID.</param>
    public void ScrollGesture(Interactive target, Vector delta, int gestureId = 1)
    {
        var args = new ScrollGestureEventArgs(gestureId, delta)
        {
            RoutedEvent = InputElement.ScrollGestureEvent,
            Source = target
        };

        target.RaiseEvent(args);
    }

    /// <summary>
    /// Simulates a scroll gesture ended event.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="gestureId">Optional gesture ID.</param>
    public void ScrollGestureEnded(Interactive target, int gestureId = 1)
    {
        var args = new ScrollGestureEndedEventArgs(gestureId)
        {
            RoutedEvent = InputElement.ScrollGestureEndedEvent,
            Source = target
        };

        target.RaiseEvent(args);
    }

    /// <summary>
    /// Simulates a touchpad magnify gesture (e.g., macOS pinch-to-zoom on trackpad).
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="delta">Magnification delta.</param>
    /// <param name="position">Position of the gesture.</param>
    /// <param name="modifiers">Key modifiers.</param>
    public void TouchpadMagnify(Interactive target, Vector delta, Point position, KeyModifiers modifiers = KeyModifiers.None)
    {
        var args = CreatePointerDeltaEventArgs(target, delta, position, InputElement.PointerTouchPadGestureMagnifyEvent, modifiers);
        target.RaiseEvent(args);
    }

    /// <summary>
    /// Simulates a touchpad swipe gesture.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="delta">Swipe delta vector.</param>
    /// <param name="position">Position of the gesture.</param>
    /// <param name="modifiers">Key modifiers.</param>
    public void TouchpadSwipe(Interactive target, Vector delta, Point position, KeyModifiers modifiers = KeyModifiers.None)
    {
        var args = CreatePointerDeltaEventArgs(target, delta, position, InputElement.PointerTouchPadGestureSwipeEvent, modifiers);
        target.RaiseEvent(args);
    }

    /// <summary>
    /// Simulates a touchpad rotate gesture.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="delta">Rotation delta (in degrees).</param>
    /// <param name="position">Position of the gesture.</param>
    /// <param name="modifiers">Key modifiers.</param>
    public void TouchpadRotate(Interactive target, double delta, Point position, KeyModifiers modifiers = KeyModifiers.None)
    {
        var args = CreatePointerDeltaEventArgs(target, new Vector(delta, 0), position, InputElement.PointerTouchPadGestureRotateEvent, modifiers);
        target.RaiseEvent(args);
    }

    #endregion

    #region Multi-Touch Scenarios

    /// <summary>
    /// Simulates a two-finger pinch zoom gesture.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="centerPoint">Center point of the pinch gesture.</param>
    /// <param name="startDistance">Starting distance between fingers.</param>
    /// <param name="endDistance">Ending distance between fingers.</param>
    /// <param name="steps">Number of intermediate steps.</param>
    public void SimulatePinchZoom(Interactive target, Point centerPoint, double startDistance, double endDistance, int steps = 10)
    {
        // Start with two touch points (horizontal)
        var halfStartDist = startDistance / 2;
        var point1Start = new Point(centerPoint.X - halfStartDist, centerPoint.Y);
        var point2Start = new Point(centerPoint.X + halfStartDist, centerPoint.Y);

        var touch1Id = TouchDown(target, point1Start);
        var touch2Id = TouchDown(target, point2Start);

        var distanceStep = (endDistance - startDistance) / steps;

        for (int i = 1; i <= steps; i++)
        {
            var currentDistance = startDistance + (distanceStep * i);
            var halfDist = currentDistance / 2;

            var point1 = new Point(centerPoint.X - halfDist, centerPoint.Y);
            var point2 = new Point(centerPoint.X + halfDist, centerPoint.Y);

            TouchMove(target, touch1Id, point1);
            TouchMove(target, touch2Id, point2);

            // Also raise pinch gesture event
            var scale = currentDistance / startDistance;
            PinchGesture(target, scale, centerPoint);

            AdvanceTime(16); // ~60fps
        }

        // End gesture
        PinchGestureEnded(target);
        TouchUp(target, touch1Id);
        TouchUp(target, touch2Id);
    }

    /// <summary>
    /// Simulates a two-finger pan gesture.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="startPoint">Starting position of the pan.</param>
    /// <param name="endPoint">Ending position of the pan.</param>
    /// <param name="fingerSpacing">Distance between the two fingers.</param>
    /// <param name="steps">Number of intermediate steps.</param>
    public void SimulateTwoFingerPan(Interactive target, Point startPoint, Point endPoint, double fingerSpacing = 50, int steps = 10)
    {
        var halfSpacing = fingerSpacing / 2;
        var point1Start = new Point(startPoint.X - halfSpacing, startPoint.Y);
        var point2Start = new Point(startPoint.X + halfSpacing, startPoint.Y);

        var touch1Id = TouchDown(target, point1Start);
        var touch2Id = TouchDown(target, point2Start);

        var deltaX = (endPoint.X - startPoint.X) / steps;
        var deltaY = (endPoint.Y - startPoint.Y) / steps;

        for (int i = 1; i <= steps; i++)
        {
            var currentCenter = new Point(
                startPoint.X + (deltaX * i),
                startPoint.Y + (deltaY * i)
            );

            var point1 = new Point(currentCenter.X - halfSpacing, currentCenter.Y);
            var point2 = new Point(currentCenter.X + halfSpacing, currentCenter.Y);

            TouchMove(target, touch1Id, point1);
            TouchMove(target, touch2Id, point2);

            // Also raise scroll gesture event
            ScrollGesture(target, new Vector(deltaX, deltaY));

            AdvanceTime(16);
        }

        ScrollGestureEnded(target);
        TouchUp(target, touch1Id);
        TouchUp(target, touch2Id);
    }

    /// <summary>
    /// Simulates a rotation gesture with two fingers.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="centerPoint">Center point of the rotation.</param>
    /// <param name="radius">Radius of the rotation.</param>
    /// <param name="startAngle">Starting angle in degrees.</param>
    /// <param name="endAngle">Ending angle in degrees.</param>
    /// <param name="steps">Number of intermediate steps.</param>
    public void SimulateRotation(Interactive target, Point centerPoint, double radius, double startAngle, double endAngle, int steps = 10)
    {
        var startAngleRad = startAngle * Math.PI / 180;
        var point1Start = new Point(
            centerPoint.X + radius * Math.Cos(startAngleRad),
            centerPoint.Y + radius * Math.Sin(startAngleRad)
        );
        var point2Start = new Point(
            centerPoint.X + radius * Math.Cos(startAngleRad + Math.PI),
            centerPoint.Y + radius * Math.Sin(startAngleRad + Math.PI)
        );

        var touch1Id = TouchDown(target, point1Start);
        var touch2Id = TouchDown(target, point2Start);

        var angleStep = (endAngle - startAngle) / steps;

        for (int i = 1; i <= steps; i++)
        {
            var currentAngle = startAngle + (angleStep * i);
            var currentAngleRad = currentAngle * Math.PI / 180;

            var point1 = new Point(
                centerPoint.X + radius * Math.Cos(currentAngleRad),
                centerPoint.Y + radius * Math.Sin(currentAngleRad)
            );
            var point2 = new Point(
                centerPoint.X + radius * Math.Cos(currentAngleRad + Math.PI),
                centerPoint.Y + radius * Math.Sin(currentAngleRad + Math.PI)
            );

            TouchMove(target, touch1Id, point1);
            TouchMove(target, touch2Id, point2);

            // Also raise pinch gesture with angle delta
            var angleDeltaRad = angleStep * Math.PI / 180;
            PinchGesture(target, 1.0, centerPoint, angleDeltaRad, radius * 2);

            AdvanceTime(16);
        }

        PinchGestureEnded(target);
        TouchUp(target, touch1Id);
        TouchUp(target, touch2Id);
    }

    /// <summary>
    /// Simulates a single finger drag/pan gesture.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="startPoint">Starting position.</param>
    /// <param name="endPoint">Ending position.</param>
    /// <param name="steps">Number of intermediate steps.</param>
    public void SimulateDrag(Interactive target, Point startPoint, Point endPoint, int steps = 10)
    {
        var touchId = TouchDown(target, startPoint);

        var deltaX = (endPoint.X - startPoint.X) / steps;
        var deltaY = (endPoint.Y - startPoint.Y) / steps;

        for (int i = 1; i <= steps; i++)
        {
            var currentPoint = new Point(
                startPoint.X + (deltaX * i),
                startPoint.Y + (deltaY * i)
            );

            TouchMove(target, touchId, currentPoint);
            AdvanceTime(16);
        }

        TouchUp(target, touchId);
    }

    /// <summary>
    /// Simulates a swipe gesture in a direction.
    /// </summary>
    /// <param name="target">The target control to receive the event.</param>
    /// <param name="startPoint">Starting position.</param>
    /// <param name="direction">Swipe direction.</param>
    /// <param name="distance">Swipe distance.</param>
    /// <param name="duration">Swipe duration in milliseconds.</param>
    public void Swipe(Interactive target, Point startPoint, SwipeDirection direction, double distance = 100, int duration = 200)
    {
        var endPoint = direction switch
        {
            SwipeDirection.Left => new Point(startPoint.X - distance, startPoint.Y),
            SwipeDirection.Right => new Point(startPoint.X + distance, startPoint.Y),
            SwipeDirection.Up => new Point(startPoint.X, startPoint.Y - distance),
            SwipeDirection.Down => new Point(startPoint.X, startPoint.Y + distance),
            _ => startPoint
        };

        var steps = duration / 16; // ~60fps
        SimulateDrag(target, startPoint, endPoint, (int)steps);
    }

    #endregion

    #region Helper Methods

    private Pointer CreateTouchPointer(int touchId)
    {
        return new Pointer(touchId, PointerType.Touch, true);
    }

    private PointerPressedEventArgs CreatePointerPressedEventArgs(Interactive target, Point position, int touchId)
    {
        var pointer = CreateTouchPointer(touchId);
        var properties = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed);
        var (root, rootPosition) = ResolveRootPosition(target, position);
        
        return new PointerPressedEventArgs(
            target,
            pointer,
            root,
            rootPosition,
            _timestamp,
            properties,
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
    }

    private PointerEventArgs CreatePointerMovedEventArgs(Interactive target, Point position, int touchId)
    {
        var pointer = CreateTouchPointer(touchId);
        var properties = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other);
        var (root, rootPosition) = ResolveRootPosition(target, position);
        
        return new PointerEventArgs(
            InputElement.PointerMovedEvent,
            target,
            pointer,
            root,
            rootPosition,
            _timestamp,
            properties,
            KeyModifiers.None);
    }

    private PointerReleasedEventArgs CreatePointerReleasedEventArgs(Interactive target, Point position, int touchId)
    {
        var pointer = CreateTouchPointer(touchId);
        var properties = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased);
        var (root, rootPosition) = ResolveRootPosition(target, position);
        
        return new PointerReleasedEventArgs(
            target,
            pointer,
            root,
            rootPosition,
            _timestamp,
            properties,
            KeyModifiers.None,
            MouseButton.Left)
        {
            RoutedEvent = InputElement.PointerReleasedEvent
        };
    }

    private static (Visual Root, Point Position) ResolveRootPosition(Interactive target, Point position)
    {
        var targetVisual = (Visual)target;
        var root = targetVisual.GetPresentationSource()?.RootVisual as Visual ?? targetVisual;
        var rootPosition = targetVisual.TransformToVisual(root)?.Transform(position) ?? position;
        return (root, rootPosition);
    }

    private PointerDeltaEventArgs CreatePointerDeltaEventArgs(Interactive target, Vector delta, Point position, RoutedEvent routedEvent, KeyModifiers modifiers)
    {
        var pointer = CreateTouchPointer(0);
        var properties = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other);
        
        return new PointerDeltaEventArgs(
            routedEvent,
            target,
            pointer,
            (Visual)target,
            position,
            _timestamp,
            properties,
            modifiers,
            delta);
    }

    #endregion
}

/// <summary>
/// Direction for swipe gestures.
/// </summary>
public enum SwipeDirection
{
    /// <summary>
    /// Swipe from right to left.
    /// </summary>
    Left,
    
    /// <summary>
    /// Swipe from left to right.
    /// </summary>
    Right,
    
    /// <summary>
    /// Swipe from bottom to top.
    /// </summary>
    Up,
    
    /// <summary>
    /// Swipe from top to bottom.
    /// </summary>
    Down
}
