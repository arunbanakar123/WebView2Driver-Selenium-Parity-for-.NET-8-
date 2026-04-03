// ============================================================
//  Actions.cs
//  Action chain builder — analogous to Selenium's Actions class.
// ============================================================
using WebView2Driver.Elements;

namespace WebView2Driver.Interactions;

/// <summary>
/// Builds and executes a chain of user-input actions (mouse + keyboard).
/// Mirrors Selenium's <c>Actions</c> class.
/// <para>Finish the chain with <c>Perform()</c>.</para>
/// </summary>
public sealed class Actions
{
    private readonly Core.WebView2Driver _driver;
    private readonly List<Func<Task>> _steps = new();

    public Actions(Core.WebView2Driver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    // ── Mouse ─────────────────────────────────────────────────

    /// <summary>Move the mouse to the centre of a web element.</summary>
    public Actions MoveToElement(WebView2Element element, int offsetX = 0, int offsetY = 0)
    {
        _steps.Add(() =>
        {
            _driver.Mouse.MoveTo(element, offsetX, offsetY);
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>Move the mouse by an offset from its current position.</summary>
    public Actions MoveByOffset(int xOffset, int yOffset)
    {
        _steps.Add(() =>
        {
            _driver.Mouse.MoveByOffset(xOffset, yOffset);
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>Click at the current mouse position (or on <paramref name="element"/> if provided).</summary>
    public Actions Click(WebView2Element? element = null)
    {
        _steps.Add(() =>
        {
            if (element != null) _driver.Mouse.MoveTo(element);
            _driver.Mouse.Click();
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>Double-click at the current position or on the specified element.</summary>
    public Actions DoubleClick(WebView2Element? element = null)
    {
        _steps.Add(() =>
        {
            if (element != null) _driver.Mouse.MoveTo(element);
            _driver.Mouse.DoubleClick();
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>Right-click (context menu) at the current position or element.</summary>
    public Actions ContextClick(WebView2Element? element = null)
    {
        _steps.Add(() =>
        {
            if (element != null) _driver.Mouse.MoveTo(element);
            _driver.Mouse.RightClick();
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>Hold the mouse button down at the current position or element.</summary>
    public Actions ClickAndHold(WebView2Element? element = null)
    {
        _steps.Add(() =>
        {
            if (element != null) _driver.Mouse.MoveTo(element);
            _driver.Mouse.Down();
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>Release a held mouse button.</summary>
    public Actions Release(WebView2Element? element = null)
    {
        _steps.Add(() =>
        {
            if (element != null) _driver.Mouse.MoveTo(element);
            _driver.Mouse.Up();
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>Drag an element and drop it onto another element.</summary>
    public Actions DragAndDrop(WebView2Element source, WebView2Element target)
    {
        _steps.Add(() =>
        {
            // Simulate via JS dragstart / dragover / drop events
            _driver.ExecuteScript($@"
                (function(){{
                    var src = {source.JsRef};
                    var tgt = {target.JsRef};
                    function fire(el,t){{el.dispatchEvent(new DragEvent(t,{{bubbles:true,cancelable:true}}));}}
                    fire(src,'dragstart'); fire(tgt,'dragover'); fire(tgt,'drop'); fire(src,'dragend');
                }})();
            ");
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>Drag an element by a pixel offset.</summary>
    public Actions DragAndDropToOffset(WebView2Element source, int xOffset, int yOffset)
    {
        _steps.Add(() =>
        {
            _driver.ExecuteScript($@"
                (function(){{
                    var src = {source.JsRef};
                    var r = src.getBoundingClientRect();
                    function fire(el,t,x,y){{el.dispatchEvent(new DragEvent(t,{{bubbles:true,cancelable:true,clientX:x,clientY:y}}));}}
                    fire(src,'dragstart',r.left,r.top);
                    fire(document.elementFromPoint(r.left+{xOffset},r.top+{yOffset}),'drop',r.left+{xOffset},r.top+{yOffset});
                    fire(src,'dragend',r.left+{xOffset},r.top+{yOffset});
                }})();
            ");
            return Task.CompletedTask;
        });
        return this;
    }

    // ── Keyboard ─────────────────────────────────────────────

    /// <summary>Press a key (uses <see cref="Keys"/> constants).</summary>
    public Actions KeyDown(string key, WebView2Element? element = null)
    {
        _steps.Add(() =>
        {
            if (element != null) element.Click();
            _driver.Keyboard.PressKey(key);
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>Release a held key.</summary>
    public Actions KeyUp(string key, WebView2Element? element = null)
    {
        _steps.Add(() =>
        {
            _driver.Keyboard.ReleaseKey(key);
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>Send a string of keys; special keys can be passed using <see cref="Keys"/> constants.</summary>
    public Actions SendKeys(string keys, WebView2Element? element = null)
    {
        _steps.Add(() =>
        {
            if (element != null) element.Click();
            _driver.Keyboard.SendKeys(keys);
            return Task.CompletedTask;
        });
        return this;
    }

    // ── Scroll ───────────────────────────────────────────────

    /// <summary>Scrolls the element into view.</summary>
    public Actions ScrollToElement(WebView2Element element)
    {
        _steps.Add(() =>
        {
            element.ScrollIntoView();
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>Scrolls the page by a pixel offset.</summary>
    public Actions ScrollByAmount(int deltaX, int deltaY)
    {
        _steps.Add(() =>
        {
            _driver.ExecuteScript($"window.scrollBy({deltaX},{deltaY});");
            return Task.CompletedTask;
        });
        return this;
    }

    // ── Pause ────────────────────────────────────────────────

    /// <summary>Inserts a fixed pause between actions.</summary>
    public Actions Pause(TimeSpan duration)
    {
        _steps.Add(async () => await Task.Delay(duration));
        return this;
    }
    public Actions Pause(int milliseconds) => Pause(TimeSpan.FromMilliseconds(milliseconds));

    // ── Execute ───────────────────────────────────────────────

    /// <summary>
    /// Executes all queued actions in order.
    /// </summary>
    public void Perform()
    {
        foreach (var step in _steps)
            step().GetAwaiter().GetResult();
        _steps.Clear();
    }

    /// <summary>Clears the action queue without executing.</summary>
    public void Release() => _steps.Clear();
}
