using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class FontSheetlet<T> : ISheetlet<T>
    where T : IFontConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        var rules = new List<StyleRule>
        {
            // Default font
            E().Prop(Label.StylePropertyFont, config.BaseFont.GetFont(config.CommonFontSizes[0].Item2))
        };

        foreach (var (name, size) in config.CommonFontSizes)
        {
            foreach (var kind in Enum.GetValues<FontKind>())
            {
                // TODO: it's always null prefix by default
                var builder = E().Class(GetFontClass(kind));

                if (name is not null)
                    builder.Class(name);

                builder.Prop(Label.StylePropertyFont, config.BaseFont.GetFont(size, kind));

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
    private static string GetFontClass(FontKind kind, string? prefix = null)
    {
        var kindStr = kind.ToString().ToLowerInvariant();
        return prefix is null ? $"font-{kindStr}" : $"{prefix}-{kindStr}";
    }
}
