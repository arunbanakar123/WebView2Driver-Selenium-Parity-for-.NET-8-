// ============================================================
//  WebDriverWait.cs
//  Explicit wait — analogous to Selenium's WebDriverWait.
// ============================================================
namespace WebView2Driver.Waits;

/// <summary>
/// Waits for a condition to become true within a specified timeout.
/// Mirrors Selenium's <c>WebDriverWait</c>.
/// </summary>
public sealed class WebDriverWait
{
    private readonly Core.WebView2Driver _driver;
    private readonly TimeSpan _timeout;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(500);
    private readonly List<Type> _ignoredExceptions = new();

    /// <param name="driver">The driver to pass to the condition function.</param>
    /// <param name="timeoutSeconds">Max seconds to wait.</param>
    /// <param name="pollingIntervalMs">How often to re-evaluate (ms). Default 500.</param>
    public WebDriverWait(Core.WebView2Driver driver, double timeoutSeconds = 10, int pollingIntervalMs = 500)
    {
        _driver         = driver ?? throw new ArgumentNullException(nameof(driver));
        _timeout        = TimeSpan.FromSeconds(timeoutSeconds);
        PollingInterval = TimeSpan.FromMilliseconds(pollingIntervalMs);
    }

    public WebDriverWait(Core.WebView2Driver driver, TimeSpan timeout)
    {
        _driver         = driver ?? throw new ArgumentNullException(nameof(driver));
        _timeout        = timeout;
        PollingInterval = TimeSpan.FromMilliseconds(500);
    }

    /// <summary>Optional message appended to <see cref="WaitTimeoutException"/> on failure.</summary>
    public string? Message { get; set; }

    /// <summary>
    /// Ignore exceptions of type <typeparamref name="TEx"/> while polling.
    /// </summary>
    public WebDriverWait IgnoreExceptionTypes<TEx>() where TEx : Exception
    {
        _ignoredExceptions.Add(typeof(TEx));
        return this;
    }

    // ── Until ────────────────────────────────────────────────

    /// <summary>
    /// Polls <paramref name="condition"/> until it returns a non-null,
    /// non-false result or the timeout expires.
    /// </summary>
    public T Until<T>(Func<Core.WebView2Driver, T> condition)
    {
        var deadline = DateTime.UtcNow + _timeout;
        Exception? lastEx = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var result = condition(_driver);

                if (result is bool b)
                {
                    if (b) return result;
                }
                else if (result is not null)
                {
                    return result;
                }
            }
            catch (Exception ex) when (_ignoredExceptions.Exists(t => t.IsInstanceOfType(ex)))
            {
                lastEx = ex;
            }

            Thread.Sleep(PollingInterval);
        }

        var msg = Message ?? $"Timed out after {_timeout.TotalSeconds:F1}s waiting for condition.";
        throw new WaitTimeoutException(msg, lastEx);
    }
}

/// <summary>Thrown when an explicit wait condition is not met within the timeout.</summary>
public class WaitTimeoutException : TimeoutException
{
    public WaitTimeoutException(string message, Exception? inner = null)
        : base(message, inner) { }
}
