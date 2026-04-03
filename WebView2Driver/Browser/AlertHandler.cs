// ============================================================
//  AlertHandler.cs
//  Handles JS alerts, confirms, prompts.
// ============================================================
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace WebView2Driver.Browser;

/// <summary>
/// Handles <c>window.alert</c>, <c>window.confirm</c>, and <c>window.prompt</c>.
/// Analogous to Selenium's <c>IAlert</c>.
/// </summary>
public sealed class AlertHandler
{
    private readonly Core.WebView2Driver _driver;

    internal AlertHandler(Core.WebView2Driver driver)
    {
        _driver = driver;
        _driver.WebView.Invoke((Action)(() => 
        {
            _driver.WebView.CoreWebView2.ScriptDialogOpening += OnScriptDialogOpening;
        }));
    }

    private readonly Queue<ScriptDialogData> _dialogQueue = new();

    private void OnScriptDialogOpening(object? sender, CoreWebView2ScriptDialogOpeningEventArgs e)
    {
        lock (_dialogQueue)
        {
            var def = e.GetDeferral();
            _dialogQueue.Enqueue(new ScriptDialogData(e, def));
        }
    }

    /// <summary>Checks if an unhandled alert is currently present.</summary>
    public bool IsPresent
    {
        get { lock (_dialogQueue) return _dialogQueue.Count > 0; }
    }

    /// <summary>The text of the active alert.</summary>
    public string Text
    {
        get
        {
            var d = PeekDialog();
            return d.Args.Message;
        }
    }

    /// <summary>Clicks OK on the active alert/confirm/prompt.</summary>
    public void Accept()
    {
        var d = DequeueDialog();
        _driver.WebView.Invoke((Action)(() => 
        {
            d.Args.Accept();
            d.Deferral.Complete();
        }));
    }

    /// <summary>Clicks Cancel on the active confirm/prompt.</summary>
    public void Dismiss()
    {
        var d = DequeueDialog();
        _driver.WebView.Invoke((Action)(() => 
        {
            // For alerts, Accept/Dismiss both just close it.
            // For confirm/prompt, Dismiss returns false/null.
            d.Deferral.Complete();
        }));
    }

    /// <summary>Types text into the active prompt dialog.</summary>
    public void SendKeys(string text)
    {
        var d = PeekDialog();
        if (d.Args.Kind != CoreWebView2ScriptDialogKind.Prompt)
            throw new InvalidOperationException("Active dialog is not a prompt.");
        
        d.Args.ResultText = text;
    }

    private ScriptDialogData PeekDialog()
    {
        lock (_dialogQueue)
        {
            if (_dialogQueue.Count == 0)
                throw new NoAlertPresentException("No alert is currently active.");
            return _dialogQueue.Peek();
        }
    }

    private ScriptDialogData DequeueDialog()
    {
        lock (_dialogQueue)
        {
            if (_dialogQueue.Count == 0)
                throw new NoAlertPresentException("No alert is currently active.");
            return _dialogQueue.Dequeue();
        }
    }

    private class ScriptDialogData
    {
        public CoreWebView2ScriptDialogOpeningEventArgs Args { get; }
        public CoreWebView2Deferral Deferral { get; }

        public ScriptDialogData(CoreWebView2ScriptDialogOpeningEventArgs args, CoreWebView2Deferral deferral)
        {
            Args = args;
            Deferral = deferral;
        }
    }
}

public class NoAlertPresentException : Exception
{
    public NoAlertPresentException(string msg) : base(msg) { }
}
