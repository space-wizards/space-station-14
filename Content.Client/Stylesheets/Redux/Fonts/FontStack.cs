using Content.Client.Resources;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client.Stylesheets.Redux.Fonts;

[PublicAPI]
public abstract class FontStack(IResourceCache resCache)
{
    /// <summary>
    ///     The primary font path, with string substitution markers.
    /// </summary>
    /// <remarks>
    ///     If using the default GetFontPaths function, the substitutions are as follows:
    ///     0 is the font kind.
    ///     1 is the font kind with BoldItalic replaced with Bold when it occurs.
    /// </remarks>
    public abstract string FontPrimary { get; }
    /// <summary>
    ///     The symbols font path, with string substitution markers.
    /// </summary>
    /// <remarks>
    ///     If using the default GetFontPaths function, the substitutions are as follows:
    ///     0 is the font kind.
    ///     1 is the font kind with BoldItalic replaced with Bold when it occurs.
    /// </remarks>
    public abstract string FontSymbols { get; }

    /// <summary>
    ///     The fallback font path, exactly. (no string substitutions.)
    /// </summary>
    public virtual string FontFallback => "/EngineFonts/NotoSans/NotoSans-Regular.ttf";

    /// <summary>
    ///     Any extra fonts that should be stuck in after Symbols but before the fallback.
    /// </summary>
    public abstract string[] Extra { get; }

    public virtual HashSet<FontKind> AvailableKinds => new() {FontKind.Regular, FontKind.Bold, FontKind.Italic, FontKind.BoldItalic};

    /// <summary>
    ///     This should return the paths of every font in this stack given the abstract members.
    /// </summary>
    /// <param name="kind">Which font kind to use.</param>
    /// <returns>An array of </returns>
    protected virtual string[] GetFontPaths(FontKind kind)
    {
        if (!AvailableKinds.Contains(kind))
        {
            if (kind == FontKind.BoldItalic && AvailableKinds.Contains(FontKind.Bold))
            {
                kind = FontKind.Bold;
            }
            else
            {
                kind = FontKind.Regular;
            }
        }

        var simpleKindStr = kind.SimplifyCompound().AsFileName();
        var boldOrRegularStr = kind.RegularOr(FontKind.Bold).AsFileName();

        var kindStr = kind.AsFileName();
        var fontList = new List<string>()
        {
            string.Format(FontPrimary, kindStr, simpleKindStr, boldOrRegularStr),
            string.Format(FontSymbols, kindStr, simpleKindStr, boldOrRegularStr),
        };
        fontList.AddRange(Extra);
        fontList.Add(FontFallback);
        return fontList.ToArray();
    }

    /// <summary>
    ///     Retrieves an in-style font, of the provided size and kind.
    /// </summary>
    /// <param name="size">Size of the font to provide.</param>
    /// <param name="kind">Optional font kind. Defaults to Regular.</param>
    /// <returns>A Font resource.</returns>
    public Font GetFont(int size, FontKind kind = FontKind.Regular)
    {
        //ALDebugTools.AssertContains(AvailableKinds, kind);
        var paths = GetFontPaths(kind);

        return resCache.GetFont(paths, size);
    }

    /// <summary>
    ///     The available kinds of font.
    /// </summary>
    public enum FontKind
    {
        Regular,
        Bold,
        Italic,
        BoldItalic
    }
}

[PublicAPI]
public static class FontKindExtensions
{
    public static string AsFileName(this FontStack.FontKind kind)
    {
        return kind switch
        {
            FontStack.FontKind.Regular => "Regular",
            FontStack.FontKind.Bold => "Bold",
            FontStack.FontKind.Italic => "Italic",
            FontStack.FontKind.BoldItalic => "BoldItalic",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public static bool IsBold(this FontStack.FontKind kind)
    {
        return kind == FontStack.FontKind.Bold || kind == FontStack.FontKind.BoldItalic;
    }

    public static bool IsItalic(this FontStack.FontKind kind)
    {
        return kind == FontStack.FontKind.Italic || kind == FontStack.FontKind.BoldItalic;
    }

    public static FontStack.FontKind SimplifyCompound(this FontStack.FontKind kind)
    {
        return kind switch
        {
            FontStack.FontKind.BoldItalic => FontStack.FontKind.Bold,
            _ => kind
        };
    }


    public static FontStack.FontKind RegularOr(this FontStack.FontKind kind, FontStack.FontKind other)
    {
        return kind switch
        {
            FontStack.FontKind.Regular => FontStack.FontKind.Regular,
            _ => other
        };
    }
}
