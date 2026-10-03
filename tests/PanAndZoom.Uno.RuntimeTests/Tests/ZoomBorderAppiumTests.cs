// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;
using static PanAndZoom.Uno.RuntimeTests.Infrastructure.ScenarioTestHelpers;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Port of the Avalonia <c>ZoomBorderAppiumTests</c>.
/// </summary>
/// <remarks>
/// The Avalonia tests use the Appium-like HeadlessTestingFramework driver (<c>AvaloniaDriver</c>, <c>By</c>,
/// <c>TouchAction</c>, <c>MultiTouchAction</c>). On Uno the elements are located by walking the visual tree
/// (<see cref="ScenarioTestHelpers"/>), the gestures are real injected touch/mouse input and the assertions verify
/// the resulting <see cref="ZoomBorder"/> state.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderAppiumTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    #region Helper Methods

    private static async Task<ZoomBorder> CreateAndLoadAsync(FrameworkElement? content = null, StretchMode stretch = StretchMode.None, Action<ZoomBorder>? configure = null)
    {
        var zoomBorder = CreateZoomBorder(content, stretch);
        configure?.Invoke(zoomBorder);

        // Avalonia: new Window { Content = zoomBorder }.Show() -> loaded into the runtime test window.
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        return zoomBorder;
    }

    private static ZoomBorder CreateZoomBorder(FrameworkElement? content = null, StretchMode stretch = StretchMode.None)
    {
        var zoomBorder = new ZoomBorder
        {
            Name = "TestZoomBorder",
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            EnablePan = true,
            EnableZoom = true,
            EnableGestures = true,
            Background = new SolidColorBrush(Colors.Gray),
            Stretch = stretch // Default to None to avoid auto-fit during tests
        };

        AutomationProperties.SetAutomationId(zoomBorder, "zoom_border");

        zoomBorder.Child = content ?? new Border
        {
            Name = "ContentBorder",
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Blue),
            Child = new TextBlock
            {
                Name = "ContentText",
                Text = "Zoom Me",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        return zoomBorder;
    }

    // The Avalonia driver searches the test window (which only hosts the ZoomBorder): search from the
    // runtime test content host (the parent of the loaded ZoomBorder).
    private static DependencyObject Root(FrameworkElement element) => VisualTreeHelper.GetParent(element) ?? element;

    private static Point Window(ZoomBorder zoomBorder, double x, double y) => ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(x, y));

    private static Point Center(ZoomBorder zoomBorder) => ZoomBorderTestHelper.GetWindowCenter(zoomBorder);

    private static void AssertUnchanged(ZoomBorder zoomBorder)
    {
        Assert.AreEqual(1.0, zoomBorder.ZoomX, 1e-9);
        Assert.AreEqual(1.0, zoomBorder.ZoomY, 1e-9);
        Assert.AreEqual(0.0, zoomBorder.OffsetX, 1e-9);
        Assert.AreEqual(0.0, zoomBorder.OffsetY, 1e-9);
    }

    #endregion

    #region Find ZoomBorder Tests

    [TestMethod]
    public async Task Driver_FindElement_ByName_FindsZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // By.Name -> x:Name lookup in the visual tree.
        var element = UIHelper.GetChild<FrameworkElement>("TestZoomBorder", Root(zoomBorder));

        Assert.IsNotNull(element);
        Assert.AreEqual("ZoomBorder", element.GetType().Name);
    }

    [TestMethod]
    public async Task Driver_FindElement_ByAutomationId_FindsZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = FindByAutomationId(Root(zoomBorder), "zoom_border");

        Assert.IsNotNull(element);
        Assert.IsInstanceOfType(element, typeof(ZoomBorder));
    }

    [TestMethod]
    public async Task Driver_FindElement_ByClassName_FindsZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // By.ClassName -> runtime type name.
        var element = FindByTypeName(Root(zoomBorder), "ZoomBorder").FirstOrDefault();

        Assert.IsNotNull(element);
        Assert.IsInstanceOfType(element, typeof(ZoomBorder));
    }

    [TestMethod]
    public async Task Driver_FindElement_ByType_FindsZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));

        Assert.IsNotNull(element);
        Assert.AreSame(zoomBorder, element);
    }

    [TestMethod]
    public async Task Driver_FindElement_ByXPath_FindsZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // XPath "//ZoomBorder[@Name='TestZoomBorder']" -> type name + Name predicate over the visual tree.
        var element = FindFirst(Root(zoomBorder), e => e.GetType().Name == "ZoomBorder" && e.Name == "TestZoomBorder");

        Assert.IsNotNull(element);
        Assert.IsInstanceOfType(element, typeof(ZoomBorder));
    }

    #endregion

    #region ZoomBorder Property Tests

    [TestMethod]
    public async Task Element_GetProperty_ZoomX_ReturnsValue()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        var zoomX = GetProperty<double>(element, "ZoomX");

        Assert.AreEqual(1.0, zoomX);
    }

    [TestMethod]
    public async Task Element_GetProperty_ZoomY_ReturnsValue()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        var zoomY = GetProperty<double>(element, "ZoomY");

        Assert.AreEqual(1.0, zoomY);
    }

    [TestMethod]
    public async Task Element_GetProperty_EnablePan_ReturnsTrue()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        var enablePan = GetProperty<bool>(element, "EnablePan");

        Assert.IsTrue(enablePan);
    }

    [TestMethod]
    public async Task Element_GetProperty_EnableZoom_ReturnsTrue()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        var enableZoom = GetProperty<bool>(element, "EnableZoom");

        Assert.IsTrue(enableZoom);
    }

    [TestMethod]
    public async Task Element_SetProperty_ZoomSpeed_ChangesValue()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        SetProperty(element, "ZoomSpeed", 1.5);

        Assert.AreEqual(1.5, zoomBorder.ZoomSpeed);
    }

    [TestMethod]
    public async Task Element_SetProperty_EnablePan_ChangesValue()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        SetProperty(element, "EnablePan", false);

        Assert.IsFalse(zoomBorder.EnablePan);
    }

    #endregion

    #region ZoomBorder Command Tests via Appium API

    [TestMethod]
    public async Task Element_InvokeCommand_ZoomIn_IncreasesZoom()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var initialZoom = zoomBorder.ZoomX;

        if (zoomBorder.ZoomInCommand.CanExecute(null))
        {
            zoomBorder.ZoomInCommand.Execute(null);
        }

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom);
    }

    [TestMethod]
    public async Task Element_InvokeCommand_ZoomOut_DecreasesZoom()
    {
        var zoomBorder = await CreateAndLoadAsync();

        zoomBorder.ZoomInCommand.Execute(null);
        var zoomAfterIn = zoomBorder.ZoomX;

        zoomBorder.ZoomOutCommand.Execute(null);

        Assert.IsTrue(zoomBorder.ZoomX < zoomAfterIn);
    }

    [TestMethod]
    public async Task Element_InvokeCommand_Reset_ResetsZoom()
    {
        var zoomBorder = await CreateAndLoadAsync();

        zoomBorder.ZoomInCommand.Execute(null);
        Assert.AreNotEqual(1.0, zoomBorder.ZoomX);

        zoomBorder.ResetCommand.Execute(null);

        Assert.AreEqual(1.0, zoomBorder.ZoomX);
        Assert.AreEqual(0.0, zoomBorder.OffsetX);
        Assert.AreEqual(0.0, zoomBorder.OffsetY);
    }

    #endregion

    #region Touch Gesture Tests with TouchAction

    [TestMethod]
    public async Task TouchAction_Tap_OnZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // TouchAction.Tap -> real injected touch tap.
        TouchTap(Center(zoomBorder));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Verify zoom remains unchanged at 1.0 (StretchMode.None)
        Assert.AreEqual(1.0, zoomBorder.ZoomX);
        AssertUnchanged(zoomBorder);
    }

    [TestMethod]
    public async Task TouchAction_DoubleTap_OnZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync(configure: zb =>
        {
            zb.EnableDoubleClickZoom = true;
            zb.DoubleClickZoomFactor = 2.0;
        });

        // TouchAction.DoubleTap -> real injected touch double tap (the Avalonia test could only verify the action chain).
        TouchDoubleTap(Center(zoomBorder));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(2.0, zoomBorder.ZoomX, 1e-9);
        Assert.AreEqual(2.0, zoomBorder.ZoomY, 1e-9);
    }

    [TestMethod]
    public async Task TouchAction_LongPress_OnZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        await TouchHoldAsync(Center(zoomBorder), 500);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // A long press without movement neither pans nor zooms.
        AssertUnchanged(zoomBorder);
    }

    [TestMethod]
    public async Task TouchAction_Swipe_OnZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync(configure: zb => zb.PanButton = ButtonName.Left);

        var center = Center(zoomBorder);
        await TouchDragAsync(center, new Point(center.X + 50, center.Y));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // One finger touch pans the content (WinUI manipulation) with the finger.
        Assert.AreEqual(1.0, zoomBorder.ZoomX, 1e-9);
        Assert.AreEqual(50.0, zoomBorder.OffsetX, ZoomBorderTestHelper.TouchPanTolerance());
        Assert.AreEqual(0.0, zoomBorder.OffsetY, 1.0);
    }

    [TestMethod]
    public async Task TouchAction_SwipeDirection_OnZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // Swipe(SwipeDirection.Right, 100) from the element center.
        var center = Center(zoomBorder);
        await TouchDragAsync(center, new Point(center.X + 100, center.Y));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(100.0, zoomBorder.OffsetX, ZoomBorderTestHelper.TouchPanTolerance());
        Assert.AreEqual(0.0, zoomBorder.OffsetY, 1.0);
    }

    #endregion

    #region MultiTouch Tests

    [TestMethod]
    public async Task MultiTouchAction_Pinch_ZoomIn()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // MultiTouchAction.Pinch(scale: 1.5) -> two injected fingers spreading from 100 to 150 pixels.
        await PinchAsync(Center(zoomBorder), 100, 150);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(1.5, zoomBorder.ZoomX, 0.05);
        Assert.AreEqual(zoomBorder.ZoomX, zoomBorder.ZoomY, 1e-9);
    }

    [TestMethod]
    public async Task MultiTouchAction_Pinch_ZoomOut()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // MultiTouchAction.Pinch(scale: 0.5) -> two injected fingers closing from 200 to 100 pixels.
        await PinchAsync(Center(zoomBorder), 200, 100);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(0.5, zoomBorder.ZoomX, 0.05);
        Assert.AreEqual(zoomBorder.ZoomX, zoomBorder.ZoomY, 1e-9);
    }

    [TestMethod]
    public async Task MultiTouchAction_Scroll_OnZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // MultiTouchAction.Scroll(0, 50) is a scroll gesture delta (scroll direction semantics): the equivalent
        // touch manipulation moves the finger 50 pixels up, the content follows the finger.
        var center = Center(zoomBorder);
        await TouchDragAsync(center, new Point(center.X, center.Y - 50));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(0.0, zoomBorder.OffsetX, 1.0);
        Assert.AreEqual(-50.0, zoomBorder.OffsetY, ZoomBorderTestHelper.TouchPanTolerance());
    }

    #endregion

    #region Wait and ExpectedConditions Tests

    [TestMethod]
    public async Task Wait_Until_ElementExists_ZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var root = Root(zoomBorder);

        ZoomBorder? element = null;
        var found = await ZoomBorderTestHelper.WaitForAsync(() => (element = Descendants(root).OfType<ZoomBorder>().FirstOrDefault()) != null, 5000);

        Assert.IsTrue(found);
        Assert.IsInstanceOfType(element, typeof(ZoomBorder));
    }

    [TestMethod]
    public async Task Wait_Until_ElementIsVisible_ZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var root = Root(zoomBorder);

        ZoomBorder? element = null;
        var visible = await ZoomBorderTestHelper.WaitForAsync(() => (element = Descendants(root).OfType<ZoomBorder>().FirstOrDefault()) is { } zb && IsDisplayed(zb), 5000);

        Assert.IsTrue(visible);
        Assert.IsNotNull(element);
        Assert.IsTrue(IsDisplayed(element));
    }

    [TestMethod]
    public async Task Wait_Until_PropertyValue_ZoomX()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var root = Root(zoomBorder);

        var result = await ZoomBorderTestHelper.WaitForAsync(() =>
        {
            var zb = UIHelper.GetChild<ZoomBorder>(root);
            return GetProperty<double>(zb, "ZoomX") == 1.0;
        }, 5000);

        Assert.IsTrue(result);
    }

    #endregion

    #region Find Child Elements Tests

    [TestMethod]
    public async Task Element_FindElement_ChildContent()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var contentBorder = FindByName(zoomBorder, "ContentBorder");

        Assert.IsNotNull(contentBorder);
        Assert.AreEqual("Border", contentBorder.GetType().Name);
    }

    [TestMethod]
    public async Task Element_FindElement_TextContent()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var textBlock = FindByName(zoomBorder, "ContentText") as TextBlock;

        Assert.IsNotNull(textBlock);
        Assert.AreEqual("Zoom Me", textBlock.Text);
    }

    [TestMethod]
    public async Task Element_FindElements_AllBorders()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var borders = FindByTypeName(Root(zoomBorder), "Border");

        // The Uno ZoomBorder is a Control whose template hosts a Border (PART_Border), so the template border
        // and ContentBorder are found (Avalonia: the ZoomBorder itself derives from Border).
        Assert.IsTrue(borders.Count >= 2);
        Assert.IsTrue(borders.Any(b => b.Name == "ContentBorder"));
    }

    #endregion

    #region Element State Tests

    [TestMethod]
    public async Task Element_Displayed_ZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));

        Assert.IsTrue(IsDisplayed(element));
    }

    [TestMethod]
    public async Task Element_Enabled_ZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));

        Assert.IsTrue(element.IsEnabled);
    }

    [TestMethod]
    public async Task Element_Location_ZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var bounds = GetWindowBounds(zoomBorder);

        Assert.IsTrue(bounds.X >= 0);
        Assert.IsTrue(bounds.Y >= 0);
    }

    [TestMethod]
    public async Task Element_Size_ZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var bounds = GetWindowBounds(zoomBorder);

        Assert.IsTrue(bounds.Width > 0);
        Assert.IsTrue(bounds.Height > 0);
        Assert.AreEqual(400.0, bounds.Width, 1e-9);
        Assert.AreEqual(300.0, bounds.Height, 1e-9);
    }

    [TestMethod]
    public async Task Element_Center_ZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var center = Center(zoomBorder);

        Assert.IsTrue(center.X > 0);
        Assert.IsTrue(center.Y > 0);
    }

    #endregion

    #region Complex Content Tests

    [TestMethod]
    public async Task ZoomBorder_WithCanvas_Content()
    {
        var canvas = new Canvas
        {
            Name = "DrawingCanvas",
            Width = 500,
            Height = 400,
            Background = new SolidColorBrush(Colors.White)
        };

        var rect = new Rectangle
        {
            Name = "Rect1",
            Width = 100,
            Height = 50,
            Fill = new SolidColorBrush(Colors.Red)
        };
        Canvas.SetLeft(rect, 50);
        Canvas.SetTop(rect, 50);
        canvas.Children.Add(rect);

        var ellipse = new Ellipse
        {
            Name = "Ellipse1",
            Width = 80,
            Height = 80,
            Fill = new SolidColorBrush(Colors.Green)
        };
        Canvas.SetLeft(ellipse, 200);
        Canvas.SetTop(ellipse, 100);
        canvas.Children.Add(ellipse);

        var zoomBorder = await CreateAndLoadAsync(canvas);
        var root = Root(zoomBorder);

        var zoomElement = UIHelper.GetChild<ZoomBorder>(root);
        var canvasElement = FindByName(zoomElement, "DrawingCanvas");
        var rectElement = FindByName(root, "Rect1");
        var ellipseElement = FindByName(root, "Ellipse1");

        Assert.IsNotNull(canvasElement);
        Assert.IsNotNull(rectElement);
        Assert.IsNotNull(ellipseElement);
    }

    [TestMethod]
    public async Task ZoomBorder_WithImage_Content()
    {
        var image = new Image
        {
            Name = "TestImage",
            Width = 300,
            Height = 200,
            Stretch = Stretch.Uniform
        };

        var zoomBorder = await CreateAndLoadAsync(image);

        var zoomElement = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        var imageElement = FindByName(zoomElement, "TestImage");

        Assert.IsNotNull(imageElement);
        Assert.AreEqual("Image", imageElement.GetType().Name);
    }

    #endregion

    #region Composite Locator Tests

    [TestMethod]
    public async Task By_Chained_FindsNestedElement()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // By.Chained(By.Type<ZoomBorder>(), By.Name("ContentBorder")) -> nested visual tree queries.
        var element = Descendants(Root(zoomBorder)).OfType<ZoomBorder>()
            .Select(zb => FindByName(zb, "ContentBorder"))
            .FirstOrDefault(e => e != null);

        Assert.IsNotNull(element);
        Assert.AreEqual("Border", element.GetType().Name);
    }

    [TestMethod]
    public async Task By_All_FindsElementMatchingAllConditions()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // By.All(By.ClassName("ZoomBorder"), By.Name("TestZoomBorder")) -> combined predicate.
        var element = FindFirst(Root(zoomBorder), e => e.GetType().Name == "ZoomBorder" && e.Name == "TestZoomBorder");

        Assert.IsNotNull(element);
        Assert.IsInstanceOfType(element, typeof(ZoomBorder));
    }

    [TestMethod]
    public async Task By_Any_FindsElementMatchingAnyCondition()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // By.Any(By.Name("NonExistent"), By.Name("TestZoomBorder")) -> alternative predicate.
        var element = FindFirst(Root(zoomBorder), e => e.Name == "NonExistent" || e.Name == "TestZoomBorder");

        Assert.IsNotNull(element);
        Assert.IsInstanceOfType(element, typeof(ZoomBorder));
    }

    #endregion

    #region Property Locator Tests

    [TestMethod]
    public async Task By_Property_EnablePan_FindsZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = FindByProperty(Root(zoomBorder), "EnablePan", true);

        Assert.IsNotNull(element);
        Assert.IsInstanceOfType(element, typeof(ZoomBorder));
    }

    [TestMethod]
    public async Task By_Property_Width_FindsZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = FindByProperty(Root(zoomBorder), "Width", 400.0);

        Assert.IsNotNull(element);
        Assert.AreSame(zoomBorder, element);
    }

    #endregion

    #region Screenshot Tests

    [TestMethod]
    public async Task Element_Screenshot_ReturnsImage()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var screenshot = await ScreenShotAsync(zoomBorder);

        Assert.IsNotNull(screenshot);
        Assert.IsTrue(screenshot.Width > 0);
        Assert.IsTrue(screenshot.Height > 0);
    }

    [TestMethod]
    public async Task Driver_Screenshot_ReturnsWindowImage()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // Driver screenshot -> screenshot of the window root element.
        var root = (FrameworkElement)GetWindowRoot(zoomBorder);
        var screenshot = await ScreenShotAsync(root);

        Assert.IsNotNull(screenshot);
        Assert.IsTrue(screenshot.Width > 0);
        Assert.IsTrue(screenshot.Height > 0);
        Assert.IsTrue(screenshot.Width >= 400);
        Assert.IsTrue(screenshot.Height >= 300);
    }

    #endregion

    #region ZoomBorder Direct Method Tests via Appium API

    [TestMethod]
    public async Task ZoomBorder_ZoomIn_ViaAppiumElement()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        var initialZoom = GetProperty<double>(element, "ZoomX");

        element.ZoomIn(skipTransitions: true);

        var newZoom = GetProperty<double>(element, "ZoomX");
        Assert.IsTrue(newZoom > initialZoom);
    }

    [TestMethod]
    public async Task ZoomBorder_ZoomOut_ViaAppiumElement()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));

        element.ZoomIn(skipTransitions: true);
        var zoomAfterIn = GetProperty<double>(element, "ZoomX");

        element.ZoomOut(skipTransitions: true);
        var zoomAfterOut = GetProperty<double>(element, "ZoomX");

        Assert.IsTrue(zoomAfterOut < zoomAfterIn);
    }

    [TestMethod]
    public async Task ZoomBorder_Pan_ViaAppiumElement()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        var initialOffsetX = GetProperty<double>(element, "OffsetX");
        var initialOffsetY = GetProperty<double>(element, "OffsetY");

        element.PanDelta(50, 30, skipTransitions: true);

        var newOffsetX = GetProperty<double>(element, "OffsetX");
        var newOffsetY = GetProperty<double>(element, "OffsetY");

        Assert.AreNotEqual(initialOffsetX, newOffsetX);
        Assert.AreNotEqual(initialOffsetY, newOffsetY);
    }

    [TestMethod]
    public async Task ZoomBorder_ResetMatrix_ViaAppiumElement()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));

        element.ZoomIn(skipTransitions: true);
        element.PanDelta(50, 30, skipTransitions: true);

        element.ResetMatrix(skipTransitions: true);

        Assert.AreEqual(1.0, GetProperty<double>(element, "ZoomX"));
        Assert.AreEqual(1.0, GetProperty<double>(element, "ZoomY"));
        Assert.AreEqual(0.0, GetProperty<double>(element, "OffsetX"));
        Assert.AreEqual(0.0, GetProperty<double>(element, "OffsetY"));
    }

    #endregion

    #region ZoomBorder Constraint Tests via Appium API

    [TestMethod]
    public async Task ZoomBorder_MinZoom_Constraint()
    {
        var zoomBorder = await CreateAndLoadAsync(configure: zb =>
        {
            zb.MinZoomX = 0.5;
            zb.MinZoomY = 0.5;
        });

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));

        for (var i = 0; i < 10; i++)
        {
            element.ZoomOut(skipTransitions: true);
        }

        var zoomX = GetProperty<double>(element, "ZoomX");
        var zoomY = GetProperty<double>(element, "ZoomY");

        Assert.IsTrue(zoomX >= 0.5);
        Assert.IsTrue(zoomY >= 0.5);
    }

    [TestMethod]
    public async Task ZoomBorder_MaxZoom_Constraint()
    {
        var zoomBorder = await CreateAndLoadAsync(configure: zb =>
        {
            zb.MaxZoomX = 4.0;
            zb.MaxZoomY = 4.0;
        });

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));

        for (var i = 0; i < 20; i++)
        {
            element.ZoomIn(skipTransitions: true);
        }

        var zoomX = GetProperty<double>(element, "ZoomX");
        var zoomY = GetProperty<double>(element, "ZoomY");

        Assert.IsTrue(zoomX <= 4.0);
        Assert.IsTrue(zoomY <= 4.0);
    }

    #endregion

    #region ZoomBorder Events via Appium API

    [TestMethod]
    public async Task ZoomBorder_ZoomChanged_EventRaised()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var eventRaised = false;
        zoomBorder.ZoomChanged += (s, e) => eventRaised = true;

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        element.ZoomIn(skipTransitions: true);

        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public async Task ZoomBorder_MatrixChanged_EventRaised()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var initialZoomX = zoomBorder.ZoomX;
        var initialMatrix = zoomBorder.Matrix;

        // Use ZoomIn via the command which modifies the matrix (the MatrixChanged event itself is only raised
        // by SetMatrix, on both Avalonia and Uno, so the Avalonia test verifies the matrix/zoom change).
        zoomBorder.ZoomInCommand.Execute(null);

        Assert.AreNotEqual(initialZoomX, zoomBorder.ZoomX);
        Assert.AreNotEqual(initialMatrix, zoomBorder.Matrix);
    }

    #endregion

    #region Focus and Keyboard Tests

    [TestMethod]
    public async Task Element_Focus_ZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        var focused = element.Focus(FocusState.Programmatic);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(focused);
        Assert.AreNotEqual(FocusState.Unfocused, element.FocusState);
    }

    [TestMethod]
    public async Task Element_PressKey_ZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync(configure: zb => zb.EnableKeyboardNavigation = true);

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        element.Focus(FocusState.Programmatic);

        // Keyboard injection is not implemented by Uno on Skia: keys are routed through the control key handler.
        Assert.IsTrue(ZoomBorderTestHelper.PressKey(element, VirtualKey.Right));
        Assert.IsTrue(ZoomBorderTestHelper.PressKey(element, VirtualKey.Down));

        Assert.AreEqual(element.KeyboardPanStep, Math.Abs(element.OffsetX), 1e-9);
        Assert.AreEqual(element.KeyboardPanStep, Math.Abs(element.OffsetY), 1e-9);
    }

    #endregion

    #region PageSource Tests

    [TestMethod]
    public async Task Driver_PageSource_ContainsZoomBorder()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // Driver.PageSource -> XML like dump of the visual tree.
        var pageSource = GetVisualTreeSource(Root(zoomBorder));

        StringAssert.Contains(pageSource, "ZoomBorder");
        StringAssert.Contains(pageSource, "TestZoomBorder");
    }

    [TestMethod]
    public async Task Driver_PageSource_ContainsContent()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var pageSource = GetVisualTreeSource(Root(zoomBorder));

        StringAssert.Contains(pageSource, "ContentBorder");
        StringAssert.Contains(pageSource, "ContentText");
    }

    #endregion

    #region Element Count and Exists Tests

    [TestMethod]
    public async Task Driver_ElementExists_ZoomBorder_ReturnsTrue()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var exists = Descendants(Root(zoomBorder)).OfType<ZoomBorder>().Any();

        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task Driver_ElementExists_NonExistent_ReturnsFalse()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var exists = FindByName(Root(zoomBorder), "NonExistentElement") != null;

        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task Driver_ElementCount_Borders()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var count = FindByTypeName(Root(zoomBorder), "Border").Count;

        Assert.IsTrue(count >= 2); // ZoomBorder template border (PART_Border) and ContentBorder
    }

    #endregion

    #region Async Operations

    [TestMethod]
    public async Task TouchAction_PerformAsync_Completes()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var tapped = 0;
        zoomBorder.AddHandler(UIElement.TappedEvent, new TappedEventHandler((_, _) => tapped++), true);

        TouchTap(Center(zoomBorder));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(1, tapped);
        AssertUnchanged(zoomBorder);
    }

    [TestMethod]
    public async Task TouchAction_ChainedGestures_PerformAsync()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // Press, Wait(100), MoveBy(50, 0), Wait(100), Release with real touch input.
        var center = Center(zoomBorder);
        using (var touch = BeginTouch())
        {
            var id = NextTouchId();
            touch.Inject(TouchSession.Down(id, center));
            await Task.Delay(100);
            for (var i = 1; i <= 10; i++)
            {
                touch.Inject(TouchSession.Move(id, new Point(center.X + 5 * i, center.Y)));
            }

            await Task.Delay(100);
            touch.Inject(TouchSession.Up(id, new Point(center.X + 50, center.Y)));
        }

        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(50.0, zoomBorder.OffsetX, ZoomBorderTestHelper.TouchPanTolerance());
        Assert.AreEqual(0.0, zoomBorder.OffsetY, 1.0);
    }

    #endregion

    #region TryFindElement Tests

    [TestMethod]
    public async Task Driver_TryFindElement_Exists_ReturnsElement()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = Descendants(Root(zoomBorder)).OfType<ZoomBorder>().FirstOrDefault();

        Assert.IsNotNull(element);
    }

    [TestMethod]
    public async Task Driver_TryFindElement_NotExists_ReturnsNull()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = FindByName(Root(zoomBorder), "NonExistent");

        Assert.IsNull(element);
    }

    #endregion

    #region ZoomBorder Stretch Mode Tests

    [TestMethod]
    public async Task ZoomBorder_SetStretch_None()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        SetProperty(element, "Stretch", StretchMode.None);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(StretchMode.None, zoomBorder.Stretch);
        Assert.AreEqual(1.0, zoomBorder.ZoomX, 1e-9);
    }

    [TestMethod]
    public async Task ZoomBorder_SetStretch_Fill()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        SetProperty(element, "Stretch", StretchMode.Fill);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(StretchMode.Fill, zoomBorder.Stretch);
    }

    [TestMethod]
    public async Task ZoomBorder_SetStretch_Uniform()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));
        SetProperty(element, "Stretch", StretchMode.Uniform);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(StretchMode.Uniform, zoomBorder.Stretch);
    }

    #endregion

    #region ZoomBorder ZoomToRectangle Tests

    [TestMethod]
    public async Task ZoomBorder_ZoomToRectangle_ViaAppiumElement()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));

        element.ZoomToRectangle(new Rect(50, 50, 100, 100), animate: false);

        var zoomX = GetProperty<double>(element, "ZoomX");
        Assert.AreNotEqual(1.0, zoomX);
    }

    #endregion

    #region ZoomBorder CenterOn Tests

    [TestMethod]
    public async Task ZoomBorder_CenterOn_ViaAppiumElement()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var element = UIHelper.GetChild<ZoomBorder>(Root(zoomBorder));

        element.CenterOn(new Point(100, 75), zoom: 2.0, animate: false);

        var zoomX = GetProperty<double>(element, "ZoomX");
        Assert.AreEqual(2.0, zoomX);
    }

    #endregion

    #region Driver Registration Tests

    [TestMethod]
    public async Task Driver_RegisterPredicate_CustomLocator()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // Driver.RegisterPredicate + By.Predicate -> a named custom predicate over the visual tree.
        var predicates = new Dictionary<string, Func<FrameworkElement, bool>>
        {
            ["ZoomEnabled"] = c => c is ZoomBorder zb && zb.EnableZoom
        };

        var element = FindFirst(Root(zoomBorder), predicates["ZoomEnabled"]);

        Assert.IsNotNull(element);
        Assert.IsInstanceOfType(element, typeof(ZoomBorder));
    }

    #endregion

    #region Touch Action Variations

    [TestMethod]
    public async Task TouchAction_Press_WithCoordinates()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // Press(100, 100), Wait(50), Release.
        await TouchHoldAsync(Window(zoomBorder, 100, 100), 50);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        AssertUnchanged(zoomBorder);
    }

    [TestMethod]
    public async Task TouchAction_Tap_MultipleCount()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var tapped = 0;
        var doubleTapped = 0;
        zoomBorder.AddHandler(UIElement.TappedEvent, new TappedEventHandler((_, _) => tapped++), true);
        zoomBorder.AddHandler(UIElement.DoubleTappedEvent, new DoubleTappedEventHandler((_, _) => doubleTapped++), true);

        // Triple tap
        TouchMultiTap(Center(zoomBorder), 3);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(tapped >= 1, $"Tapped={tapped}");

        // The Uno gesture recognizer reports a DoubleTapped for every tap that follows a tap of the same pointer
        // (taps 2 and 3 of a triple tap). With the default double click zoom (ZoomInOut, factor 2) the first double
        // tap zooms in to 2 and the second one resets (zoom >= 1.5 threshold).
#if PANANDZOOM_WINUI
        // WinUI: the Windows gesture recognizer reports a single DoubleTapped for a triple tap (the third tap
        // starts a new tap sequence), so the double click zoom zooms in once.
        Assert.AreEqual(1, doubleTapped);
        Assert.AreEqual(2.0, zoomBorder.ZoomX, 1e-9);
#else
        Assert.AreEqual(2, doubleTapped);
        Assert.AreEqual(1.0, zoomBorder.ZoomX, 1e-9);
#endif
    }

    [TestMethod]
    public async Task TouchAction_Swipe_FullPath()
    {
        var zoomBorder = await CreateAndLoadAsync();

        // Swipe(100, 100, 200, 150, 300ms)
        await TouchDragAsync(Window(zoomBorder, 100, 100), Window(zoomBorder, 200, 150), steps: 10, stepDelayMilliseconds: 30);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(100.0, zoomBorder.OffsetX, ZoomBorderTestHelper.TouchPanTolerance());
        Assert.AreEqual(50.0, zoomBorder.OffsetY, ZoomBorderTestHelper.TouchPanTolerance());
    }

    #endregion

    #region ExpectedConditions Tests

    [TestMethod]
    public async Task ExpectedConditions_ElementToBeClickable()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var root = Root(zoomBorder);

        ZoomBorder? element = null;
        var clickable = await ZoomBorderTestHelper.WaitForAsync(
            () => (element = Descendants(root).OfType<ZoomBorder>().FirstOrDefault()) is { } zb && IsDisplayed(zb) && zb.IsEnabled && zb.IsHitTestVisible,
            5000);

        Assert.IsTrue(clickable);
        Assert.IsNotNull(element);
        Assert.IsTrue(element.IsEnabled);
    }

    [TestMethod]
    public async Task ExpectedConditions_AttributeToBe()
    {
        var zoomBorder = await CreateAndLoadAsync();
        var root = Root(zoomBorder);

        var result = await ZoomBorderTestHelper.WaitForAsync(
            () => Descendants(root).OfType<ZoomBorder>().FirstOrDefault() is { } zb && GetProperty<string>(zb, "Name") == "TestZoomBorder",
            5000);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ExpectedConditions_NumberOfElementsToBe()
    {
        var zoomBorder = await CreateAndLoadAsync();

        var elements = Descendants(Root(zoomBorder)).OfType<TextBlock>().ToList();

        Assert.IsNotNull(elements);
        Assert.IsTrue(elements.Count >= 1, $"Expected at least 1 TextBlock, found {elements.Count}");
    }

    #endregion
}
