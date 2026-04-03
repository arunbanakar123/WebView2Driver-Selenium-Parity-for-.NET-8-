// ============================================================
//  By.cs
//  Selector strategies — analogous to Selenium By class.
// ============================================================
namespace WebView2Driver.Elements;

/// <summary>
/// Strategy used to locate elements.
/// </summary>
public enum ByStrategy
{
    CssSelector,
    XPath,
    Id,
    Name,
    TagName,
    ClassName,
    LinkText,
    PartialLinkText
}

/// <summary>
/// Factory for element-locator strategies.
/// Mirrors Selenium's static <c>By</c> class.
/// </summary>
public sealed class By
{
    public ByStrategy Strategy { get; }
    public string      Value    { get; }

    private By(ByStrategy strategy, string value)
    {
        Strategy = strategy;
        Value    = value ?? throw new ArgumentNullException(nameof(value));
    }

    // ── Factories ──────────────────────────────────────────

    /// <summary>Locate by HTML <c>id</c> attribute.</summary>
    public static By Id(string id) => new(ByStrategy.Id, id);

    /// <summary>Locate by HTML <c>name</c> attribute.</summary>
    public static By Name(string name) => new(ByStrategy.Name, name);

    /// <summary>Locate by CSS selector.</summary>
    public static By CssSelector(string css) => new(ByStrategy.CssSelector, css);

    /// <summary>Locate by XPath expression.</summary>
    public static By XPath(string xpath) => new(ByStrategy.XPath, xpath);

    /// <summary>Locate by tag name (e.g. <c>"input"</c>).</summary>
    public static By TagName(string tag) => new(ByStrategy.TagName, tag);

    /// <summary>Locate by CSS class name (single, no dot prefix).</summary>
    public static By ClassName(string cls) => new(ByStrategy.ClassName, cls);

    /// <summary>Locate &lt;a&gt; elements whose visible text matches exactly.</summary>
    public static By LinkText(string text) => new(ByStrategy.LinkText, text);

    /// <summary>Locate &lt;a&gt; elements whose visible text contains the given substring.</summary>
    public static By PartialLinkText(string text) => new(ByStrategy.PartialLinkText, text);

    // ── Serialisation helpers ──────────────────────────────

    /// <summary>
    /// Generates the JavaScript expression that resolves this selector
    /// in the context of <paramref name="contextExpr"/>.
    /// Returns a single-element expression (null if not found).
    /// </summary>
    internal string ToJsSingleExpression(string contextExpr = "document")
    {
        string jsValue = JsStr(Value);
        bool isDoc = contextExpr == "document";

        return Strategy switch
        {
            ByStrategy.Id          => isDoc ? $"document.getElementById({jsValue})" : $"({contextExpr}).querySelector('#' + CSS.escape({jsValue}.slice(1, -1)))",
            ByStrategy.Name        => isDoc ? $"document.getElementsByName({jsValue})[0] || null" : $"({contextExpr}).querySelector('[name=\"' + {jsValue}.slice(1, -1).replace(/\"/g, '\\\\\"') + '\"]') ",
            ByStrategy.TagName     => $"({contextExpr}).getElementsByTagName({jsValue})[0] || null",
            ByStrategy.ClassName   => $"({contextExpr}).getElementsByClassName({jsValue})[0] || null",
            ByStrategy.CssSelector => $"({contextExpr}).querySelector({jsValue})",
            ByStrategy.XPath       => XPathSingle(contextExpr),
            ByStrategy.LinkText    => LinkTextSingle(contextExpr, exact: true),
            ByStrategy.PartialLinkText => LinkTextSingle(contextExpr, exact: false),
            _ => throw new NotSupportedException(Strategy.ToString())
        };
    }

    /// <summary>
    /// Generates the JavaScript expression that resolves to an *Array* of
    /// matching elements.
    /// </summary>
    internal string ToJsArrayExpression(string contextExpr = "document")
    {
        string jv = JsStr(Value);
        bool isDoc = contextExpr == "document";

        return Strategy switch
        {
            ByStrategy.Id              => isDoc ? $"[document.getElementById({jv})].filter(e=>e)" : $"Array.from(({contextExpr}).querySelectorAll('#' + CSS.escape({jv}.slice(1, -1))))",
            ByStrategy.Name            => isDoc ? $"Array.from(document.getElementsByName({jv}))" : $"Array.from(({contextExpr}).querySelectorAll('[name=\"' + {jv}.slice(1,-1).replace(/\"/g, '\\\\\"') + '\"]'))",
            ByStrategy.TagName         => $"Array.from(({contextExpr}).getElementsByTagName({jv}))",
            ByStrategy.ClassName       => $"Array.from(({contextExpr}).getElementsByClassName({jv}))",
            ByStrategy.CssSelector     => $"Array.from(({contextExpr}).querySelectorAll({jv}))",
            ByStrategy.XPath           => XPathArray(contextExpr),
            ByStrategy.LinkText        => LinkTextArray(contextExpr, exact: true),
            ByStrategy.PartialLinkText => LinkTextArray(contextExpr, exact: false),
            _ => throw new NotSupportedException(Strategy.ToString())
        };
    }

    private string XPathSingle(string ctx) =>
        $"(function(){{var r=document.evaluate({JsStr(Value)},{ctx},null,XPathResult.FIRST_ORDERED_NODE_TYPE,null);return r.singleNodeValue;}})()";

    private string XPathArray(string ctx) =>
        $"(function(){{var snap=document.evaluate({JsStr(Value)},{ctx},null,XPathResult.ORDERED_NODE_SNAPSHOT_TYPE,null);var a=[];for(var i=0;i<snap.snapshotLength;i++)a.push(snap.snapshotItem(i));return a;}})()";

    private string LinkTextSingle(string ctx, bool exact)
    {
        string cmp = exact
            ? $"t==={JsStr(Value)}"
            : $"t.includes({JsStr(Value)})";
        return $"(function(){{var links=({ctx}).querySelectorAll('a');for(var i=0;i<links.length;i++){{var t=(links[i].textContent||'').trim();if({cmp})return links[i];}}return null;}})()";
    }

    private string LinkTextArray(string ctx, bool exact)
    {
        string cmp = exact
            ? $"t==={JsStr(Value)}"
            : $"t.includes({JsStr(Value)})";
        return $"(function(){{var links=({ctx}).querySelectorAll('a');var a=[];for(var i=0;i<links.length;i++){{var t=(links[i].textContent||'').trim();if({cmp})a.push(links[i]);}}return a;}})()";
    }

    private static string JsStr(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    public override string ToString() => $"By.{Strategy}(\"{Value}\")";
}
