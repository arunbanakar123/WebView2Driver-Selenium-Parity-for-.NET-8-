// ============================================================
//  DevToolsProtocol.cs
//  Raw CDP command execution.
// ============================================================
using System.Text.Json;
using System.Threading.Tasks;

namespace WebView2Driver.Browser;

/// <summary>
/// Manages raw Chromium DevTools Protocol communication.
/// </summary>
public sealed class DevToolsProtocol
{
    private readonly Core.WebView2Driver _driver;

    internal DevToolsProtocol(Core.WebView2Driver driver)
    {
        _driver = driver;
    }

    /// <summary>
    /// Sends a raw CDP method call.
    /// </summary>
    /// <param name="methodName">The CDP method, e.g. <c>"Network.emulateNetworkConditions"</c>.</param>
    /// <param name="parameters">An anonymous object or dictionary of parameters.</param>
    public async Task<JsonDocument> CallMethodAsync(string methodName, object? parameters = null)
    {
        string paramJson = "{}";
        if (parameters != null) paramJson = JsonSerializer.Serialize(parameters);

        var tcs = new TaskCompletionSource<string>();
        _driver.WebView.BeginInvoke((Action)(async () => 
        {
            try
            {
                if (_driver.WebView.IsDisposed || _driver.WebView.CoreWebView2 == null)
                {
                    tcs.SetException(new InvalidOperationException("WebView2 not ready for CDP."));
                    return;
                }
                var result = await _driver.WebView.CoreWebView2.CallDevToolsProtocolMethodAsync(methodName, paramJson);
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }));

        var resultJson = await tcs.Task;
        return JsonDocument.Parse(resultJson ?? "{}");
    }
}
