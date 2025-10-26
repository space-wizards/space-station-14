using System.Collections.Frozen;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Fonts;

/// <summary>
/// A set of font files for displaying a single font family.
/// </summary>
/// <seealso cref="FontFamilyStackBuilder"/>
public sealed class FontFamilyStack
{
    private readonly FrozenDictionary<FontKind, ResPath[]> _fontPaths;

    [Access(typeof(FontFamilyStackBuilder))]
    internal FontFamilyStack(FrozenDictionary<FontKind, ResPath[]> fontPaths)
    {
        if (!fontPaths.ContainsKey(FontKind.Regular)
            || !fontPaths.ContainsKey(FontKind.Italic)
            || !fontPaths.ContainsKey(FontKind.Bold)
            || !fontPaths.ContainsKey(FontKind.BoldItalic))
        {
            throw new ArgumentException("Font Family Stack must contain all font kinds");
        }

        _fontPaths = fontPaths;
    }

    /// <summary>
    /// Get the list of fonts to load for a given <see cref="FontKind"/>.
    /// </summary>
    /// <returns>
    /// A list of fonts to load. These should all be loaded and rendered using a <see cref="StackedFont"/>
    /// </returns>
    public ResPath[] GetFontPaths(FontKind kind)
    {
        return _fontPaths[kind];
    }

    /// <summary>
    /// Start creating a new <see cref="FontFamilyStack"/>.
    /// </summary>
    /// <returns>A builder object that can be used to construct the <see cref="FontFamilyStack"/>.</returns>
    public static FontFamilyStackBuilder New()
    {
        return new FontFamilyStackBuilder();
    }
}

/// <summary>
/// A builder object used to construct a <see cref="FontFamilyStack"/>.
/// </summary>
/// <remarks>
/// <para>
/// The builder allows adding fonts for various <see cref="FontKind"/>s.
/// Only <see cref="FontKind.Regular"/> must be provided, other kinds will fall back gracefully.
/// </para>
/// </remarks>
public sealed class FontFamilyStackBuilder
{
    private readonly Dictionary<FontKind, List<ResPath>> _kinds = [];
    private readonly List<ResPath> _extra = [];

    /// <summary>
    /// Add font files that will be loaded for the given font kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If called twice on the same <see cref="FontKind"/>, the paths will be appended to the end for that kind.
    /// </para>
    /// </remarks>
    /// <param name="kind">The kind these font files apply to.</param>
    /// <param name="paths">The font file paths to load. Earlier paths are given higher priority.</param>
    /// <returns>The builder object, allowing easy chaining</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="paths"/> is empty.</exception>
    public FontFamilyStackBuilder AddKind(FontKind kind, params ResPath[] paths)
    {
        if (paths.Length == 0)
            throw new ArgumentException("Must provide at least one path", nameof(paths));

        var list = _kinds.GetOrNew(kind);
        list.AddRange(paths);
        return this;
    }

    /// <summary>
    /// Add extra font files that will be loaded for every font kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If called twice, the paths will be appended to the end of the previous call.
    /// </para>
    /// <para>
    /// Extra fonts are loaded after fonts added for a specific font.
    /// </para>
    /// </remarks>
    /// <param name="paths">The font file paths to load. Earlier paths are given higher priority.</param>
    /// <returns>The builder object, allowing easy chaining</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="paths"/> is empty.</exception>
    public FontFamilyStackBuilder AddExtra(params ResPath[] paths)
    {
        if (paths.Length == 0)
            throw new ArgumentException("Must provide at least one path", nameof(paths));

        _extra.AddRange(paths);
        return this;
    }

    /// <summary>
    /// Finish constructing the <see cref="FontFamilyStack"/>
    /// </summary>
    /// <returns>The finished <see cref="FontFamilyStack"/></returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no <see cref="FontKind.Regular"/> was added via <see cref="AddKind"/>.
    /// </exception>
    public FontFamilyStack Build()
    {
        var newDict = _kinds.ToDictionary(kv => kv.Key, kv => kv.Value.Concat(_extra).ToArray());

        if (!newDict.TryGetValue(FontKind.Regular, out var regularValue))
            throw new InvalidOperationException("Font stack must have regular kind!");

        if (!newDict.ContainsKey(FontKind.BoldItalic))
            newDict.Add(FontKind.BoldItalic, newDict.GetValueOrDefault(FontKind.Bold) ?? regularValue);

        newDict.TryAdd(FontKind.Bold, regularValue);
        newDict.TryAdd(FontKind.Italic, regularValue);

        return new FontFamilyStack(newDict.ToFrozenDictionary());
    }

    public static implicit operator FontFamilyStack(FontFamilyStackBuilder builder)
    {
        return builder.Build();
    }
}
