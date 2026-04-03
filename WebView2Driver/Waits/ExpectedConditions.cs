// ============================================================
//  ExpectedConditions.cs
//  Built-in wait conditions — mirrors Selenium ExpectedConditions.
// ============================================================
using WebView2Driver.Elements;

namespace WebView2Driver.Waits;

/// <summary>
/// Pre-built condition predicates for use with
/// <see cref="WebDriverWait"/> and <see cref="FluentWait"/>.
/// Mirrors Selenium's <c>ExpectedConditions</c> class.
/// </summary>
public static class ExpectedConditions
{
    // ── Title ─────────────────────────────────────────────────

    public static Func<Core.WebView2Driver, bool> TitleIs(string title) =>
        d => d.Title == title;

    public static Func<Core.WebView2Driver, bool> TitleContains(string fragment) =>
        d => d.Title?.Contains(fragment, StringComparison.OrdinalIgnoreCase) == true;

    // ── URL ───────────────────────────────────────────────────

    public static Func<Core.WebView2Driver, bool> UrlIs(string url) =>
        d => d.Url == url;

    public static Func<Core.WebView2Driver, bool> UrlContains(string fragment) =>
        d => d.Url?.Contains(fragment, StringComparison.OrdinalIgnoreCase) == true;

    public static Func<Core.WebView2Driver, bool> UrlMatches(string pattern) =>
        d => System.Text.RegularExpressions.Regex.IsMatch(d.Url ?? "", pattern);

    // ── Elements ──────────────────────────────────────────────

    /// <summary>Waits until the element exists in the DOM (may be hidden).</summary>
    public static Func<Core.WebView2Driver, WebView2Element?> ElementExists(By by) =>
        d =>
        {
            try   { return d.FindElement(by); }
            catch { return null; }
        };

    /// <summary>Waits until the element exists AND is visible.</summary>
    public static Func<Core.WebView2Driver, WebView2Element?> ElementIsVisible(By by) =>
        d =>
        {
            try
            {
                var el = d.FindElement(by);
                return el.Displayed ? el : null;
            }
            catch { return null; }
        };

    /// <summary>Waits until the element is visible and enabled (clickable).</summary>
    public static Func<Core.WebView2Driver, WebView2Element?> ElementToBeClickable(By by) =>
        d =>
        {
            try
            {
                var el = d.FindElement(by);
                return (el.Displayed && el.Enabled) ? el : null;
            }
            catch { return null; }
        };

    public static Func<Core.WebView2Driver, WebView2Element?> ElementToBeClickable(WebView2Element element) =>
        _ => (element.Displayed && element.Enabled) ? element : null;

    /// <summary>Waits for the element to become invisible / removed.</summary>
    public static Func<Core.WebView2Driver, bool> InvisibilityOfElement(By by) =>
        d =>
        {
            try   { return !d.FindElement(by).Displayed; }
            catch { return true; }        // No longer in DOM = invisible
        };

    public static Func<Core.WebView2Driver, bool> InvisibilityOfElementWithText(By by, string text) =>
        d =>
        {
            try
            {
                var el = d.FindElement(by);
                return !el.Displayed || !el.Text.Contains(text);
            }
            catch { return true; }
        };

    /// <summary>Waits for the element's inner-text to contain <paramref name="text"/>.</summary>
    public static Func<Core.WebView2Driver, bool> TextToBePresentInElement(By by, string text) =>
        d =>
        {
            try   { return d.FindElement(by).Text.Contains(text); }
            catch { return false; }
        };

    public static Func<Core.WebView2Driver, bool> TextToBePresentInElementValue(By by, string text) =>
        d =>
        {
            try   { return (d.FindElement(by).GetAttribute("value") ?? "").Contains(text); }
            catch { return false; }
        };

    /// <summary>Waits for element to be selected (e.g. checkbox).</summary>
    public static Func<Core.WebView2Driver, bool> ElementToBeSelected(By by) =>
        d =>
        {
            try   { return d.FindElement(by).Selected; }
            catch { return false; }
        };

    /// <summary>Waits for element to no longer be attached to DOM (stale).</summary>
    public static Func<Core.WebView2Driver, bool> StalenessOf(WebView2Element element) =>
        d =>
        {
            try
            {
                var tag = element.TagName; // Accessing any property will throw if stale
                return false;
            }
            catch { return true; }
        };

    // ── Frames ────────────────────────────────────────────────

    /// <summary>Switches to the named frame and returns <see langword="true"/> once available.</summary>
    public static Func<Core.WebView2Driver, bool> FrameToBeAvailableAndSwitchToIt(string nameOrId) =>
        d =>
        {
            try   { d.SwitchTo().Frame(nameOrId); return true; }
            catch { return false; }
        };

    public static Func<Core.WebView2Driver, bool> FrameToBeAvailableAndSwitchToIt(int index) =>
        d =>
        {
            try   { d.SwitchTo().Frame(index); return true; }
            catch { return false; }
        };

    // ── Alerts ────────────────────────────────────────────────

    /// <summary>Returns the alert text if a JS alert is present, else null.</summary>
    public static Func<Core.WebView2Driver, string?> AlertIsPresent() =>
        d => d.SwitchTo().AlertIsPresent() ? d.SwitchTo().Alert().Text : null;

    // ── Page readiness ────────────────────────────────────────

    /// <summary>Waits until <c>document.readyState === 'complete'</c>.</summary>
    public static Func<Core.WebView2Driver, bool> DocumentReadyStateIsComplete() =>
        d => d.ExecuteScript<string?>("return document.readyState;") == "complete";

    // ── Number of elements ────────────────────────────────────

    public static Func<Core.WebView2Driver, IReadOnlyList<WebView2Element>?> NumberOfElementsToBeMoreThan(By by, int count) =>
        d =>
        {
            var els = d.FindElements(by);
            return els.Count > count ? els : null;
        };

    public static Func<Core.WebView2Driver, IReadOnlyList<WebView2Element>?> NumberOfElementsToBe(By by, int count) =>
        d =>
        {
            var els = d.FindElements(by);
            return els.Count == count ? els : null;
        };
}
