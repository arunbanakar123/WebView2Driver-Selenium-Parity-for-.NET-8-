// ============================================================
//  Keys.cs
//  Special key constants — mirrors Selenium's Keys class.
// ============================================================
namespace WebView2Driver.Interactions;

/// <summary>
/// Key name constants for use with <see cref="Actions"/>,
/// <see cref="Keyboard"/>, and <see cref="WebView2Element.SendKeys"/>.
/// Values match the Chromium DevTools Protocol <c>Input.dispatchKeyEvent</c> key names.
/// </summary>
public static class Keys
{
    public const string Null        = "\uE000";
    public const string Cancel      = "\uE001";
    public const string Help        = "\uE002";
    public const string Backspace   = "\uE003";
    public const string Tab         = "\uE004";
    public const string Clear       = "\uE005";
    public const string Return      = "\uE006";
    public const string Enter       = "\uE007";
    public const string Shift       = "\uE008";
    public const string Control     = "\uE009";
    public const string Alt         = "\uE00A";
    public const string Pause       = "\uE00B";
    public const string Escape      = "\uE00C";
    public const string Space       = "\uE00D";
    public const string PageUp      = "\uE00E";
    public const string PageDown    = "\uE00F";
    public const string End         = "\uE010";
    public const string Home        = "\uE011";
    public const string Left        = "\uE012";
    public const string ArrowLeft   = "\uE012";
    public const string Up          = "\uE013";
    public const string ArrowUp     = "\uE013";
    public const string Right       = "\uE014";
    public const string ArrowRight  = "\uE014";
    public const string Down        = "\uE015";
    public const string ArrowDown   = "\uE015";
    public const string Insert      = "\uE016";
    public const string Delete      = "\uE017";
    public const string Semicolon   = "\uE018";
    public new const string Equals  = "\uE019";
    public const string NumberPad0  = "\uE01A";
    public const string NumberPad1  = "\uE01B";
    public const string NumberPad2  = "\uE01C";
    public const string NumberPad3  = "\uE01D";
    public const string NumberPad4  = "\uE01E";
    public const string NumberPad5  = "\uE01F";
    public const string NumberPad6  = "\uE020";
    public const string NumberPad7  = "\uE021";
    public const string NumberPad8  = "\uE022";
    public const string NumberPad9  = "\uE023";
    public const string Multiply    = "\uE024";
    public const string Add         = "\uE025";
    public const string Separator   = "\uE026";
    public const string Subtract    = "\uE027";
    public const string Decimal     = "\uE028";
    public const string Divide      = "\uE029";
    public const string F1          = "\uE031";
    public const string F2          = "\uE032";
    public const string F3          = "\uE033";
    public const string F4          = "\uE034";
    public const string F5          = "\uE035";
    public const string F6          = "\uE036";
    public const string F7          = "\uE037";
    public const string F8          = "\uE038";
    public const string F9          = "\uE039";
    public const string F10         = "\uE03A";
    public const string F11         = "\uE03B";
    public const string F12         = "\uE03C";
    public const string Meta        = "\uE03D";
    public const string Command     = "\uE03D";
    public const string ZenkakuHankaku = "ZenkakuHankaku";
}
