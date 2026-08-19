// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.HeadlessTestingFramework.Appium;

/// <summary>
/// Main driver class for Avalonia headless testing with Appium-like API.
/// Provides WebDriver/Appium compatible methods for UI automation.
/// </summary>
/// <example>
/// <code>
/// using var driver = new AvaloniaDriver(window);
/// 
/// // Find and interact with elements
/// var button = driver.FindElement(By.Id("submitBtn"));
/// button.Click();
/// 
/// // Use XPath-like queries
/// var textBox = driver.FindElement(By.XPath("//TextBox[@Name='username']"));
/// textBox.SendKeys("testuser");
/// 
/// // Wait for conditions
/// driver.Wait.Until(d => d.FindElement(By.Id("result")).Displayed);
/// </code>
/// </example>
public class AvaloniaDriver : IDisposable
{
    private readonly Control _root;
    private readonly Dictionary<string, Func<Control, bool>> _customPredicates = new();
    private bool _disposed;

    /// <summary>
    /// Gets the root control being automated.
    /// </summary>
    public Control Root => _root;

    /// <summary>
    /// Gets the touch input simulator.
    /// </summary>
    public TouchInputSimulator TouchSimulator { get; }

    /// <summary>
    /// Gets the keyboard input simulator.
    /// </summary>
    public KeyboardInputSimulator KeyboardSimulator { get; }

    /// <summary>
    /// Gets the mouse input simulator.
    /// </summary>
    public MouseInputSimulator MouseSimulator { get; }

    /// <summary>
    /// Gets the gesture simulator.
    /// </summary>
    public GestureSimulator GestureSimulator { get; }

    /// <summary>
    /// Gets the wait helper for explicit waits.
    /// </summary>
    public DriverWait Wait { get; }

    /// <summary>
    /// Gets or sets the implicit wait timeout.
    /// </summary>
    public TimeSpan ImplicitWait { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets the current page source (visual tree XML representation).
    /// </summary>
    public string PageSource => GetPageSource();

    /// <summary>
    /// Gets the title of the window if root is a Window.
    /// </summary>
    public string? Title => (_root as Window)?.Title;

    /// <summary>
    /// Creates a new AvaloniaDriver for the specified window.
    /// </summary>
    /// <param name="window">The window to automate.</param>
    public AvaloniaDriver(Window window) : this((Control)window)
    {
    }

    /// <summary>
    /// Creates a new AvaloniaDriver for the specified control.
    /// </summary>
    /// <param name="root">The root control to automate.</param>
    public AvaloniaDriver(Control root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        TouchSimulator = new TouchInputSimulator();
        KeyboardSimulator = new KeyboardInputSimulator();
        MouseSimulator = new MouseInputSimulator();
        GestureSimulator = new GestureSimulator();
        Wait = new DriverWait(this);
    }

    /// <summary>
    /// Creates a driver from the current application's main window.
    /// </summary>
    /// <returns>A new AvaloniaDriver instance.</returns>
    public static AvaloniaDriver FromApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window != null)
                return new AvaloniaDriver(window);
        }

        throw new InvalidOperationException("No main window available");
    }

    #region Find Elements

    /// <summary>
    /// Finds an element matching the locator.
    /// </summary>
    /// <param name="by">The locator strategy.</param>
    /// <returns>The matching element.</returns>
    /// <exception cref="NoSuchElementException">If no element is found.</exception>
    public AvaloniaElement FindElement(By by)
    {
        return FindElement(by, _root);
    }

    /// <summary>
    /// Finds an element matching the locator, starting from a specific root.
    /// </summary>
    /// <param name="by">The locator strategy.</param>
    /// <param name="root">The root to search from.</param>
    /// <returns>The matching element.</returns>
    internal AvaloniaElement FindElement(By by, Control root)
    {
        var elements = FindElementsInternal(by, root, limit: 1);
        if (elements.Count == 0)
        {
            if (ImplicitWait > TimeSpan.Zero)
            {
                // Retry with implicit wait
                var start = DateTime.Now;
                while (DateTime.Now - start < ImplicitWait)
                {
                    Dispatcher.UIThread.RunJobs();
                    Task.Delay(100).Wait();

                    elements = FindElementsInternal(by, root, limit: 1);
                    if (elements.Count > 0)
                        return new AvaloniaElement(elements[0], this);
                }
            }

            throw new NoSuchElementException(
                $"Unable to locate element using {by.Strategy}: '{by.Value}'. " +
                $"Root: {root.GetType().Name}. Verify the element exists and is in the visual tree.", by);
        }

        return new AvaloniaElement(elements[0], this);
    }

    /// <summary>
    /// Finds all elements matching the locator.
    /// </summary>
    /// <param name="by">The locator strategy.</param>
    /// <returns>List of matching elements.</returns>
    public IReadOnlyList<AvaloniaElement> FindElements(By by)
    {
        return FindElements(by, _root);
    }

    /// <summary>
    /// Finds all elements matching the locator, starting from a specific root.
    /// </summary>
    /// <param name="by">The locator strategy.</param>
    /// <param name="root">The root to search from.</param>
    /// <returns>List of matching elements.</returns>
    internal IReadOnlyList<AvaloniaElement> FindElements(By by, Control root)
    {
        var elements = FindElementsInternal(by, root);
        return elements.Select(c => new AvaloniaElement(c, this)).ToList();
    }

    /// <summary>
    /// Tries to find an element, returning null if not found.
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
    /// Checks if an element exists.
    /// </summary>
    /// <param name="by">The locator strategy.</param>
    /// <returns>True if the element exists.</returns>
    public bool ElementExists(By by)
    {
        return FindElementsInternal(by, _root, limit: 1).Count > 0;
    }

    /// <summary>
    /// Gets the count of elements matching the locator.
    /// </summary>
    /// <param name="by">The locator strategy.</param>
    /// <returns>The number of matching elements.</returns>
    public int ElementCount(By by)
    {
        return FindElementsInternal(by, _root).Count;
    }

    /// <summary>
    /// Registers a custom predicate for use with By.Predicate.
    /// </summary>
    /// <param name="key">The predicate key.</param>
    /// <param name="predicate">The predicate function.</param>
    public void RegisterPredicate(string key, Func<Control, bool> predicate)
    {
        _customPredicates[key] = predicate;
    }

    private List<Control> FindElementsInternal(By by, Control root, int? limit = null)
    {
        return by.Strategy switch
        {
            LocatorStrategy.Id => FindById(root, by.Value, limit),
            LocatorStrategy.Name => FindByName(root, by.Value, limit),
            LocatorStrategy.AutomationId => FindByAutomationId(root, by.Value, limit),
            LocatorStrategy.ClassName => FindByClassName(root, by.Value, limit),
            LocatorStrategy.FullClassName => FindByFullClassName(root, by.Value, limit),
            LocatorStrategy.Type => FindByType(root, by.Value, limit),
            LocatorStrategy.XPath => FindByXPath(root, by.Value, limit),
            LocatorStrategy.Text => FindByText(root, by.Value, by.Options.ExactMatch, limit),
            LocatorStrategy.CssClass => FindByCssClass(root, by.Value, limit),
            LocatorStrategy.TagName => FindByClassName(root, by.Value, limit),
            LocatorStrategy.Property => FindByProperty(root, by.Value, by.Options.PropertyValue, limit),
            LocatorStrategy.AccessibilityName => FindByAccessibilityName(root, by.Value, limit),
            LocatorStrategy.AccessibilityLabel => FindByAccessibilityName(root, by.Value, limit),
            LocatorStrategy.Predicate => FindByPredicate(root, by.Value, limit),
            LocatorStrategy.NameRegex => FindByNameRegex(root, by.Value, limit),
            LocatorStrategy.Focused => FindFocused(root, limit),
            LocatorStrategy.Composite => FindByComposite(root, by, limit),
            LocatorStrategy.Chained => FindByChained(root, by, limit),
            _ => throw new NotSupportedException($"Locator strategy {by.Strategy} is not supported")
        };
    }

    private List<Control> FindById(Control root, string id, int? limit)
    {
        return GetAllDescendants(root)
            .Where(c => c.Name == id)
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private List<Control> FindByName(Control root, string name, int? limit)
    {
        return FindById(root, name, limit);
    }

    private List<Control> FindByAutomationId(Control root, string automationId, int? limit)
    {
        return GetAllDescendants(root)
            .Where(c => AutomationProperties.GetAutomationId(c) == automationId)
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private List<Control> FindByClassName(Control root, string className, int? limit)
    {
        return GetAllDescendants(root)
            .Where(c => c.GetType().Name == className)
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private List<Control> FindByFullClassName(Control root, string fullClassName, int? limit)
    {
        return GetAllDescendants(root)
            .Where(c => c.GetType().FullName == fullClassName)
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private List<Control> FindByType(Control root, string typeName, int? limit)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);

        if (type == null)
            return new List<Control>();

        return GetAllDescendants(root)
            .Where(c => type.IsInstanceOfType(c))
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private List<Control> FindByXPath(Control root, string xpath, int? limit)
    {
        var xpathEngine = new TreeXPath(root);
        var results = xpathEngine.Select(xpath);
        return results
            .OfType<Control>()
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private List<Control> FindByText(Control root, string text, bool exactMatch, int? limit)
    {
        return GetAllDescendants(root)
            .Where(c => MatchesText(c, text, exactMatch))
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private bool MatchesText(Control control, string text, bool exactMatch)
    {
        var content = GetControlText(control);
        if (content == null)
            return false;

        return exactMatch
            ? content.Equals(text, StringComparison.OrdinalIgnoreCase)
            : content.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private string? GetControlText(Control control)
    {
        return control switch
        {
            TextBlock tb => tb.Text,
            TextBox tb => tb.Text,
            Button btn => btn.Content?.ToString(),
            ContentControl cc => cc.Content?.ToString(),
            _ => control.GetType().GetProperty("Text")?.GetValue(control)?.ToString()
        };
    }

    private List<Control> FindByCssClass(Control root, string cssClass, int? limit)
    {
        return GetAllDescendants(root)
            .Where(c => c.Classes.Contains(cssClass))
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private List<Control> FindByProperty(Control root, string propertyName, object? expectedValue, int? limit)
    {
        return GetAllDescendants(root)
            .Where(c => PropertyMatches(c, propertyName, expectedValue))
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private bool PropertyMatches(Control control, string propertyName, object? expectedValue)
    {
        var prop = AvaloniaPropertyRegistry.Instance.FindRegistered(control, propertyName);
        if (prop != null)
        {
            var value = control.GetValue(prop);
            return Equals(value, expectedValue);
        }

        var propInfo = control.GetType().GetProperty(propertyName);
        if (propInfo != null)
        {
            var value = propInfo.GetValue(control);
            return Equals(value, expectedValue);
        }

        return false;
    }

    private List<Control> FindByAccessibilityName(Control root, string name, int? limit)
    {
        return GetAllDescendants(root)
            .Where(c => AutomationProperties.GetName(c) == name)
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private List<Control> FindByPredicate(Control root, string predicateKey, int? limit)
    {
        if (!_customPredicates.TryGetValue(predicateKey, out var predicate))
            throw new InvalidOperationException($"Predicate '{predicateKey}' not registered. Call RegisterPredicate first.");

        return GetAllDescendants(root)
            .Where(predicate)
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private List<Control> FindByNameRegex(Control root, string pattern, int? limit)
    {
        var regex = new Regex(pattern, RegexOptions.Compiled);
        return GetAllDescendants(root)
            .Where(c => c.Name != null && regex.IsMatch(c.Name))
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private List<Control> FindFocused(Control root, int? limit)
    {
        return GetAllDescendants(root)
            .Where(c => c.IsFocused)
            .Take(limit ?? int.MaxValue)
            .ToList();
    }

    private List<Control> FindByComposite(Control root, By by, int? limit)
    {
        var locators = by.Options.CompositeLocators;
        if (locators == null || locators.Length == 0)
            return new List<Control>();

        if (by.Options.CompositeMode == CompositeMode.And)
        {
            // AND: Element must match all locators
            var candidates = GetAllDescendants(root).ToList();
            foreach (var locator in locators)
            {
                var matches = new HashSet<Control>(FindElementsInternal(locator, root));
                candidates = candidates.Where(c => matches.Contains(c)).ToList();
            }
            return candidates.Take(limit ?? int.MaxValue).ToList();
        }
        else
        {
            // OR: Element must match any locator
            var results = new HashSet<Control>();
            foreach (var locator in locators)
            {
                foreach (var match in FindElementsInternal(locator, root))
                {
                    results.Add(match);
                    if (limit.HasValue && results.Count >= limit.Value)
                        return results.ToList();
                }
            }
            return results.ToList();
        }
    }

    private List<Control> FindByChained(Control root, By by, int? limit)
    {
        var locators = by.Options.CompositeLocators;
        if (locators == null || locators.Length == 0)
            return new List<Control>();

        var currentRoots = new List<Control> { root };
        foreach (var locator in locators)
        {
            var nextRoots = new List<Control>();
            foreach (var r in currentRoots)
            {
                nextRoots.AddRange(FindElementsInternal(locator, r));
            }
            currentRoots = nextRoots;

            if (currentRoots.Count == 0)
                break;
        }

        return currentRoots.Take(limit ?? int.MaxValue).ToList();
    }

    private IEnumerable<Control> GetAllDescendants(Control root)
    {
        yield return root;

        foreach (var child in root.GetVisualDescendants().OfType<Control>())
        {
            yield return child;
        }
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Gets the active/focused element.
    /// </summary>
    /// <returns>The active element or null.</returns>
    public AvaloniaElement? ActiveElement
    {
        get
        {
            var focused = GetAllDescendants(_root).FirstOrDefault(c => c.IsFocused);
            return focused != null ? new AvaloniaElement(focused, this) : null;
        }
    }

    /// <summary>
    /// Navigates to a view by setting window content (for single-page apps).
    /// Uses SetCurrentValue to preserve any bindings on the Content property.
    /// </summary>
    /// <param name="viewType">The type of view to navigate to.</param>
    public void NavigateTo(Type viewType)
    {
        var view = Activator.CreateInstance(viewType) as Control;
        if (view != null && _root is ContentControl cc)
        {
            // Use SetCurrentValue to not break bindings
            cc.SetCurrentValue(ContentControl.ContentProperty, view);
        }
    }

    /// <summary>
    /// Navigates to a view by setting window content.
    /// Uses SetCurrentValue to preserve any bindings on the Content property.
    /// </summary>
    /// <typeparam name="T">The type of view to navigate to.</typeparam>
    public void NavigateTo<T>() where T : Control, new()
    {
        if (_root is ContentControl cc)
        {
            // Use SetCurrentValue to not break bindings
            cc.SetCurrentValue(ContentControl.ContentProperty, new T());
        }
    }

    /// <summary>
    /// Navigates back (if using Frame navigation).
    /// </summary>
    public void Back()
    {
        KeyboardSimulator.KeyPress(_root, Key.Back, modifiers: KeyModifiers.Alt);
    }

    /// <summary>
    /// Navigates forward (if using Frame navigation).
    /// </summary>
    public void Forward()
    {
        KeyboardSimulator.KeyPress(_root, Key.Right, modifiers: KeyModifiers.Alt);
    }

    /// <summary>
    /// Refreshes the current view.
    /// </summary>
    public void Refresh()
    {
        // Force layout update
        _root.InvalidateArrange();
        _root.InvalidateMeasure();
        _root.InvalidateVisual();
        Dispatcher.UIThread.RunJobs();
    }

    #endregion

    #region Screenshots

    /// <summary>
    /// Takes a screenshot of the entire root control.
    /// </summary>
    /// <returns>The screenshot as a bitmap.</returns>
    public RenderTargetBitmap? Screenshot()
    {
        var pixelSize = new PixelSize((int)_root.Bounds.Width, (int)_root.Bounds.Height);
        if (pixelSize.Width <= 0 || pixelSize.Height <= 0)
            return null;

        var bitmap = new RenderTargetBitmap(pixelSize);
        bitmap.Render(_root);
        return bitmap;
    }

    /// <summary>
    /// Saves a screenshot to a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    public void SaveScreenshot(string path)
    {
        var bitmap = Screenshot();
        bitmap?.Save(path, PngBitmapEncoderOptions.Default);
    }

    /// <summary>
    /// Takes a screenshot and returns it as Base64.
    /// </summary>
    /// <returns>Base64-encoded PNG image.</returns>
    public string? ScreenshotAsBase64()
    {
        var bitmap = Screenshot();
        if (bitmap == null)
            return null;

        using var stream = new MemoryStream();
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return Convert.ToBase64String(stream.ToArray());
    }

    #endregion

    #region Window Management

    /// <summary>
    /// Maximizes the window.
    /// </summary>
    public void Maximize()
    {
        if (_root is Window window)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    /// <summary>
    /// Minimizes the window.
    /// </summary>
    public void Minimize()
    {
        if (_root is Window window)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    /// <summary>
    /// Restores the window to normal state.
    /// </summary>
    public void Restore()
    {
        if (_root is Window window)
        {
            window.WindowState = WindowState.Normal;
        }
    }

    /// <summary>
    /// Makes the window fullscreen.
    /// </summary>
    public void Fullscreen()
    {
        if (_root is Window window)
        {
            window.WindowState = WindowState.FullScreen;
        }
    }

    /// <summary>
    /// Resizes the window.
    /// </summary>
    /// <param name="width">New width.</param>
    /// <param name="height">New height.</param>
    public void SetWindowSize(double width, double height)
    {
        if (_root is Window window)
        {
            window.Width = width;
            window.Height = height;
        }
    }

    /// <summary>
    /// Moves the window.
    /// </summary>
    /// <param name="x">New X position.</param>
    /// <param name="y">New Y position.</param>
    public void SetWindowPosition(int x, int y)
    {
        if (_root is Window window)
        {
            window.Position = new PixelPoint(x, y);
        }
    }

    /// <summary>
    /// Gets the window size.
    /// </summary>
    /// <returns>The window size.</returns>
    public Size GetWindowSize()
    {
        if (_root is Window window)
        {
            return new Size(window.Width, window.Height);
        }
        return _root.Bounds.Size;
    }

    /// <summary>
    /// Gets the window position.
    /// </summary>
    /// <returns>The window position.</returns>
    public PixelPoint GetWindowPosition()
    {
        if (_root is Window window)
        {
            return window.Position;
        }
        return default;
    }

    /// <summary>
    /// Closes the window.
    /// </summary>
    public void Close()
    {
        if (_root is Window window)
        {
            window.Close();
        }
    }

    #endregion

    #region Input Actions

    /// <summary>
    /// Clicks at the specified position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    public void Click(double x, double y)
    {
        MouseSimulator.Click(_root, new Point(x, y));
    }

    /// <summary>
    /// Taps at the specified position (touch).
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    public void Tap(double x, double y)
    {
        TouchSimulator.Tap(_root, new Point(x, y));
    }

    /// <summary>
    /// Sends keys to the focused element.
    /// </summary>
    /// <param name="keys">The keys to send.</param>
    public void SendKeys(string keys)
    {
        KeyboardSimulator.TypeText(_root, keys);
    }

    /// <summary>
    /// Presses a key.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <param name="modifiers">Optional modifiers.</param>
    public void PressKey(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        KeyboardSimulator.KeyPress(_root, key, modifiers: modifiers);
    }

    /// <summary>
    /// Creates a new touch action chain.
    /// </summary>
    /// <returns>A new TouchAction instance.</returns>
    public TouchAction CreateTouchAction()
    {
        return new TouchAction(this);
    }

    #endregion

    #region Page Source

    private string GetPageSource()
    {
        var writer = new System.Text.StringBuilder();
        WriteElement(writer, _root, 0);
        return writer.ToString();
    }

    private void WriteElement(System.Text.StringBuilder writer, Control control, int indent)
    {
        var indentStr = new string(' ', indent * 2);
        var name = control.Name != null ? $" name=\"{control.Name}\"" : "";
        var automationId = AutomationProperties.GetAutomationId(control);
        var autoId = !string.IsNullOrEmpty(automationId) ? $" automationId=\"{automationId}\"" : "";
        var classes = control.Classes.Count > 0 ? $" class=\"{string.Join(" ", control.Classes)}\"" : "";
        var text = GetControlText(control);
        var textAttr = !string.IsNullOrEmpty(text) ? $" text=\"{EscapeXml(text)}\"" : "";

        var children = control.GetVisualChildren().OfType<Control>().ToList();
        if (children.Count == 0)
        {
            writer.AppendLine($"{indentStr}<{control.GetType().Name}{name}{autoId}{classes}{textAttr} />");
        }
        else
        {
            writer.AppendLine($"{indentStr}<{control.GetType().Name}{name}{autoId}{classes}{textAttr}>");
            foreach (var child in children)
            {
                WriteElement(writer, child, indent + 1);
            }
            writer.AppendLine($"{indentStr}</{control.GetType().Name}>");
        }
    }

    private string EscapeXml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    #endregion

    #region Execute Script (DataContext manipulation)

    /// <summary>
    /// Executes an action on the DataContext.
    /// </summary>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    /// <param name="action">The action to execute.</param>
    public void ExecuteScript<TViewModel>(Action<TViewModel> action) where TViewModel : class
    {
        if (_root.DataContext is TViewModel vm)
        {
            action(vm);
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>
    /// Gets a value from the DataContext.
    /// </summary>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="selector">The selector function.</param>
    /// <returns>The selected value.</returns>
    public TResult? ExecuteScript<TViewModel, TResult>(Func<TViewModel, TResult> selector) where TViewModel : class
    {
        if (_root.DataContext is TViewModel vm)
        {
            return selector(vm);
        }
        return default;
    }

    #endregion

    /// <summary>
    /// Disposes the driver resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _customPredicates.Clear();
            GC.SuppressFinalize(this);
        }
    }
}

/// <summary>
/// Provides wait functionality for the driver.
/// </summary>
public class DriverWait
{
    private readonly AvaloniaDriver _driver;

    /// <summary>
    /// Gets or sets the default timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the polling interval.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    internal DriverWait(AvaloniaDriver driver)
    {
        _driver = driver;
    }

    /// <summary>
    /// Waits until a condition is met.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="condition">The condition to wait for.</param>
    /// <param name="timeout">Optional custom timeout.</param>
    /// <returns>The result of the condition.</returns>
    public T Until<T>(Func<AvaloniaDriver, T> condition, TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? Timeout;
        var start = DateTime.Now;

        Exception? lastException = null;

        while (DateTime.Now - start < actualTimeout)
        {
            try
            {
                var result = condition(_driver);
                if (result != null && !Equals(result, false))
                    return result;
            }
            catch (NoSuchElementException ex)
            {
                lastException = ex;
            }
            catch (StaleElementReferenceException ex)
            {
                lastException = ex;
            }

            Dispatcher.UIThread.RunJobs();
            Task.Delay(PollingInterval).Wait();
        }

        throw new TimeoutException(
            $"Condition not met within {actualTimeout.TotalSeconds}s",
            lastException);
    }

    /// <summary>
    /// Waits for an element to be present.
    /// </summary>
    /// <param name="by">The locator.</param>
    /// <param name="timeout">Optional custom timeout.</param>
    /// <returns>The element.</returns>
    public AvaloniaElement ForElement(By by, TimeSpan? timeout = null)
    {
        return Until(d => d.FindElement(by), timeout);
    }

    /// <summary>
    /// Waits for an element to be visible.
    /// </summary>
    /// <param name="by">The locator.</param>
    /// <param name="timeout">Optional custom timeout.</param>
    /// <returns>The element.</returns>
    public AvaloniaElement ForElementVisible(By by, TimeSpan? timeout = null)
    {
        return Until(d =>
        {
            var element = d.FindElement(by);
            return element.Displayed ? element : null!;
        }, timeout);
    }

    /// <summary>
    /// Waits for an element to be clickable.
    /// </summary>
    /// <param name="by">The locator.</param>
    /// <param name="timeout">Optional custom timeout.</param>
    /// <returns>The element.</returns>
    public AvaloniaElement ForElementClickable(By by, TimeSpan? timeout = null)
    {
        return Until(d =>
        {
            var element = d.FindElement(by);
            return element.Displayed && element.Enabled ? element : null!;
        }, timeout);
    }

    /// <summary>
    /// Waits for an element to disappear.
    /// </summary>
    /// <param name="by">The locator.</param>
    /// <param name="timeout">Optional custom timeout.</param>
    public void ForElementNotPresent(By by, TimeSpan? timeout = null)
    {
        Until(d => !d.ElementExists(by), timeout);
    }

    /// <summary>
    /// Waits for text to be present in an element.
    /// </summary>
    /// <param name="by">The locator.</param>
    /// <param name="text">The text to wait for.</param>
    /// <param name="timeout">Optional custom timeout.</param>
    /// <returns>True when text is present.</returns>
    public bool ForTextPresent(By by, string text, TimeSpan? timeout = null)
    {
        return Until(d =>
        {
            var element = d.FindElement(by);
            return element.Text.Contains(text);
        }, timeout);
    }

    /// <summary>
    /// Waits for a specified duration.
    /// </summary>
    /// <param name="duration">The duration to wait.</param>
    public void For(TimeSpan duration)
    {
        Task.Delay(duration).Wait();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Waits for a specified number of milliseconds.
    /// </summary>
    /// <param name="milliseconds">The milliseconds to wait.</param>
    public void ForMilliseconds(int milliseconds)
    {
        For(TimeSpan.FromMilliseconds(milliseconds));
    }
}
