# WebView2Driver (Selenium-Parity for .NET 8)

A high-performance, Selenium-like WebDriver wrapper for the `Microsoft.Web.WebView2` WinForms control. This library provides a familiar API for automating WebView2 instances, making it ideal for scrapers, testing, and automation tasks in .NET 8 Windows applications.

## Key Features

- **Selenium-Parity API**: Familiar `FindElement`, `FindElements`, `Navigate`, `SwitchTo`, and `Manage` interfaces.
- **Robust Multi-threading**: Handles the underlying WinForms/STA thread model automatically, preventing COM deadlocks and thread-access errors.
- **JS Execution Bridge**: High-performance script execution with automatic IIFE wrapping for multi-statement scripts.
- **Wait Mechanisms**: Built-in support for `ImplicitWait`, `ExplicitWait` (via `WebDriverWait`), and `FluentWait`.
- **Selector Strategies**: Supports `By.Id`, `By.Name`, `By.ClassName`, `By.CssSelector`, `By.XPath`, `By.LinkText`, and `By.TagName`.
- **Advanced Interactions**: Full `IAlert` support (alerts, confirms, prompts), standard `IOptions` (Cookies), and `IScreenshot` capabilities.
- **Reliable Typing**: `SendKeys` with JS event fallback and correct PUA keyboard mapping for special characters.

## Getting Started

### 1. Installation
The project is a .NET 8 WinForms library. Ensure you have the `Microsoft.Web.WebView2` NuGet package installed.

### 2. Initialization
The driver starts its own isolated WinForms message loop in a background STA thread, allowing you to use it from any calling thread.

```csharp
using WebView2Driver.Core;
using WebView2Driver.Waits;

var options = new WebView2Options 
{ 
    Visible = true, 
    WindowWidth = 1000, 
    WindowHeight = 800 
};

using var driver = new WebView2Driver(options);
driver.Navigate("https://www.google.com");

// Wait for page load
var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
wait.Until(d => d.Title.Contains("Google"));
```

### 3. Usage Examples

#### Finding Elements and Typing
```csharp
var searchBox = driver.FindElement(By.Name("q"));
searchBox.SendKeys("WebView2Driver rocks!" + Keys.Enter);
```

#### Handling Transitions (Visiblity wait)
```csharp
// Wait for an element to become visible (e.g. after a JS transition)
var hiddenDiv = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("hidden-div")));
Console.WriteLine(hiddenDiv.Text);
```

#### Alert Handling
```csharp
driver.FindElement(By.Id("trigger-alert")).Click();
var alert = driver.SwitchTo().Alert();
Console.WriteLine(alert.Text);
alert.Accept();
```

#### Executing JavaScript
```csharp
string result = driver.JavaScript.ExecuteScript<string>("return document.title;");
```

## Architecture

The `WebView2Driver` is designed to be **thread-safe** and **deadlock-free**:
1. It bootstraps an internal WinForms `Form` and `CoreWebView2` on a dedicated **STA Background Thread**.
2. All synchronous driver API calls (like `FindElement` or `Click`) automatically bridge to the UI thread using a non-blocking `TaskCompletionSource` pattern.
3. This prevents the common "Sync-over-Async" deadlocks typically encountered when using WebView2 in console or background services.

## Full Feature Matrix

| Feature | Supported | Description |
| :--- | :---: | :--- |
| **IWebDriver** | ✅ | Navigate, Title, Url, PageSource, Quit, Dispose |
| **IWebElement**| ✅ | Click, SendKeys, Clear, GetAttribute, GetProperty, Displayed, Location |
| **IAlert**     | ✅ | Accept, Dismiss, Text, SendKeys (for Prompts) |
| **ICookies**   | ✅ | AddCookie, GetCookieNamed, AllCookies, DeleteCookie, DeleteAll |
| **IWindow**    | ✅ | SetSize, SetPosition, Maximize |
| **IJavaScript**| ✅ | ExecuteScript, ExecuteScript<T> |
| **IWait**      | ✅ | Implicit, Explicit, Fluent |

## Demo Suite

The `WebView2Driver.Demo` project contains a comprehensive verification suite targeting a local `testbed.html`. It serves as the primary integration test for the entire framework.

## Suggestions are welcome

## License
MIT License.
