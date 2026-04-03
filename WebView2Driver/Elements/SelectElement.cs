// ============================================================
//  SelectElement.cs
//  Helper for interacting with <select> dropdowns.
//  Analogous to Selenium's OpenQA.Selenium.Support.UI.SelectElement.
// ============================================================
using System.Text.Json;
using WebView2Driver.Elements;

namespace WebView2Driver.Elements;

/// <summary>
/// Provides convenience methods for interacting with HTML
/// <c>&lt;select&gt;</c> elements.
/// </summary>
public sealed class SelectElement
{
    private readonly WebView2Element _element;
    private readonly Core.WebView2Driver _driver;

    public SelectElement(WebView2Element element)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
        _driver  = element.Driver;
        string tag = element.TagName;
        if (!tag.Equals("select", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"SelectElement requires a <select> element, got <{tag}>.");
    }

    // ── Select actions ────────────────────────────────────────

    /// <summary>Selects the option whose visible text equals <paramref name="text"/>.</summary>
    public void SelectByText(string text)
    {
        string jt = JsStr(text);
        int changed = _driver.ExecuteScript<int?>($@"
            var sel = {_element.JsRef};
            for(var i=0;i<sel.options.length;i++){{
                if((sel.options[i].text||'').trim()==={jt}){{
                    sel.selectedIndex=i;
                    sel.dispatchEvent(new Event('change',{{bubbles:true}}));
                    return 1;
                }}
            }}
            return 0;
        ") ?? 0;
        if (changed == 0) throw new InvalidOperationException($"No option with text '{text}' found.");
    }

    /// <summary>Selects the option whose <c>value</c> attribute equals <paramref name="value"/>.</summary>
    public void SelectByValue(string value)
    {
        string jv = JsStr(value);
        int changed = _driver.ExecuteScript<int?>($@"
            var sel = {_element.JsRef};
            for(var i=0;i<sel.options.length;i++){{
                if(sel.options[i].value==={jv}){{
                    sel.selectedIndex=i;
                    sel.dispatchEvent(new Event('change',{{bubbles:true}}));
                    return 1;
                }}
            }}
            return 0;
        ") ?? 0;
        if (changed == 0) throw new InvalidOperationException($"No option with value '{value}' found.");
    }

    /// <summary>Selects the option at zero-based <paramref name="index"/>.</summary>
    public void SelectByIndex(int index)
    {
        _driver.ExecuteScript($@"
            var sel = {_element.JsRef};
            sel.selectedIndex = {index};
            sel.dispatchEvent(new Event('change',{{bubbles:true}}));
        ");
    }

    /// <summary>Deselects all options (only valid for multi-select).</summary>
    public void DeselectAll()
    {
        _driver.ExecuteScript($@"
            var sel = {_element.JsRef};
            for(var i=0;i<sel.options.length;i++) sel.options[i].selected=false;
            sel.dispatchEvent(new Event('change',{{bubbles:true}}));
        ");
    }

    /// <summary>Deselects option by visible text.</summary>
    public void DeselectByText(string text)
    {
        string jt = JsStr(text);
        _driver.ExecuteScript($@"
            var sel = {_element.JsRef};
            for(var i=0;i<sel.options.length;i++)
                if((sel.options[i].text||'').trim()==={jt}) sel.options[i].selected=false;
            sel.dispatchEvent(new Event('change',{{bubbles:true}}));
        ");
    }

    /// <summary>Deselects option by value attribute.</summary>
    public void DeselectByValue(string value)
    {
        string jv = JsStr(value);
        _driver.ExecuteScript($@"
            var sel = {_element.JsRef};
            for(var i=0;i<sel.options.length;i++)
                if(sel.options[i].value==={jv}) sel.options[i].selected=false;
            sel.dispatchEvent(new Event('change',{{bubbles:true}}));
        ");
    }

    // ── Query properties ──────────────────────────────────────

    /// <summary>All available options.</summary>
    public IReadOnlyList<WebView2Element> Options
    {
        get
        {
            int count = _driver.ExecuteScript<int?>($"return {_element.JsRef}.options.length;") ?? 0;
            var result = new List<WebView2Element>(count);
            for (int i = 0; i < count; i++)
                result.Add(new WebView2Element(_driver, $"{_element.JsRef}.options[{i}]"));
            return result;
        }
    }

    /// <summary>All currently selected options.</summary>
    public IReadOnlyList<WebView2Element> AllSelectedOptions
    {
        get
        {
            int count = _driver.ExecuteScript<int?>($"return {_element.JsRef}.options.length;") ?? 0;
            var result = new List<WebView2Element>();
            for (int i = 0; i < count; i++)
            {
                bool sel = _driver.ExecuteScript<bool?>($"return {_element.JsRef}.options[{i}].selected;") ?? false;
                if (sel) result.Add(new WebView2Element(_driver, $"{_element.JsRef}.options[{i}]"));
            }
            return result;
        }
    }

    /// <summary>First selected option (or throws if none).</summary>
    public WebView2Element SelectedOption
    {
        get
        {
            int idx = _driver.ExecuteScript<int?>($"return {_element.JsRef}.selectedIndex;") ?? -1;
            if (idx < 0) throw new InvalidOperationException("No option is selected.");
            return new WebView2Element(_driver, $"{_element.JsRef}.options[{idx}]");
        }
    }

    /// <summary><see langword="true"/> if the select allows multiple selections.</summary>
    public bool IsMultiple =>
        _driver.ExecuteScript<bool?>($"return !!{_element.JsRef}.multiple;") ?? false;

    private static string JsStr(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
