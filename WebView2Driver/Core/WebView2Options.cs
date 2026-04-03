// ============================================================
//  WebView2Options.cs
//  Configuration options for the WebView2Driver, analogous to
//  ChromeOptions / FirefoxOptions in Selenium.
// ============================================================
using Microsoft.Web.WebView2.Core;

namespace WebView2Driver.Core;

/// <summary>
/// Configures startup behaviour of the <see cref="WebView2Driver"/>.
/// </summary>
public sealed class WebView2Options
{
    // ── Browser / Environment ────────────────────────────────

    /// <summary>
    /// Folder where WebView2 stores its user-data profile.
    /// Defaults to a temporary directory so each instance is isolated.
    /// </summary>
    public string UserDataFolder { get; set; } =
        Path.Combine(Path.GetTempPath(), $"WebView2Driver_{Guid.NewGuid():N}");

    /// <summary>
    /// Additional Chromium command-line switches.
    /// Example: <c>"--disable-web-security"</c>.
    /// </summary>
    public List<string> AdditionalBrowserArguments { get; } = new();

    /// <summary>
    /// When <see langword="true"/> WebView2 starts in an InPrivate session.
    /// </summary>
    public bool InPrivate { get; set; } = false;

    /// <summary>
    /// Make the host form visible during automation.
    /// Set to <see langword="false"/> for fully headless-style operation.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>Initial width of the host window (pixels).</summary>
    public int WindowWidth { get; set; } = 1280;

    /// <summary>Initial height of the host window (pixels).</summary>
    public int WindowHeight { get; set; } = 900;

    // ── Downloads ────────────────────────────────────────────

    /// <summary>
    /// Folder where downloaded files are saved.
    /// Defaults to the system Downloads folder.
    /// </summary>
    public string DownloadFolder { get; set; } =
        KnownFolders.Downloads;

    // ── Proxy ────────────────────────────────────────────────

    /// <summary>
    /// Optional proxy server (e.g. <c>"http://127.0.0.1:8080"</c>).
    /// Adds <c>--proxy-server</c> to the browser arguments automatically.
    /// </summary>
    public string? Proxy
    {
        get => _proxy;
        set
        {
            _proxy = value;
            if (!string.IsNullOrWhiteSpace(value))
                AdditionalBrowserArguments.Add($"--proxy-server={value}");
        }
    }
    private string? _proxy;

    // ── Timeouts ─────────────────────────────────────────────

    /// <summary>Per-driver timeout settings.</summary>
    public WebView2TimeoutsConfig Timeouts { get; } = new();

    // ── Internal helper ──────────────────────────────────────

    internal CoreWebView2EnvironmentOptions BuildEnvironmentOptions()
    {
        var opts = new CoreWebView2EnvironmentOptions
        {
            AdditionalBrowserArguments = string.Join(" ", AdditionalBrowserArguments)
        };
        return opts;
    }
}

/// <summary>Known Windows shell paths without a hard P/Invoke dependency.</summary>
internal static class KnownFolders
{
    public static string Downloads =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
}
