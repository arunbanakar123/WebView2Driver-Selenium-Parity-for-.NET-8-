// ============================================================
//  CookieManager.cs
//  Browser cookies — analogous to Selenium's ICookieJar.
// ============================================================
using Microsoft.Web.WebView2.Core;

namespace WebView2Driver.Browser;

/// <summary>
/// Mirrors Selenium's <c>Cookie</c> class.
/// </summary>
public sealed class Cookie
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Path { get; set; } = "/";
    public DateTime? Expiry { get; set; }
    public bool Secure { get; set; }
    public bool HttpOnly { get; set; }
    
    public override string ToString() => $"{Name}={Value}; Domain={Domain}; Path={Path}";
}

/// <summary>
/// Manages browser cookies for the current domain.
/// </summary>
public sealed class CookieManager
{
    private readonly Core.WebView2Driver _driver;

    internal CookieManager(Core.WebView2Driver driver)
    {
        _driver = driver;
    }

    /// <summary>Adds a specific cookie to the browser.</summary>
    public void AddCookie(Cookie cookie)
    {
        _driver.WebView.Invoke((Action)(() => 
        {
            var Mgr = _driver.WebView.CoreWebView2.CookieManager;
            var cw2Cookie = Mgr.CreateCookie(cookie.Name, cookie.Value, cookie.Domain, cookie.Path);
            cw2Cookie.IsSecure = cookie.Secure;
            cw2Cookie.IsHttpOnly = cookie.HttpOnly;
            if (cookie.Expiry.HasValue)
                cw2Cookie.Expires = cookie.Expiry.Value;
            
            Mgr.AddOrUpdateCookie(cw2Cookie);
        }));
    }

    /// <summary>Gets all cookies for the current domain.</summary>
    public IReadOnlyList<Cookie> AllCookies
    {
        get
        {
            var uri = _driver.Url;
            if (string.IsNullOrEmpty(uri)) return Array.Empty<Cookie>();

            IReadOnlyList<Cookie> result = null!;
            var mre = new System.Threading.ManualResetEventSlim(false);
            
            _driver.WebView.Invoke((Action)(async () => 
            {
                var Mgr = _driver.WebView.CoreWebView2.CookieManager;
                var list = await Mgr.GetCookiesAsync(uri);
                result = list.Select(c => new Cookie
                {
                    Name = c.Name,
                    Value = c.Value,
                    Domain = c.Domain,
                    Path = c.Path,
                    Secure = c.IsSecure,
                    HttpOnly = c.IsHttpOnly,
                    Expiry = c.Expires != DateTime.MinValue ? c.Expires : null
                }).ToList();
                mre.Set();
            }));
            
            mre.Wait();
            return result ?? Array.Empty<Cookie>();
        }
    }

    /// <summary>Gets a cookie with the specified name.</summary>
    public Cookie? GetCookieNamed(string name)
    {
        return AllCookies.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Deletes the specified cookie.</summary>
    public void DeleteCookie(Cookie cookie)
    {
        DeleteCookieNamed(cookie.Name);
    }

    /// <summary>Deletes a cookie with the specified name.</summary>
    public void DeleteCookieNamed(string name)
    {
        var uri = _driver.Url;
        if (string.IsNullOrEmpty(uri)) return;

        var mre = new System.Threading.ManualResetEventSlim(false);
        _driver.WebView.Invoke((Action)(async () => 
        {
            var Mgr = _driver.WebView.CoreWebView2.CookieManager;
            var cookies = await Mgr.GetCookiesAsync(uri);
            foreach (var c in cookies)
            {
                if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                    Mgr.DeleteCookie(c);
            }
            mre.Set();
        }));
        mre.Wait();
    }

    /// <summary>Deletes all cookies for the current domain.</summary>
    public void DeleteAllCookies()
    {
        var uri = _driver.Url;
        if (string.IsNullOrEmpty(uri)) return;

        var mre = new System.Threading.ManualResetEventSlim(false);
        _driver.WebView.Invoke((Action)(async () => 
        {
            var Mgr = _driver.WebView.CoreWebView2.CookieManager;
            var cookies = await Mgr.GetCookiesAsync(uri);
            foreach (var c in cookies)
                Mgr.DeleteCookie(c);
            mre.Set();
        }));
        mre.Wait();
    }
}
