// ============================================================
//  Keyboard.cs
//  Raw keyboard simulation — analogous to Selenium's IKeyboard.
// ============================================================
namespace WebView2Driver.Interactions;

/// <summary>
/// Exposes low-level keyboard simulation.
/// Uses basic <c>PostWebMessageAsString</c> or CDP to trigger typing.
/// </summary>
public sealed class Keyboard
{
    private readonly Core.WebView2Driver _driver;

    internal Keyboard(Core.WebView2Driver driver)
    {
        _driver = driver;
    }

    /// <summary>Sends a physical key-down event.</summary>
    public void PressKey(string keyToPress)
    {
        _driver.DevTools.CallMethodAsync("Input.dispatchKeyEvent", new
        {
            type = "keyDown",
            key = keyToPress,
            text = keyToPress.Length == 1 ? keyToPress : null
        }).GetAwaiter().GetResult();
    }

    /// <summary>Sends a physical key-up event.</summary>
    public void ReleaseKey(string keyToRelease)
    {
        _driver.DevTools.CallMethodAsync("Input.dispatchKeyEvent", new
        {
            type = "keyUp",
            key = keyToRelease
        }).GetAwaiter().GetResult();
    }

    /// <summary>Types the character sequence sequentially.</summary>
    public void SendKeys(string keySequence)
    {
        if (keySequence == null) return;
        foreach (char c in keySequence)
        {
            string keyName;
            string? text = null;

            // PUA mapping (Selenium compatibility)
            if (c >= '\uE000' && c <= '\uE03D')
            {
                keyName = c switch
                {
                    '\uE000' => "Unidentified",
                    '\uE003' => "Backspace",
                    '\uE004' => "Tab",
                    '\uE006' => "Enter",
                    '\uE007' => "Enter",
                    '\uE00C' => "Escape",
                    '\uE00D' => " ",
                    '\uE012' => "ArrowLeft",
                    '\uE013' => "ArrowUp",
                    '\uE014' => "ArrowRight",
                    '\uE015' => "ArrowDown",
                    '\uE017' => "Delete",
                    _ => "Unidentified"
                };
            }
            else
            {
                keyName = c.ToString();
                text = keyName;
                if (c == '\n' || c == '\r') { keyName = "Enter"; text = "\r"; }
            }

            _driver.DevTools.CallMethodAsync("Input.dispatchKeyEvent", new
            {
                type = "keyDown",
                key = keyName,
                text = text,
                unmodifiedText = text
            }).GetAwaiter().GetResult();

            _driver.DevTools.CallMethodAsync("Input.dispatchKeyEvent", new
            {
                type = "keyUp",
                key = keyName
            }).GetAwaiter().GetResult();
        }
    }
}
