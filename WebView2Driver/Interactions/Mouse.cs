// ============================================================
//  Mouse.cs
//  Raw mouse simulation — analogous to Selenium's IMouse.
// ============================================================
using WebView2Driver.Elements;

namespace WebView2Driver.Interactions;

/// <summary>
/// Exposes low-level mouse simulation via Chromium DevTools Protocol (CDP).
/// </summary>
public sealed class Mouse
{
    private readonly Core.WebView2Driver _driver;
    private int _x;
    private int _y;

    internal Mouse(Core.WebView2Driver driver)
    {
        _driver = driver;
    }

    private void DispatchMouse(string type, string button = "left", int clickCount = 1)
    {
        _driver.DevTools.CallMethodAsync("Input.dispatchMouseEvent", new
        {
            type = type,
            button = button,
            x = _x,
            y = _y,
            clickCount = clickCount
        }).GetAwaiter().GetResult();
    }

    /// <summary>Moves mouse to the absolute page coordinates.</summary>
    public void MoveToAbsolute(int x, int y)
    {
        _x = x;
        _y = y;
        DispatchMouse("mouseMoved", "none", 0);
    }

    /// <summary>Moves mouse to the centre of the given element.</summary>
    public void MoveTo(WebView2Element element, int offsetX = 0, int offsetY = 0)
    {
        element.ScrollIntoView();
        var loc = element.Location;
        var sz  = element.Size;
        MoveToAbsolute(loc.X + (sz.Width / 2) + offsetX, loc.Y + (sz.Height / 2) + offsetY);
    }

    /// <summary>Moves mouse by an offset from its current location.</summary>
    public void MoveByOffset(int offsetX, int offsetY) =>
        MoveToAbsolute(_x + offsetX, _y + offsetY);

    /// <summary>Presses the mouse button down.</summary>
    public void Down(string button = "left") => DispatchMouse("mousePressed", button, 1);

    /// <summary>Releases the mouse button.</summary>
    public void Up(string button = "left") => DispatchMouse("mouseReleased", button, 1);

    /// <summary>Performs a single click.</summary>
    public void Click(string button = "left")
    {
        Down(button);
        Up(button);
    }

    /// <summary>Performs a double click.</summary>
    public void DoubleClick()
    {
        DispatchMouse("mousePressed", "left", 1);
        DispatchMouse("mouseReleased", "left", 1);
        DispatchMouse("mousePressed", "left", 2);
        DispatchMouse("mouseReleased", "left", 2);
    }

    /// <summary>Performs a right-click.</summary>
    public void RightClick() => Click("right");
}
