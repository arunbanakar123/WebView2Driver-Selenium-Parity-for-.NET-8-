// ============================================================
//  JavaScriptExecutor.cs
//  Analogous to Selenium's IJavaScriptExecutor.
// ============================================================
using System.Text.Json;

namespace WebView2Driver.Browser;

/// <summary>
/// Executes arbitrary JavaScript within the context of the currently selected frame or window.
/// </summary>
public sealed class JavaScriptExecutor
{
    private readonly Core.WebView2Driver _driver;

    internal JavaScriptExecutor(Core.WebView2Driver driver)
    {
        _driver = driver;
    }

    /// <summary>
    /// Executes JavaScript synchronously.
    /// </summary>
    public object? ExecuteScript(string script, params object[] args)
    {
        return _driver.ExecuteScript<object?>(BuildScript(script, args));
    }

    /// <summary>
    /// Executes JavaScript synchronously, returning the strong type <typeparamref name="T"/>.
    /// </summary>
    public T? ExecuteScript<T>(string script, params object[] args)
    {
        return _driver.ExecuteScript<T>(BuildScript(script, args));
    }

    /// <summary>
    /// Executes JavaScript asynchronously. The script must call the provided callback
    /// (the last argument in <c>arguments</c>) to signal completion.
    /// </summary>
    public T? ExecuteAsyncScript<T>(string script, params object[] args)
    {
        // Selenium's mechanism injects a callback function as the last argument.
        // We will mock this via a Promise wrapper.
        string guid = Guid.NewGuid().ToString("N");
        string callbackName = "window.cb_" + guid;
        
        string wrapper = $@"
            return new Promise((resolve, reject) => {{
                {callbackName} = function(res) {{ resolve(res); delete {callbackName}; }};
                var args = {SerializeArgs(args)};
                args.push({callbackName});
                try {{
                    (function(){{ {script} }}).apply(null, args);
                }} catch (e) {{
                    reject(e.toString());
                }}
            }});
        ";

        return _driver.ExecuteScript<T>(wrapper);
    }

    private string BuildScript(string script, object[] args)
    {
        string argJson = SerializeArgs(args);
        return $"(function(){{ var arguments = {argJson}; {script} }})();";
    }

    private string SerializeArgs(object[] args)
    {
        if (args == null || args.Length == 0) return "[]";
        
        // Convert any WebView2Element to its JS ref string
        var mapped = args.Select(a =>
        {
            if (a is Elements.WebView2Element el)
                return $"__REF__{el.JsRef}__REF__";
            return a;
        }).ToArray();

        string json = JsonSerializer.Serialize(mapped);

        // Sub out the placeholder with the actual unquoted JS ref
        json = System.Text.RegularExpressions.Regex.Replace(json, "\"__REF__([\\s\\S]*?)__REF__\"", "$1");
        return json;
    }
}
