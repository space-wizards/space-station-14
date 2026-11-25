using Content.Client.Resources;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client.Stylesheets.Fonts;

/// <summary>
///     This class should have a base type. The whole font system is currently kind of bad and completely temporary.
///     This class is just here because it does sort of work.
///     TODO: fix (once engine support is added for font properties?)
/// </summary>
/// <param name="resCache"></param>
/// <param name="variant"></param>
[PublicAPI]
public sealed class NotoFontFamilyStack(IResourceCache resCache, string variant = "")
{
    /// <summary>
    ///     The primary font path, with string substitution markers.
    /// </summary>
    /// <remarks>
    ///     If using the default GetFontPaths function, the substitutions are as follows:
    ///     0 is the font kind.
    ///     1 is the font kind with BoldItalic replaced with Bold when it occurs.
    /// </remarks>
    private string _fontPrimary = $"/Fonts/NotoSans{variant}/NotoSans{variant}-{{0}}.ttf";

    /// <summary>
    ///     The symbols font path, with string substitution markers.
    /// </summary>
    /// <remarks>
    ///     If using the default GetFontPaths function, the substitutions are as follows:
    ///     0 is the font kind.
    ///     1 is the font kind with BoldItalic replaced with Bold when it occurs.
    /// </remarks>
    private string _fontSymbols = "/Fonts/NotoSans/NotoSansSymbols-{2}.ttf";

    /// <summary>
    ///     The fallback font path, exactly. (no string substitutions.)
    /// </summary>
    private string[] _extras = new[] { "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf" };

    public HashSet<FontKind> AvailableKinds = [FontKind.Regular, FontKind.Bold, FontKind.Italic, FontKind.BoldItalic];

    /// <summary>
    ///     This should return the paths of every font in this stack given the abstract members.
    /// </summary>
    /// <param name="kind">Which font kind to use.</param>
    /// <returns>An array of </returns>
    private string[] GetFontPaths(FontKind kind)
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
            string.Format(_fontPrimary, kindStr, simpleKindStr, boldOrRegularStr),
            string.Format(_fontSymbols, kindStr, simpleKindStr, boldOrRegularStr),
        };
        fontList.AddRange(_extras);
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
