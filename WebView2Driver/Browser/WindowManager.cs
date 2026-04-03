// ============================================================
//  WindowManager.cs
//  Window and Tab management.
// ============================================================
namespace WebView2Driver.Browser;

/// <summary>
/// Mirrors Selenium's <c>ITargetLocator</c> window-switching API.
/// In WebView2, "windows" are handled via NewWindowRequested.
/// For simplicity in a WinForms host, we will track the main window
/// and allow opening DevTools, but full multi-tab support requires 
/// host app cooperation (which Selenium has native control over in Chrome).
/// </summary>
public sealed class WindowManager
{
    private readonly Core.WebView2Driver _driver;

    internal WindowManager(Core.WebView2Driver driver)
    {
        _driver = driver;
    }

    /// <summary>Maximizes the host window.</summary>
    public void Maximize()
    {
        if (_driver.HostForm != null)
        {
            _driver.HostForm.Invoke((Action)(() =>
            {
                _driver.HostForm.WindowState = FormWindowState.Maximized;
            }));
        }
    }

    /// <summary>Minimizes the host window.</summary>
    public void Minimize()
    {
        if (_driver.HostForm != null)
        {
            _driver.HostForm.Invoke((Action)(() =>
            {
                _driver.HostForm.WindowState = FormWindowState.Minimized;
            }));
        }
    }

    /// <summary>Restores (un-maximizes) the host window.</summary>
    public void Restore()
    {
        if (_driver.HostForm != null)
        {
            _driver.HostForm.Invoke((Action)(() =>
            {
                _driver.HostForm.WindowState = FormWindowState.Normal;
            }));
        }
    }

    /// <summary>Sets the host window size.</summary>
    public void SetSize(int width, int height)
    {
        if (_driver.HostForm != null)
        {
            _driver.HostForm.Invoke((Action)(() =>
            {
                _driver.HostForm.Size = new Size(width, height);
            }));
        }
    }

    /// <summary>Sets the host window position.</summary>
    public void SetPosition(int x, int y)
    {
        if (_driver.HostForm != null)
        {
            _driver.HostForm.Invoke((Action)(() =>
            {
                _driver.HostForm.Location = new Point(x, y);
            }));
        }
    }

    /// <summary>Opens the Chromium DevTools window for this WebView.</summary>
    public void OpenDevTools()
    {
        _driver.WebView.Invoke((Action)(() => 
        {
            _driver.WebView.CoreWebView2.OpenDevToolsWindow();
        }));
    }
}
