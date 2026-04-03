// ============================================================
//  WebStorage.cs
//  LocalStorage and SessionStorage access.
// ============================================================
using System.Text.Json;

namespace WebView2Driver.Browser;

/// <summary>
/// Provides access to HTML5 localStorage and sessionStorage.
/// Mirrors Selenium's <c>IWebStorage</c>, <c>ILocalStorage</c>, <c>ISessionStorage</c>.
/// </summary>
public sealed class WebStorage
{
    private readonly Core.WebView2Driver _driver;

    internal WebStorage(Core.WebView2Driver driver)
    {
        _driver = driver;
        LocalStorage = new StorageProxy(_driver, "localStorage");
        SessionStorage = new StorageProxy(_driver, "sessionStorage");
    }

    /// <summary>Access to the window.localStorage map.</summary>
    public StorageProxy LocalStorage { get; }

    /// <summary>Access to the window.sessionStorage map.</summary>
    public StorageProxy SessionStorage { get; }

    /// <summary>Represents a single WebStorage map.</summary>
    public sealed class StorageProxy
    {
        private readonly Core.WebView2Driver _driver;
        private readonly string _storageType;

        internal StorageProxy(Core.WebView2Driver driver, string storageType)
        {
            _driver = driver;
            _storageType = storageType;
        }

        public int Count => _driver.ExecuteScript<int>($"return window.{_storageType}.length;");

        public void Clear() => _driver.ExecuteScript($"window.{_storageType}.clear();");

        public string? GetItem(string key) => 
            _driver.ExecuteScript<string?>($"return window.{_storageType}.getItem({JsStr(key)});");

        public void SetItem(string key, string value) => 
            _driver.ExecuteScript($"window.{_storageType}.setItem({JsStr(key)}, {JsStr(value)});");

        public string? RemoveItem(string key)
        {
            string? val = GetItem(key);
            _driver.ExecuteScript($"window.{_storageType}.removeItem({JsStr(key)});");
            return val;
        }

        public IReadOnlyList<string> KeySet()
        {
            return _driver.ExecuteScript<string[]>($"return Object.keys(window.{_storageType});") 
                   ?? Array.Empty<string>();
        }

        private static string JsStr(string s) =>
            "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
