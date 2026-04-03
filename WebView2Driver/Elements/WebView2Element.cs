// ============================================================
//  WebView2Element.cs
//  Represents a single DOM element — analogous to IWebElement.
// ============================================================
using System.Drawing;
using System.Text.Json;
using WebView2Driver.Browser;

namespace WebView2Driver.Elements;

/// <summary>
/// Wraps a DOM element reference and exposes a Selenium-compatible
/// element interaction API.  All interactions go through JavaScript
/// executed in the <see cref="Core.WebView2Driver"/>.
/// </summary>
public sealed class WebView2Element
{
    // ── Internals ─────────────────────────────────────────────
    private readonly Core.WebView2Driver _driver;
    private readonly string _jsRef;   // JS expression that resolves to this element

    internal WebView2Element(Core.WebView2Driver driver, string jsRef)
    {
        _driver = driver;
        _jsRef  = jsRef;
    }

    // ── Identity ─────────────────────────────────────────────

    /// <summary>JS reference expression (for composability).</summary>
    internal string JsRef => _jsRef;

    /// <summary>Access to the owning driver.</summary>
    internal Core.WebView2Driver Driver => _driver;

    // ── Text content ─────────────────────────────────────────

    /// <summary>Visible text of the element.</summary>
    public string Text =>
        _driver.ExecuteScript<string>($"return ({_jsRef}).innerText || ({_jsRef}).textContent || '';") ?? "";

    // ── Tag name ─────────────────────────────────────────────

    /// <summary>Lower-case tag name (e.g. <c>"input"</c>).</summary>
    public string TagName =>
        (_driver.ExecuteScript<string>($"return ({_jsRef}).tagName;") ?? "").ToLowerInvariant();

    // ── State ────────────────────────────────────────────────

    /// <summary>Whether the element is displayed (not hidden or display:none).</summary>
    public bool Displayed
    {
        get
        {
            var result = _driver.ExecuteScript<JsonElement?>(@$"
                (function() {{
                    var e = {_jsRef};
                    if (!e) return false;
                    var s = window.getComputedStyle(e);
                    return s.display !== 'none' && s.visibility !== 'hidden' && s.opacity !== '0';
                }})();
            ");
            return result?.GetBoolean() ?? false;
        }
    }

    /// <summary>Whether the element is enabled (not disabled).</summary>
    public bool Enabled =>
        !(_driver.ExecuteScript<bool?>($"return !!({_jsRef}).disabled;") ?? false);

    /// <summary>Whether the element (checkbox/radio/option) is selected.</summary>
    public bool Selected =>
        _driver.ExecuteScript<bool?>($"return !!({_jsRef}).checked || !!({_jsRef}).selected;") ?? false;

    // ── Location & Size ──────────────────────────────────────

    /// <summary>Top-left corner of the element relative to the viewport.</summary>
    public Point Location
    {
        get
        {
            var r = _driver.ExecuteScript<JsonElement?>($"(function(){{ var r=({_jsRef}).getBoundingClientRect(); return {{x:Math.round(r.left),y:Math.round(r.top)}}; }})()");
            if (r is { } je && je.ValueKind != JsonValueKind.Null)
                return new Point(je.GetProperty("x").GetInt32(), je.GetProperty("y").GetInt32());
            return Point.Empty;
        }
    }

    /// <summary>Width and height of the element.</summary>
    public Size Size
    {
        get
        {
            var r = _driver.ExecuteScript<JsonElement?>($"(function(){{ var r=({_jsRef}).getBoundingClientRect(); return {{w:Math.round(r.width),h:Math.round(r.height)}}; }})()");
            if (r is { } je && je.ValueKind != JsonValueKind.Null)
                return new Size(je.GetProperty("w").GetInt32(), je.GetProperty("h").GetInt32());
            return Size.Empty;
        }
    }

    // ── Interactions ─────────────────────────────────────────

    /// <summary>Scrolls the element into view then dispatches a click event.</summary>
    public void Click()
    {
        _driver.ExecuteScript($"({_jsRef}).scrollIntoView({{block:'center'}});({_jsRef}).click();");
    }

    /// <summary>Clears the value of a text input or textarea.</summary>
    public void Clear()
    {
        _driver.ExecuteScript($"({_jsRef}).focus();({_jsRef}).value='';({_jsRef}).dispatchEvent(new Event('input',{{bubbles:true}}));");
    }

    /// <summary>
    /// Types <paramref name="text"/> into the element character-by-character,
    /// firing keydown / keypress / input / keyup events.
    /// </summary>
    public void SendKeys(string text)
    {
        if (text == null) return;
        
        // Focus the element first using JS
        _driver.ExecuteScript($"({_jsRef}).focus();");
        
        foreach (var c in text)
        {
            var ch = c.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"");
            bool isEnter = (c == '\n' || c == '\r' || c == '\uE006' || c == '\uE007');

            if (isEnter)
            {
                _driver.ExecuteScript($@"
                    (function(){{
                        var el = {_jsRef};
                        var opts = {{key:'Enter', code:'Enter', keyCode:13, which:13, bubbles:true}};
                        el.dispatchEvent(new KeyboardEvent('keydown',  opts));
                        el.dispatchEvent(new KeyboardEvent('keypress', opts));
                        // Google search often triggers on keydown or keypress, but let's be thorough
                        el.dispatchEvent(new KeyboardEvent('keyup',    opts));
                    }})();
                ");
            }
            else if (c >= '\uE000' && c <= '\uE03D')
            {
                // Skip other control keys for JS fallback for now
            }
            else
            {
                _driver.ExecuteScript($@"
                    (function(){{
                        var el = {_jsRef};
                        var val = '{ch}';
                        var opts = {{key:val, bubbles:true}};
                        el.dispatchEvent(new KeyboardEvent('keydown',  opts));
                        el.dispatchEvent(new KeyboardEvent('keypress', opts));
                        if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA') {{
                            var start = el.selectionStart;
                            el.value = el.value.substring(0, start) + val + el.value.substring(el.selectionEnd);
                            el.selectionStart = el.selectionEnd = start + 1;
                        }}
                        el.dispatchEvent(new Event('input', {{bubbles:true}}));
                        el.dispatchEvent(new KeyboardEvent('keyup',    opts));
                    }})();
                ");
            }
        }
        
        // Final change event via JS
        _driver.ExecuteScript($"({_jsRef}).dispatchEvent(new Event('change',{{bubbles:true}}));");
    }

    /// <summary>Submits the form that contains this element.</summary>
    public void Submit()
    {
        _driver.ExecuteScript($@"
            (function(){{
                var el = {_jsRef};
                var form = el.closest('form') || el.form;
                if (form) form.submit();
                else el.dispatchEvent(new Event('submit', {{bubbles:true, cancelable:true}}));
            }})();
        ");
    }

    // ── Attributes & Properties ──────────────────────────────

    /// <summary>Returns the value of an HTML attribute (or <see langword="null"/>).</summary>
    public string? GetAttribute(string name)
    {
        string jn = JsStr(name);
        return _driver.ExecuteScript<string?>($"return ({_jsRef}).getAttribute({jn});");
    }

    /// <summary>Returns the value of a DOM property.</summary>
    public string? GetProperty(string name)
    {
        string jn = JsStr(name);
        return _driver.ExecuteScript<string?>($"var v=({_jsRef})[{jn}];return v==null?null:String(v);");
    }

    /// <summary>Returns the computed CSS value for a given property name.</summary>
    public string GetCssValue(string propertyName)
    {
        string jn = JsStr(propertyName);
        return _driver.ExecuteScript<string?>($"return window.getComputedStyle({_jsRef})[{jn}];") ?? "";
    }

    // ── Child element lookups ─────────────────────────────────

    /// <summary>Finds a child element matching <paramref name="by"/>.</summary>
    public WebView2Element FindElement(By by)
    {
        string js = by.ToJsSingleExpression($"({_jsRef})");
        var result = _driver.ExecuteScript<JsonElement?>($"return {js};");
        if (result == null || result.Value.ValueKind == JsonValueKind.Null)
            throw new NoSuchElementException($"Child element not found: {by}");

        // Re-root the JS ref relative to this element
        return new WebView2Element(_driver, $"({_jsRef}).querySelector({JsStr(by.Value)})");
    }

    /// <summary>Finds all child elements matching <paramref name="by"/>.</summary>
    public IReadOnlyList<WebView2Element> FindElements(By by)
    {
        string js = by.ToJsArrayExpression($"({_jsRef})");
        var count = _driver.ExecuteScript<int?>($"return ({js}).length;") ?? 0;
        var list  = new List<WebView2Element>(count);
        for (int i = 0; i < count; i++)
            list.Add(new WebView2Element(_driver, $"({js})[{i}]"));
        return list;
    }

    // ── Value property shortcut ──────────────────────────────

    /// <summary>Returns the <c>value</c> property (for inputs).</summary>
    public string Value => GetProperty("value") ?? "";

    // ── Screenshot ───────────────────────────────────────────

    /// <summary>Takes a screenshot cropped to this element's bounds.</summary>
    public async Task<System.Drawing.Bitmap> TakeScreenshotAsync()
    {
        var full = await _driver.Screenshot.CaptureAsync();
        var loc  = Location;
        var sz   = Size;
        var rect = Rectangle.Intersect(new Rectangle(loc.X, loc.Y, sz.Width, sz.Height), new Rectangle(0, 0, full.Width, full.Height));
        if (rect.Width == 0 || rect.Height == 0) return new Bitmap(1, 1);
        return full.Clone(rect, full.PixelFormat);
    }

    // ── Scroll into view ─────────────────────────────────────

    /// <summary>Scrolls the element into the visible area of the browser window.</summary>
    public void ScrollIntoView() =>
        _driver.ExecuteScript($"({_jsRef}).scrollIntoView({{behavior:'smooth',block:'center'}});");

    // ── Private helpers ──────────────────────────────────────

    private static string JsStr(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    public override string ToString() => $"WebView2Element[{_jsRef}]";
}

/// <summary>Thrown when an element cannot be found.</summary>
public class NoSuchElementException : Exception
{
    public NoSuchElementException(string message) : base(message) { }
}
