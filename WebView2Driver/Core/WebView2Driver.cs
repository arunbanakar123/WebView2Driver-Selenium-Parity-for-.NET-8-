// ============================================================
//  WebView2Driver.cs
//  Main driver class — analogous to IWebDriver.
// ============================================================
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Drawing;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebView2Driver.Browser;
using WebView2Driver.Elements;
using WebView2Driver.Interactions;

namespace WebView2Driver.Core;

/// <summary>
/// A Selenium-like WebDriver wrapper around the Microsoft.Web.WebView2 control.
/// </summary>
public sealed class WebView2Driver : IDisposable
{
    private Form? _hostForm;
    private Microsoft.Web.WebView2.WinForms.WebView2 _wv2 = null!;
    private readonly ManualResetEventSlim _wv2Ready = new();

    // Features
    public WebView2Options Options { get; }
    public IOptions Manage() => new OptionsManager(this);
    public FrameHandler FrameHandler { get; } = null!;
    public WindowManager Window { get; } = null!;
    public ScreenshotHelper Screenshot { get; } = null!;
    public JavaScriptExecutor JavaScript { get; } = null!;
    public NetworkInterceptor Network { get; } = null!;
    public WebStorage WebStorage { get; } = null!;
    public ConsoleLogCapture ConsoleLogs { get; } = null!;
    public DownloadManager DownloadManager { get; } = null!;
    public DevToolsProtocol DevTools { get; } = null!;
    public Keyboard Keyboard { get; } = null!;
    public Mouse Mouse { get; } = null!;
    private AlertHandler _alertHandler = null!;

    /// <summary>Access to the raw underlying WinForms control.</summary>
    public Microsoft.Web.WebView2.WinForms.WebView2 WebView => _wv2;

    /// <summary>The internal host form (if created by the driver).</summary>
    internal Form? HostForm => _hostForm;

    // ── Initialization ───────────────────────────────────────

    /// <summary>
    /// Starts a new isolated WebView2 instance in its own TopLevel form.
    /// </summary>
    public WebView2Driver(WebView2Options? options = null)
    {
        Options = options ?? new WebView2Options();

        // Must run UI initialization on an STA thread
        Exception? initEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                _hostForm = new Form
                {
                    Text = "WebView2Driver",
                    Width = Options.WindowWidth,
                    Height = Options.WindowHeight,
                    ShowInTaskbar = Options.Visible,
                    WindowState = Options.Visible ? FormWindowState.Normal : FormWindowState.Minimized
                };

                _wv2 = new Microsoft.Web.WebView2.WinForms.WebView2
                {
                    Dock = DockStyle.Fill
                };
                _hostForm.Controls.Add(_wv2);

                if (!Options.Visible)
                {
                    // Stealth mode technique: Move offscreen if not visible
                    _hostForm.StartPosition = FormStartPosition.Manual;
                    _hostForm.Location = new Point(-20000, -20000);
                }

                _hostForm.Shown += (s, e) => InitializeCoreAsync();
                Application.Run(_hostForm);
            }
            catch (Exception ex)
            {
                initEx = ex;
                _wv2Ready.Set();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        // Wait for browser core to init
        _wv2Ready.Wait();
        if (initEx != null) throw initEx;

        void cLog(string s) { Console.WriteLine(s); }

        try 
        {
            cLog("Creating CookieManager");
            ManageCookies   = new CookieManager(this); // Keep it internal or private if we use Manage().Cookies
            cLog("Creating FrameHandler");
            FrameHandler    = new FrameHandler(this);
            cLog("Creating WindowManager");
            Window          = new WindowManager(this);
            cLog("Creating ScreenshotHelper");
            Screenshot      = new ScreenshotHelper(this);
            cLog("Creating JavaScriptExecutor");
            JavaScript      = new JavaScriptExecutor(this);
            cLog("Creating NetworkInterceptor");
            Network         = new NetworkInterceptor(this);
            cLog("Creating WebStorage");
            WebStorage      = new WebStorage(this);
            cLog("Creating ConsoleLogs");
            ConsoleLogs     = new ConsoleLogCapture(this);
            cLog("Creating DownloadManager");
            DownloadManager = new DownloadManager(this, Options.DownloadFolder);
            cLog("Creating DevToolsProtocol");
            DevTools        = new DevToolsProtocol(this);
            cLog("Creating Keyboard");
            Keyboard        = new Keyboard(this);
            cLog("Creating Mouse");
            Mouse           = new Mouse(this);
            cLog("Creating AlertHandler");
            _alertHandler   = new AlertHandler(this);
            cLog("All Managers Created");
        }
        catch (Exception ex)
        {
            cLog($"EXCEPTION IN CONSTRUCTOR: {ex}");
            throw;
        }
    }

    private async void InitializeCoreAsync()
    {
        try
        {
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            Console.WriteLine("[WebView2] Runtime Found: " + version);

            _wv2.CoreWebView2InitializationCompleted += (sender, e) =>
            {
                if (!e.IsSuccess)
                {
                    Console.WriteLine($"[WebView2 Init Error] {e.InitializationException?.Message}");
                }
                else
                {
                    Console.WriteLine("[WebView2] Initialization Completed Event Fired Successfully");
                    // Apply common settings here if needed
                    _wv2.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                    _wv2.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                }
                _wv2Ready.Set();
            };

            await _wv2.EnsureCoreWebView2Async();
            Console.WriteLine("[WebView2] EnsureCore Task Completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[WebView2 Critical Error] " + ex.Message);
            _wv2Ready.Set();
        }
    }

    // ── Navigation ───────────────────────────────────────────

    /// <summary>Navigates to the specified URL and waits for load completion.</summary>
    public void Navigate(string url)
    {
        var mre = new ManualResetEventSlim(false);
        Exception? navEx = null;

        EventHandler<CoreWebView2NavigationCompletedEventArgs> handler = (s, e) =>
        {
            if (!e.IsSuccess && e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled)
            {
                navEx = new Exception($"Navigation failed: {e.WebErrorStatus}");
            }
            mre.Set();
        };

        _wv2.Invoke((Action)(() =>
        {
            _wv2.CoreWebView2.NavigationCompleted += handler;
            _wv2.CoreWebView2.Navigate(url);
        }));

        if (!mre.Wait(Options.Timeouts.PageLoad))
        {
            _wv2.Invoke((Action)(() => _wv2.CoreWebView2.NavigationCompleted -= handler));
            throw new TimeoutException($"Page load timed out after {Options.Timeouts.PageLoad.TotalSeconds}s.");
        }
        _wv2.Invoke((Action)(() => _wv2.CoreWebView2.NavigationCompleted -= handler));

        if (navEx != null) throw navEx;
        FrameHandler.DefaultContent(); // Reset frame context on new load
    }

    /// <summary>Current Document URL.</summary>
    public string Url => _wv2.Invoke((Func<string>)(() => _wv2.Source.ToString()));

    /// <summary>Current Document Title.</summary>
    public string Title => ExecuteScript<string>("return document.title;") ?? "";

    // ── Search Context ───────────────────────────────────────

    /// <summary>
    /// Finds the first element in the current DOM (or active Frame) matching the By strategy.
    /// Applies the implicit wait if configured.
    /// </summary>
    public WebView2Element FindElement(By by)
    {
        var deadline = DateTime.UtcNow + Options.Timeouts.ImplicitWait;
        while (true)
        {
            string js = by.ToJsSingleExpression(FrameHandler.GetContextExpression());
            var result = ExecuteScript<JsonElement?>($"return {js};");
            
            if (result != null && result.Value.ValueKind != JsonValueKind.Null)
            {
                return new WebView2Element(this, js);
            }

            if (DateTime.UtcNow >= deadline)
                throw new NoSuchElementException($"No element found matching: {by}");
            
            Thread.Sleep(Options.Timeouts.ImplicitPollInterval);
        }
    }

    /// <summary>
    /// Finds all elements matching the By strategy.
    /// Does not throw if empty.
    /// </summary>
    public IReadOnlyList<WebView2Element> FindElements(By by)
    {
        string js = by.ToJsArrayExpression(FrameHandler.GetContextExpression());
        var count = ExecuteScript<int?>($"return ({js}).length;") ?? 0;
        
        var list = new List<WebView2Element>(count);
        for (int i = 0; i < count; i++)
            list.Add(new WebView2Element(this, $"({js})[{i}]"));
        
        return list;
    }

    // ── Scripting ────────────────────────────────────────────

    /// <summary>
    /// Executes a script synchronously in the current frame context.
    /// </summary>
    public T? ExecuteScript<T>(string script)
    {
        string? resultJson = null;

        // Auto-wrap return statements if they are at the top level
        var finalScript = script.Trim();
        if (finalScript.StartsWith("return "))
        {
            finalScript = $"(function(){{ {finalScript} }})()";
        }

        try
        {
            var tcs = new TaskCompletionSource<string?>();
            _wv2.BeginInvoke((Action)(async () => 
            {
                try
                {
                    if (_wv2.IsDisposed || _wv2.CoreWebView2 == null)
                    {
                        tcs.SetException(new InvalidOperationException("WebView2 not ready."));
                        return;
                    }
                    var res = await _wv2.CoreWebView2.ExecuteScriptAsync(finalScript);
                    tcs.SetResult(res);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));

            // Wait for the result on the background thread
            if (!tcs.Task.Wait(Options.Timeouts.Script))
            {
                throw new TimeoutException($"Script timed out after {Options.Timeouts.Script.TotalSeconds}s: {script}");
            }
            resultJson = tcs.Task.Result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Script Error] {ex.Message} (Script: {script})");
            throw;
        }

        // Diagnostic logging (disabled for final clean run)
        // if (script.Length < 100) 
        //    Console.WriteLine($"[Script] Result for '{script}': {(resultJson ?? "<null>")}");

        if (string.IsNullOrEmpty(resultJson) || resultJson == "null") return default;
        return JsonSerializer.Deserialize<T>(resultJson);
    }

    public void ExecuteScript(string script) => ExecuteScript<object?>(script);

    // ── Switching ────────────────────────────────────────────

    /// <summary>Access to frame and alert switching (TargetLocator concept).</summary>
    public Switcher SwitchTo() => new Switcher(this);

    public readonly struct Switcher
    {
        private readonly WebView2Driver _d;
        internal Switcher(WebView2Driver d) { _d = d; }

        public void Frame(string nameOrId) => _d.FrameHandler.Frame(nameOrId);
        public void Frame(int index) => _d.FrameHandler.Frame(index);
        public void Frame(WebView2Element frameElement) => _d.FrameHandler.Frame(frameElement);
        public void DefaultContent() => _d.FrameHandler.DefaultContent();
        public void ParentFrame() => _d.FrameHandler.ParentFrame();

        public AlertHandler Alert() => _d._alertHandler;
        public bool AlertIsPresent() => _d._alertHandler.IsPresent;
    }

    // ── Management (Selenium Parity) ───────────────────────

    public interface IOptions
    {
        CookieManager Cookies { get; }
        // We can add Timeouts, Window, etc. here later
    }

    private class OptionsManager : IOptions
    {
        private readonly WebView2Driver _d;
        public OptionsManager(WebView2Driver d) { _d = d; }
        public CookieManager Cookies => _d.ManageCookies;
    }

    private CookieManager ManageCookies { get; set; }

    // ── Cleanup ──────────────────────────────────────────────

    public void Quit() => Dispose();

    public void Dispose()
    {
        if (_hostForm != null && !_hostForm.IsDisposed)
        {
            _hostForm.Invoke((Action)(() =>
            {
                _wv2.Dispose();
                _hostForm.Close();
                _hostForm.Dispose();
            }));
        }
        else if (_wv2 != null && !_wv2.IsDisposed && _wv2.InvokeRequired)
        {
             _wv2.Invoke((Action)(() => _wv2.Dispose()));
        }
    }
}
