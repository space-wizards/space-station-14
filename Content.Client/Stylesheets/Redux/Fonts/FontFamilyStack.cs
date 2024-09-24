using Content.Client.Resources;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client.Stylesheets.Redux.Fonts;

[PublicAPI]
public abstract class FontFamilyStack(IResourceCache resCache)
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

    public virtual HashSet<FontKind> AvailableKinds => new()
        { FontKind.Regular, FontKind.Bold, FontKind.Italic, FontKind.BoldItalic };

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
}
