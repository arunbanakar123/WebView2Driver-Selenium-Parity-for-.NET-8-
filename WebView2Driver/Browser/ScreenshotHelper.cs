// ============================================================
//  ScreenshotHelper.cs
//  Full page and element screenshots.
// ============================================================
using System.Drawing;
using System.IO;
using Microsoft.Web.WebView2.Core;

namespace WebView2Driver.Browser;

/// <summary>
/// Mirrors Selenium's <c>ITakesScreenshot</c>.
/// </summary>
public sealed class ScreenshotHelper
{
    private readonly Core.WebView2Driver _driver;

    internal ScreenshotHelper(Core.WebView2Driver driver)
    {
        _driver = driver;
    }

    /// <summary>
    /// Captures the visible viewport as a <c>System.Drawing.Bitmap</c>.
    /// </summary>
    public async Task<Bitmap> CaptureAsync()
    {
        using var stream = new MemoryStream();
        await (Task)_driver.WebView.Invoke((Func<Task>)(() => 
            _driver.WebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream)));
        return new Bitmap(stream);
    }

    /// <summary>
    /// Synchronously captures and saves the viewport screenshot to a file.
    /// </summary>
    public void SaveAsFile(string filePath)
    {
        var bmp = CaptureAsync().GetAwaiter().GetResult();
        bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
    }
}
