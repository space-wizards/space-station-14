using JetBrains.Annotations;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux;

/// <summary>
///     The base class for all stylesheets, providing core functionality and helpers.
/// </summary>
[PublicAPI]
public abstract partial class PalettedStylesheet : BaseStylesheet
{
    protected PalettedStylesheet(object config) : base(config)
    {
    }

    public StyleRule[] BaseRules()
    {
        var rules = new List<StyleRule>()
        {
            E()
                .Prop(StyleProperties.PrimaryPalette, PrimaryPalette)
                .Prop(StyleProperties.SecondaryPalette, SecondaryPalette)
                .Prop(StyleProperties.PositivePalette, PositivePalette)
                .Prop(StyleProperties.NegativePalette, NegativePalette)
                .Prop(StyleProperties.HighlightPalette, HighlightPalette)
        };

        var palettes = new List<(string, Color[])>
        {
            (StyleClass.PrimaryColor, PrimaryPalette),
            (StyleClass.SecondaryColor, SecondaryPalette),
            (StyleClass.PositiveColor, PositivePalette),
            (StyleClass.NegativeColor, NegativePalette),
            (StyleClass.HighlightColor, HighlightPalette),
        };

        foreach (var (styleclass, palette) in palettes)
        {
            for (uint i = 0; i < palette.Length; i++)
            {
                rules.Add(
                    E()
                        .Class(StyleClass.GetColorClass(styleclass, i))
                        .Modulate(palette[i])
                );
            }
        }

        return rules.ToArray();
    }

    public StyleRule[] GetSheetletRules(Type sheetletTy)
    {
        return GetSheetletRules<PalettedStylesheet>(sheetletTy);
    }

    public StyleRule[] GetSheetletRules<T>()
        where T : Sheetlet<PalettedStylesheet>
    {
        return GetSheetletRules<T, PalettedStylesheet>();
    }

    public StyleRule[] GetAllSheetletRules<T>()
        where T : Attribute
    {
        return GetAllSheetletRules<PalettedStylesheet, T>();
    }
}
