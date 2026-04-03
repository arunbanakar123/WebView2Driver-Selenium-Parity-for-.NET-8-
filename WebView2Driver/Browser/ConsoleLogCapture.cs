// ============================================================
//  ConsoleLogCapture.cs
//  Captures browser console output.
// ============================================================
using Microsoft.Web.WebView2.Core;

namespace WebView2Driver.Browser;

public enum LogLevel { Debug, Info, Warn, Error }

public sealed class ConsoleEntry
{
    public LogLevel Level { get; set; }
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }

    public override string ToString() => $"[{Timestamp:HH:mm:ss.fff}] [{Level}] {Message}";
}

/// <summary>
/// Intercepts <c>console.log</c>, <c>console.warn</c>, <c>console.error</c> 
/// via script injection and queues them for retrieval.
/// Mirrors Selenium's <c>ILogs</c> / LogPrefs mechanism.
/// </summary>
public sealed class ConsoleLogCapture
{
    private readonly Core.WebView2Driver _driver;
    private readonly List<ConsoleEntry> _logs = new();
    private bool _injected;

    internal ConsoleLogCapture(Core.WebView2Driver driver)
    {
        _driver = driver;
    }

    /// <summary>Enables overriding the console methods in the current page.</summary>
    public void Enable()
    {
        if (_injected) return;
        _injected = true;

        var mre = new System.Threading.ManualResetEventSlim(false);
        _driver.WebView.Invoke((Action)(async () =>
        {
            // Add init script so it applies to every navigation
            await _driver.WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                (function() {
                    var oldLog = console.log, oldWarn = console.warn, oldError = console.error, oldDebug = console.debug;
                    
                    function send(level, args) {
                        var msg = Array.from(args).map(a => {
                            if (typeof a === 'object') {
                                try { return JSON.stringify(a); } catch(e) { return String(a); }
                            }
                            return String(a);
                        }).join(' ');
                        
                        window.chrome.webview.postMessage({ type: 'ConsoleLogCapture', level: level, message: msg });
                    }

                    console.log = function() { send('Info', arguments); oldLog.apply(console, arguments); };
                    console.warn = function() { send('Warn', arguments); oldWarn.apply(console, arguments); };
                    console.error = function() { send('Error', arguments); oldError.apply(console, arguments); };
                    console.debug = function() { send('Debug', arguments); oldDebug.apply(console, arguments); };
                })();
            ");

            _driver.WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            mre.Set();
        }));
        mre.Wait();
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(json)) return;

            // Very basic parse to avoid full json parser overhead for every log
            if (json.Contains("\"type\": \"ConsoleLogCapture\"") || json.Contains("\"type\":\"ConsoleLogCapture\""))
            {
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict == null) return;

                if (dict.TryGetValue("level", out string? lvlStr) && dict.TryGetValue("message", out string? msg))
                {
                    LogLevel lvl = LogLevel.Info;
                    if (lvlStr == "Warn") lvl = LogLevel.Warn;
                    else if (lvlStr == "Error") lvl = LogLevel.Error;
                    else if (lvlStr == "Debug") lvl = LogLevel.Debug;

                    lock (_logs)
                    {
                        _logs.Add(new ConsoleEntry
                        {
                            Level = lvl,
                            Message = msg,
                            Timestamp = DateTime.Now
                        });
                    }
                }
            }
        }
        catch { /* ignore parsing errors */ }
    }

    /// <summary>Returns all captured logs since the last call to <c>GetAvailableLogs</c>.</summary>
    public IReadOnlyList<ConsoleEntry> GetAvailableLogs()
    {
        lock (_logs)
        {
            var copy = _logs.ToList();
            _logs.Clear();
            return copy;
        }
    }
}
