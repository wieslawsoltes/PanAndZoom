// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.HeadlessTestingFramework.Appium;

/// <summary>
/// Represents a UI element with Appium-like API methods.
/// Wraps an Avalonia Control to provide familiar WebDriver/Appium interactions.
/// </summary>
public class AvaloniaElement
{
    private readonly Control _control;
    private readonly AvaloniaDriver _driver;

    /// <summary>
    /// Gets the underlying Avalonia control.
    /// </summary>
    public Control Control => _control;

    /// <summary>
    /// Gets the element's unique identifier.
    /// </summary>
    public string? Id => _control.Name;

    /// <summary>
    /// Gets the element's tag name (type name).
    /// </summary>
    public string TagName => _control.GetType().Name;

    /// <summary>
    /// Gets the element's full type name.
    /// </summary>
    public string FullTagName => _control.GetType().FullName ?? TagName;

    /// <summary>
    /// Gets the element's text content.
    /// </summary>
    public string Text => GetTextContent();

    /// <summary>
    /// Gets a value indicating whether the element is displayed (visible).
    /// </summary>
    public bool Displayed => IsDisplayed();

    /// <summary>
    /// Gets a value indicating whether the element is enabled.
    /// </summary>
    public bool Enabled => _control.IsEnabled;

    /// <summary>
    /// Gets a value indicating whether the element is selected.
    /// </summary>
    public bool Selected => IsSelected();

    /// <summary>
    /// Gets a value indicating whether the element is focused.
    /// </summary>
    public bool Focused => _control.IsFocused;

    /// <summary>
    /// Gets the element's location relative to its parent.
    /// </summary>
    public Point Location => _control.Bounds.Position;

    /// <summary>
    /// Gets the element's size.
    /// </summary>
    public Size Size => _control.Bounds.Size;

    /// <summary>
    /// Gets the element's bounding rectangle.
    /// </summary>
    public Rect Rect => _control.Bounds;

    /// <summary>
    /// Gets the center point of the element.
    /// </summary>
    public Point Center => new(_control.Bounds.Width / 2, _control.Bounds.Height / 2);

    /// <summary>
    /// Gets the absolute center point in screen coordinates.
    /// </summary>
    public Point AbsoluteCenter
    {
        get
        {
            var bounds = _control.Bounds;
            var root = _control.GetPresentationSource()?.RootVisual as Visual;
            if (root != null)
            {
                var transform = _control.TransformToVisual(root);
                if (transform.HasValue)
                {
                    var topLeft = transform.Value.Transform(new Point(0, 0));
                    return new Point(topLeft.X + bounds.Width / 2, topLeft.Y + bounds.Height / 2);
                }
            }
            return Center;
        }
    }

    internal AvaloniaElement(Control control, AvaloniaDriver driver)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    #region Actions

    /// <summary>
    /// Clicks on the element at its center.
    /// </summary>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement Click()
    {
        EnsureInteractable();
        _driver.MouseSimulator.Click(_control, Center);
        return this;
    }

    /// <summary>
    /// Double-clicks on the element.
    /// </summary>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement DoubleClick()
    {
        EnsureInteractable();
        _driver.MouseSimulator.DoubleClick(_control, Center);
        return this;
    }

    /// <summary>
    /// Right-clicks on the element.
    /// </summary>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement RightClick()
    {
        EnsureInteractable();
        _driver.MouseSimulator.RightClick(_control, Center);
        return this;
    }

    /// <summary>
    /// Taps on the element (touch).
    /// </summary>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement Tap()
    {
        EnsureInteractable();
        _driver.TouchSimulator.Tap(_control, Center);
        return this;
    }

    /// <summary>
    /// Double-taps on the element (touch).
    /// </summary>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement DoubleTap()
    {
        EnsureInteractable();
        _driver.TouchSimulator.DoubleTap(_control, Center);
        return this;
    }

    /// <summary>
    /// Long-presses on the element (touch).
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement LongPress(int durationMs = 1000)
    {
        EnsureInteractable();
        _driver.TouchSimulator.Tap(_control, Center, holdTime: durationMs);
        return this;
    }

    /// <summary>
    /// Sends text input to the element.
    /// Uses keyboard simulator to preserve bindings and trigger proper events.
    /// </summary>
    /// <param name="text">The text to send.</param>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement SendKeys(string text)
    {
        EnsureInteractable();
        Focus();

        // Always use keyboard simulator to properly trigger events and preserve bindings
        _driver.KeyboardSimulator.TypeText(_control, text);
        return this;
    }

    /// <summary>
    /// Clears the text content of the element.
    /// Uses SetCurrentValue to preserve bindings.
    /// </summary>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement Clear()
    {
        EnsureInteractable();

        if (_control is TextBox textBox)
        {
            // Use SetCurrentValue to not break bindings
            textBox.SetCurrentValue(TextBox.TextProperty, string.Empty);
        }
        else if (_control is AutoCompleteBox autoComplete)
        {
            // Use SetCurrentValue to not break bindings
            autoComplete.SetCurrentValue(AutoCompleteBox.TextProperty, string.Empty);
        }
        return this;
    }

    /// <summary>
    /// Submits a form. In Avalonia context, this simulates pressing Enter
    /// or clicking a submit button within the form context.
    /// This is the standard Selenium WebElement.Submit() method.
    /// </summary>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement Submit()
    {
        // In Avalonia, submit is typically done via Enter key or button click
        // Try to find and click a submit button, or press Enter
        if (_control is Button button)
        {
            Click();
        }
        else
        {
            // Press Enter to submit (common form behavior)
            Focus();
            _driver.KeyboardSimulator.Enter(_control);
        }
        return this;
    }

    /// <summary>
    /// Sets focus to the element.
    /// </summary>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement Focus()
    {
        _control.Focus();
        return this;
    }

    /// <summary>
    /// Scrolls the element into view.
    /// </summary>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement ScrollIntoView()
    {
        _control.BringIntoView();
        return this;
    }

    /// <summary>
    /// Hovers over the element.
    /// </summary>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement Hover()
    {
        _driver.MouseSimulator.MoveTo(_control, Center);
        return this;
    }

    /// <summary>
    /// Drags this element to the target element.
    /// </summary>
    /// <param name="target">The target element.</param>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement DragTo(AvaloniaElement target)
    {
        EnsureInteractable();
        _driver.MouseSimulator.Drag(_control, Center, target.AbsoluteCenter);
        return this;
    }

    /// <summary>
    /// Swipes on this element in the specified direction.
    /// </summary>
    /// <param name="direction">The swipe direction.</param>
    /// <param name="distance">The swipe distance in pixels.</param>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement Swipe(SwipeDirection direction, double distance = 100)
    {
        EnsureInteractable();
        _driver.TouchSimulator.Swipe(_control, Center, direction, distance);
        return this;
    }

    /// <summary>
    /// Performs a pinch gesture on this element.
    /// </summary>
    /// <param name="scale">Scale factor (greater than 1 = zoom in, less than 1 = zoom out).</param>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement Pinch(double scale)
    {
        EnsureInteractable();
        _driver.TouchSimulator.PinchGesture(_control, scale, Center);
        _driver.TouchSimulator.PinchGestureEnded(_control);
        return this;
    }

    /// <summary>
    /// Performs a scroll gesture on this element.
    /// </summary>
    /// <param name="deltaX">Horizontal scroll amount.</param>
    /// <param name="deltaY">Vertical scroll amount.</param>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement Scroll(double deltaX, double deltaY)
    {
        EnsureInteractable();
        _driver.TouchSimulator.ScrollGesture(_control, new Vector(deltaX, deltaY));
        return this;
    }

    /// <summary>
    /// Presses a key while focused on this element.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <param name="modifiers">Optional key modifiers.</param>
    /// <returns>This element for chaining.</returns>
    public AvaloniaElement PressKey(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        Focus();
        _driver.KeyboardSimulator.KeyPress(_control, key, modifiers: modifiers);
        return this;
    }

    #endregion

    #region Property Access

    /// <summary>
    /// Gets the value of an attribute/property.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The attribute value as string, or null if not found.</returns>
    public string? GetAttribute(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "id" or "name" => _control.Name,
            "class" or "classname" => _control.GetType().Name,
            "text" or "content" => GetTextContent(),
            "enabled" => _control.IsEnabled.ToString().ToLowerInvariant(),
            "visible" or "displayed" => IsDisplayed().ToString().ToLowerInvariant(),
            "focused" => _control.IsFocused.ToString().ToLowerInvariant(),
            "selected" => IsSelected().ToString().ToLowerInvariant(),
            "checked" => IsChecked().ToString().ToLowerInvariant(),
            "value" => GetValue(),
            "automationid" => AutomationProperties.GetAutomationId(_control),
            "accessibilityname" => AutomationProperties.GetName(_control),
            "tooltip" => GetToolTip(),
            "width" => _control.Bounds.Width.ToString(),
            "height" => _control.Bounds.Height.ToString(),
            "x" => _control.Bounds.X.ToString(),
            "y" => _control.Bounds.Y.ToString(),
            _ => GetAvaloniaProperty(name)
        };
    }

    /// <summary>
    /// Gets the value of an Avalonia property.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The property value.</returns>
    public T? GetProperty<T>(string propertyName)
    {
        var prop = AvaloniaPropertyRegistry.Instance.FindRegistered(_control, propertyName);
        if (prop != null)
        {
            var value = _control.GetValue(prop);
            if (value is T typed)
                return typed;
        }

        // Try reflection
        var propInfo = _control.GetType().GetProperty(propertyName);
        if (propInfo != null)
        {
            var value = propInfo.GetValue(_control);
            if (value is T typed)
                return typed;
        }

        return default;
    }

    /// <summary>
    /// Sets an Avalonia property value using SetCurrentValue to preserve bindings.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>This element for chaining.</returns>
    /// <remarks>
    /// Uses SetCurrentValue for Avalonia properties to preserve any data bindings.
    /// For CLR properties, uses reflection which may break bindings.
    /// </remarks>
    public AvaloniaElement SetProperty(string propertyName, object? value)
    {
        var prop = AvaloniaPropertyRegistry.Instance.FindRegistered(_control, propertyName);
        if (prop != null)
        {
            // Use SetCurrentValue to not break bindings
            _control.SetCurrentValue(prop, value);
        }
        else
        {
            // Fallback to reflection for CLR properties (may break bindings)
            var propInfo = _control.GetType().GetProperty(propertyName);
            if (propInfo != null && propInfo.CanWrite)
            {
                propInfo.SetValue(_control, value);
            }
        }
        return this;
    }

    /// <summary>
    /// Gets all CSS/style classes on this element.
    /// </summary>
    /// <returns>List of class names.</returns>
    public IReadOnlyList<string> GetClasses()
    {
        return _control.Classes.ToList();
    }

    /// <summary>
    /// Checks if the element has the specified CSS class.
    /// </summary>
    /// <param name="className">The class name to check.</param>
    /// <returns>True if the class is present.</returns>
    public bool HasClass(string className)
    {
        return _control.Classes.Contains(className);
    }

    #endregion

    #region Finding Child Elements

    /// <summary>
    /// Finds a child element matching the locator.
    /// </summary>
    /// <param name="by">The locator strategy.</param>
    /// <returns>The matching element.</returns>
    /// <exception cref="NoSuchElementException">If no element is found.</exception>
    public AvaloniaElement FindElement(By by)
    {
        return _driver.FindElement(by, _control);
    }

    /// <summary>
    /// Finds all child elements matching the locator.
    /// </summary>
    /// <param name="by">The locator strategy.</param>
    /// <returns>List of matching elements.</returns>
    public IReadOnlyList<AvaloniaElement> FindElements(By by)
    {
        return _driver.FindElements(by, _control);
    }

    /// <summary>
    /// Tries to find a child element, returning null if not found.
    /// </summary>
    /// <param name="by">The locator strategy.</param>
    /// <returns>The element or null.</returns>
    public AvaloniaElement? TryFindElement(By by)
    {
        try
        {
            return FindElement(by);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the parent element.
    /// </summary>
    /// <returns>The parent element or null.</returns>
    public AvaloniaElement? Parent
    {
        get
        {
            var parent = _control.Parent as Control;
            return parent != null ? new AvaloniaElement(parent, _driver) : null;
        }
    }

    /// <summary>
    /// Gets all direct child elements.
    /// </summary>
    /// <returns>List of child elements.</returns>
    public IReadOnlyList<AvaloniaElement> Children
    {
        get
        {
            return _control.GetVisualChildren()
                .OfType<Control>()
                .Select(c => new AvaloniaElement(c, _driver))
                .ToList();
        }
    }

    #endregion

    #region Screenshots

    /// <summary>
    /// Takes a screenshot of this element.
    /// </summary>
    /// <returns>The screenshot as a bitmap.</returns>
    public RenderTargetBitmap? Screenshot()
    {
        var pixelSize = new PixelSize((int)_control.Bounds.Width, (int)_control.Bounds.Height);
        if (pixelSize.Width <= 0 || pixelSize.Height <= 0)
            return null;

        var bitmap = new RenderTargetBitmap(pixelSize);
        bitmap.Render(_control);
        return bitmap;
    }

    /// <summary>
    /// Saves a screenshot of this element to a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    public void SaveScreenshot(string path)
    {
        var bitmap = Screenshot();
        bitmap?.Save(path, PngBitmapEncoderOptions.Default);
    }

    #endregion

    #region Waiting

    /// <summary>
    /// Waits until a condition is met.
    /// </summary>
    /// <param name="condition">The condition to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="pollInterval">Interval between checks.</param>
    /// <returns>This element if condition met.</returns>
    /// <exception cref="TimeoutException">If timeout is exceeded.</exception>
    public AvaloniaElement WaitUntil(Func<AvaloniaElement, bool> condition, TimeSpan? timeout = null, TimeSpan? pollInterval = null)
    {
        timeout ??= TimeSpan.FromSeconds(10);
        pollInterval ??= TimeSpan.FromMilliseconds(100);

        var start = DateTime.Now;
        while (DateTime.Now - start < timeout)
        {
            if (condition(this))
                return this;

            Dispatcher.UIThread.RunJobs();
            Task.Delay(pollInterval.Value).Wait();
        }

        throw new TimeoutException($"Condition not met within {timeout.Value.TotalSeconds}s");
    }

    /// <summary>
    /// Waits until the element is visible.
    /// </summary>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <returns>This element.</returns>
    public AvaloniaElement WaitUntilVisible(TimeSpan? timeout = null)
    {
        return WaitUntil(e => e.Displayed, timeout);
    }

    /// <summary>
    /// Waits until the element is enabled.
    /// </summary>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <returns>This element.</returns>
    public AvaloniaElement WaitUntilEnabled(TimeSpan? timeout = null)
    {
        return WaitUntil(e => e.Enabled, timeout);
    }

    /// <summary>
    /// Waits until the element is clickable (visible and enabled).
    /// </summary>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <returns>This element.</returns>
    public AvaloniaElement WaitUntilClickable(TimeSpan? timeout = null)
    {
        return WaitUntil(e => e.Displayed && e.Enabled, timeout);
    }

    #endregion

    #region Private Helpers

    private void EnsureInteractable()
    {
        if (!_control.IsEnabled)
            throw new InvalidOperationException($"Element '{Id ?? TagName}' is not enabled");

        if (!IsDisplayed())
            throw new InvalidOperationException($"Element '{Id ?? TagName}' is not visible");
    }

    private string GetTextContent()
    {
        return _control switch
        {
            TextBlock tb => tb.Text ?? "",
            TextBox tb => tb.Text ?? "",
            Button btn => btn.Content?.ToString() ?? "",
            ContentControl cc => cc.Content?.ToString() ?? "",
            SelectingItemsControl sic when sic.SelectedItem != null => sic.SelectedItem.ToString() ?? "",
            _ => TryGetTextProperty() ?? ""
        };
    }

    private string? TryGetTextProperty()
    {
        var textProp = _control.GetType().GetProperty("Text");
        return textProp?.GetValue(_control)?.ToString();
    }

    private bool IsDisplayed()
    {
        if (!_control.IsVisible)
            return false;

        if (_control.Bounds.Width <= 0 || _control.Bounds.Height <= 0)
            return false;

        // Check opacity
        if (_control.Opacity <= 0)
            return false;

        return true;
    }

    private bool IsSelected()
    {
        return _control switch
        {
            ListBoxItem lbi => lbi.IsSelected,
            TreeViewItem tvi => tvi.IsSelected,
            TabItem ti => ti.IsSelected,
            SelectingItemsControl sic => sic.SelectedItem != null,
            _ => false
        };
    }

    private bool IsChecked()
    {
        return _control switch
        {
            ToggleButton tb => tb.IsChecked == true,
            _ => false
        };
    }

    private string? GetValue()
    {
        return _control switch
        {
            TextBox tb => tb.Text,
            NumericUpDown nud => nud.Value?.ToString(),
            Slider s => s.Value.ToString(),
            ProgressBar pb => pb.Value.ToString(),
            ComboBox cb => cb.SelectedItem?.ToString(),
            DatePicker dp => dp.SelectedDate?.ToString(),
            _ => null
        };
    }

    private string? GetToolTip()
    {
        var tip = ToolTip.GetTip(_control);
        return tip?.ToString();
    }

    private string? GetAvaloniaProperty(string name)
    {
        var prop = AvaloniaPropertyRegistry.Instance.FindRegistered(_control, name);
        if (prop != null)
        {
            return _control.GetValue(prop)?.ToString();
        }

        var propInfo = _control.GetType().GetProperty(name);
        return propInfo?.GetValue(_control)?.ToString();
    }

    #endregion

    /// <summary>
    /// Returns a string representation of this element.
    /// </summary>
    public override string ToString()
    {
        var id = !string.IsNullOrEmpty(Id) ? $" id='{Id}'" : "";
        return $"<{TagName}{id}>";
    }
}

/// <summary>
/// Exception thrown when an element cannot be found.
/// </summary>
public class NoSuchElementException : Exception
{
    /// <summary>
    /// Gets the locator that was used.
    /// </summary>
    public By? Locator { get; }

    /// <summary>
    /// Creates a new NoSuchElementException.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="locator">The locator that failed.</param>
    public NoSuchElementException(string message, By? locator = null)
        : base(message)
    {
        Locator = locator;
    }

    /// <summary>
    /// Creates a new NoSuchElementException.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public NoSuchElementException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when an element is stale (no longer attached to DOM).
/// </summary>
public class StaleElementReferenceException : Exception
{
    /// <summary>
    /// Creates a new StaleElementReferenceException.
    /// </summary>
    /// <param name="message">The error message.</param>
    public StaleElementReferenceException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when an element is not interactable.
/// </summary>
public class ElementNotInteractableException : Exception
{
    /// <summary>
    /// Creates a new ElementNotInteractableException.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ElementNotInteractableException(string message)
        : base(message)
    {
    }
}
