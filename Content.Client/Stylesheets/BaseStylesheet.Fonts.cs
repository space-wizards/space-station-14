using Content.Client.Stylesheets.Fonts;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets;

public abstract partial class BaseStylesheet : IStyleResources
{
    private static readonly FontKind[] AllFontKinds =
        [FontKind.Regular, FontKind.Bold, FontKind.Italic, FontKind.BoldItalic];

    [Dependency] protected readonly ISandboxHelper SandboxHelper = default!;
    [Dependency] protected readonly IReflectionManager ReflectionManager = default!;
    [Dependency] protected internal readonly IResourceCache ResCache = default!;
    [Dependency] protected readonly IFontSelectionManager FontSelection = null!;

    public Stylesheet Stylesheet { get; init; }

    /// <summary>
    /// The fonts for this stylesheet.
    /// </summary>
    public virtual IFontAccessor Fonts => FontSelection;

    [Obsolete("Use the newer font stack instead")]
    public abstract NotoFontFamilyStack BaseFont { get; }

    /// <summary>
    ///     Get the style rules for the given font stack, with the provided sizes.
    ///     Does not set the 'default' font, but does create rules for every combination of font kind and font size.
    ///     This is intended for font sizes you think will be common/generally useful, for less common usecases prefer specifying the font explicitly.
    /// </summary>
    /// <param name="prefix">The prefix for the style classes, if any.</param>
    /// <param name="stack">Font stack to use</param>
    /// <param name="sizes">A set of styleclasses and the associated size of font to use.</param>
    /// <returns>A rules list containing all combinations.</returns>
    /// <remarks>Use <see cref="GetFontClass"/> to get the appropriate styleclass for a font choice.</remarks>
    [Obsolete("Use the newer font stack instead")]
    protected StyleRule[] GetRulesForFont(string? prefix, NotoFontFamilyStack stack, List<(string?, int)> sizes) // TODO: NotoFontFamilyStack is temporary
    {
        var rules = new List<StyleRule>();

        foreach (var (name, size) in sizes)
        {
            foreach (var kind in stack.AvailableKinds)
            {
                var builder = E().Class(GetFontClass(kind, prefix));

                if (name is not null)
                    builder.Class(name);

                builder.Prop(Label.StylePropertyFont, stack.GetFont(size, kind));

                rules.Add(builder);
            }
        }

        return rules.ToArray();
    }

    /// <summary>
    /// Get default style rules for a <see cref="StandardFontType"/>.
    /// </summary>
    /// <param name="prefix">The prefix for the style classes, if any.</param>
    /// <param name="type">The font type to use.</param>
    /// <param name="sizes">A set of styleclasses and the associated size of font to use.</param>
    /// <remarks>
    /// <para>
    /// Does not set the 'default' font, but does create rules for every combination of font kind and font size.
    /// This is intended for font sizes you think will be common/generally useful,
    /// for less common usecases prefer specifying the font explicitly.
    /// </para>
    /// <para>
    /// Use <see cref="GetFontClass"/> to get the appropriate styleclass for a font choice.
    /// </para>
    /// </remarks>
    protected StyleRule[] GetRulesForFont(
        string? prefix,
        StandardFontType type,
        List<(string?, int)> sizes)
    {
        var rules = new List<StyleRule>();

        foreach (var (name, size) in sizes)
        {
            foreach (var kind in AllFontKinds)
            {
                var builder = E().Class(GetFontClass(kind, prefix));

                if (name is not null)
                    builder.Class(name);

                builder.Prop(Label.StylePropertyFont, Fonts.GetFont(type, size, kind));

                rules.Add(builder);
            }
        }

        return rules.ToArray();
    }

    /// <summary>
    ///     Returns the appropriate styleclass for the given font configuration.
    /// </summary>
    /// <param name="kind"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public static string GetFontClass(FontKind kind, string? prefix = null)
    {
        var kindStr = kind.ToString().ToLowerInvariant();
        return prefix is null ? $"font-{kindStr}" : $"{prefix}-{kindStr}";
    }
}
