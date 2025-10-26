using Robust.Client.Graphics;

namespace Content.Client.Stylesheets.Fonts;

/// <summary>
/// Standard font types used in content.
/// </summary>
/// <seealso cref="IFontAccessor"/>
/// <seealso cref="IFontSelectionManager"/>
public enum StandardFontType : byte
{
    /// <summary>
    /// Font for main bodies of text. Should be the default for everything.
    /// </summary>
    Main,

    /// <summary>
    /// Font used for titles of things.
    /// </summary>
    Title,

    /// <summary>
    /// Font used for titles of IC UIs like machines.
    /// </summary>
    MachineTitle,

    /// <summary>
    /// Monospace font.
    /// </summary>
    Monospace,
}

/// <summary>
/// Interface for getting standard fonts in the game.
/// </summary>
/// <seealso cref="IFontSelectionManager"/>
public interface IFontAccessor
{
    /// <summary>
    /// Get a specified font.
    /// </summary>
    /// <param name="type">The standard type of font to get</param>
    /// <param name="size">The point size the font should be gotten at.</param>
    /// <param name="kind">The kind for the font.</param>
    Font GetFont(StandardFontType type, int size, FontKind kind = FontKind.Regular);
}

