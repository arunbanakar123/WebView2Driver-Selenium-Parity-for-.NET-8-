// ============================================================
//  DownloadManager.cs
//  Tracks and configures file downloads.
// ============================================================
using Microsoft.Web.WebView2.Core;

namespace WebView2Driver.Browser;

/// <summary>
/// Simplifies observing and answering the <c>DownloadStarting</c> prompts.
/// </summary>
public sealed class DownloadManager
{
    private readonly Core.WebView2Driver _driver;
    private readonly string _defaultDownloadFolder;

    internal DownloadManager(Core.WebView2Driver driver, string downloadFolder)
    {
        _driver = driver;
        _defaultDownloadFolder = downloadFolder;
        _driver.WebView.Invoke((Action)(() => 
        {
            _driver.WebView.CoreWebView2.DownloadStarting += Core_DownloadStarting;
        }));
    }

    private void Core_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        // Suppress the Save As UI
        e.Handled = true;
        
        string filename = Path.GetFileName(e.ResultFilePath) ?? $"download_{Guid.NewGuid():N}";
        string fullPath = Path.Combine(_defaultDownloadFolder, filename);
        
        // Ensure folder exists
        Directory.CreateDirectory(_defaultDownloadFolder);
        
        e.ResultFilePath = fullPath;
        
        var download = e.DownloadOperation;
        download.StateChanged += delegate {
            if (download.State == CoreWebView2DownloadState.Completed)
            {
                // We emit a debug message strictly for internal dev visibility
                System.Diagnostics.Debug.WriteLine($"Download Manager: Completed '{fullPath}'");
            }
        };
    }
}
