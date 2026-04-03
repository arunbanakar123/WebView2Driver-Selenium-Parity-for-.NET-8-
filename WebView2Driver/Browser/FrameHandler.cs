// ============================================================
//  FrameHandler.cs
//  Handles switching between frames and iframes.
// ============================================================
namespace WebView2Driver.Browser;

/// <summary>
/// Manages resolving elements and executing JS within nested iframes.
/// WebView2 doesn't have native CDP frame-swapping in the same way Selenium does,
/// so we emulate it by maintaining the JS path to the current frame.
/// </summary>
public sealed class FrameHandler
{
    private readonly Core.WebView2Driver _driver;

    internal FrameHandler(Core.WebView2Driver driver)
    {
        _driver = driver;
    }

    // A javascript array expression of iframes representing the current path.
    // Default is empty array (meaning top-level window/document).
    private readonly List<string> _frameStack = new();

    /// <summary>
    /// Gets the JS prefix required to execute code in the current frame context.
    /// E.g. "window.frames[0].document"
    /// </summary>
    internal string GetContextExpression(string target = "document")
    {
        if (_frameStack.Count == 0) return target;
        
        string prefix = "window";
        foreach (string f in _frameStack)
        {
            prefix += $".frames[{f}]";
        }
        return target == "window" ? prefix : $"{prefix}.document";
    }

    /// <summary>Switches context to the iframe by ID or Name attribute.</summary>
    public void Frame(string nameOrId)
    {
        string jsName = JsStr(nameOrId);
        int idx = _driver.ExecuteScript<int?>($@"
            var ctx = {GetContextExpression()};
            var frames = ctx.querySelectorAll('iframe, frame');
            for(var i=0;i<frames.length;i++){{
                if(frames[i].id==={jsName} || frames[i].name==={jsName}) return i;
            }}
            return -1;
        ") ?? -1;

        if (idx == -1) throw new NoSuchFrameException($"No frame found with id or name: {nameOrId}");
        _frameStack.Add(idx.ToString());
    }

    /// <summary>Switches context to the iframe at the specified zero-based index.</summary>
    public void Frame(int index)
    {
        int count = _driver.ExecuteScript<int?>($"return {GetContextExpression()}.querySelectorAll('iframe, frame').length;") ?? 0;
        if (index < 0 || index >= count) throw new NoSuchFrameException($"No frame found at index: {index}");
        _frameStack.Add(index.ToString());
    }

    /// <summary>Switches context to the iframe resolving to the given element.</summary>
    public void Frame(Elements.WebView2Element frameElement)
    {
        int idx = _driver.ExecuteScript<int?>($@"
            var ctx = {GetContextExpression()};
            var frames = ctx.querySelectorAll('iframe, frame');
            var target = {frameElement.JsRef};
            for(var i=0;i<frames.length;i++){{
                if(frames[i]===target) return i;
            }}
            return -1;
        ") ?? -1;

        if (idx == -1) throw new NoSuchFrameException("The element provided is not a child frame of the current context.");
        _frameStack.Add(idx.ToString());
    }

    /// <summary>Switches back up one level in the frame hierarchy.</summary>
    public void ParentFrame()
    {
        if (_frameStack.Count > 0)
            _frameStack.RemoveAt(_frameStack.Count - 1);
    }

    /// <summary>Switches to the top-level main document.</summary>
    public void DefaultContent()
    {
        _frameStack.Clear();
    }

    private static string JsStr(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}

public class NoSuchFrameException : Exception
{
    public NoSuchFrameException(string msg) : base(msg) { }
}
