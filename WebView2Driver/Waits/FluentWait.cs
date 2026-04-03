// ============================================================
//  FluentWait.cs
//  Builder-style wait — analogous to Selenium's FluentWait<T>.
// ============================================================
namespace WebView2Driver.Waits;

/// <summary>
/// Fluent wait builder — chain <c>.WithTimeout()</c>, <c>.Polling()</c>,
/// <c>.IgnoreExceptionTypes()</c> then call <c>.Until()</c>.
/// </summary>
public sealed class FluentWait
{
    private readonly Core.WebView2Driver _driver;
    private TimeSpan _timeout         = TimeSpan.FromSeconds(10);
    private TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(500);
    private readonly List<Type> _ignored = new();
    private string? _message;

    public FluentWait(Core.WebView2Driver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    /// <summary>Set the maximum wait duration.</summary>
    public FluentWait WithTimeout(TimeSpan timeout)      { _timeout         = timeout;  return this; }
    public FluentWait WithTimeout(double seconds)        { _timeout         = TimeSpan.FromSeconds(seconds);      return this; }

    /// <summary>Set the polling interval.</summary>
    public FluentWait Polling(TimeSpan interval)         { _pollingInterval = interval; return this; }
    public FluentWait Polling(int milliseconds)          { _pollingInterval = TimeSpan.FromMilliseconds(milliseconds); return this; }

    /// <summary>Ignore exceptions of the given type while polling.</summary>
    public FluentWait IgnoreExceptionTypes<TEx>() where TEx : Exception { _ignored.Add(typeof(TEx)); return this; }
    public FluentWait IgnoreExceptionTypes(params Type[] types)         { _ignored.AddRange(types);  return this; }

    /// <summary>Message included in <see cref="WaitTimeoutException"/> on failure.</summary>
    public FluentWait WithMessage(string message) { _message = message; return this; }

    /// <summary>Wait until <paramref name="condition"/> is satisfied.</summary>
    public T Until<T>(Func<Core.WebView2Driver, T> condition)
    {
        var wait = new WebDriverWait(_driver, _timeout.TotalSeconds, (int)_pollingInterval.TotalMilliseconds)
        {
            Message = _message
        };
        foreach (var t in _ignored)
        {
            // Register via reflection since WebDriverWait uses generics
            typeof(WebDriverWait)
                .GetMethod(nameof(WebDriverWait.IgnoreExceptionTypes))!
                .MakeGenericMethod(t)
                .Invoke(wait, null);
        }
        return wait.Until(condition);
    }
}
