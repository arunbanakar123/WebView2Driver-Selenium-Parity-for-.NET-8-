// ============================================================
//  WebView2TimeoutsConfig.cs
//  Timeout settings — analogous to ITimeouts in Selenium.
// ============================================================
namespace WebView2Driver.Core;

/// <summary>
/// Holds timeout durations used across the driver.
/// Mirrors Selenium's <c>ITimeouts</c> interface.
/// </summary>
public sealed class WebView2TimeoutsConfig
{
    /// <summary>
    /// Maximum time to wait for a page-load to complete.
    /// Default: 30 s.
    /// </summary>
    public TimeSpan PageLoad { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum time to wait for an <c>ExecuteScript</c> call to resolve.
    /// Default: 30 s.
    /// </summary>
    public TimeSpan Script { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Implicit-wait applied by <c>FindElement</c> when an element is
    /// not immediately available. Default: 0 (no implicit wait).
    /// </summary>
    public TimeSpan ImplicitWait { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// How often implicit-wait polls for the element. Default: 200 ms.
    /// </summary>
    public TimeSpan ImplicitPollInterval { get; set; } = TimeSpan.FromMilliseconds(200);
}
