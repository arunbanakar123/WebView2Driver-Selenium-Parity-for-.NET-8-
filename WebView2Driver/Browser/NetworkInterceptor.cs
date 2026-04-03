// ============================================================
//  NetworkInterceptor.cs
//  Intercepts and modifies network requests.
// ============================================================
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;

namespace WebView2Driver.Browser;

/// <summary>
/// Allows inspecting and altering HTTP requests before they are sent,
/// or blocking them entirely.
/// </summary>
public sealed class NetworkInterceptor
{
    private readonly Core.WebView2Driver _driver;

    internal NetworkInterceptor(Core.WebView2Driver driver)
    {
        _driver = driver;
        _driver.WebView.Invoke((Action)(() => 
        {
            var core = _driver.WebView.CoreWebView2;
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += Core_WebResourceRequested;
        }));
    }

    private readonly List<Action<CoreWebView2WebResourceRequestedEventArgs>> _handlers = new();
    private readonly List<Regex> _blockPatterns = new();

    /// <summary>Add a handler that observes or modifies every outgoing request.</summary>
    public void AddRequestHandler(Action<CoreWebView2WebResourceRequestedEventArgs> handler)
    {
        _handlers.Add(handler);
    }

    /// <summary>Block all requests where the URL matches the given regex pattern.</summary>
    public void BlockUrlPattern(string regexPattern)
    {
        _blockPatterns.Add(new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled));
    }

    private void Core_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        string url = e.Request.Uri;

        // Check blocklist
        foreach (var pattern in _blockPatterns)
        {
            if (pattern.IsMatch(url))
            {
                // Return a 403 Forbidden response to effectively block it
                var env = _driver.WebView.CoreWebView2.Environment;
                var response = env.CreateWebResourceResponse(null, 403, "Blocked", "Reason: Blocked by NetworkInterceptor");
                e.Response = response;
                return;
            }
        }

        // Run user handlers (they can modify e.Request headers, or set e.Response)
        foreach (var h in _handlers)
        {
            h(e);
        }
    }
}
