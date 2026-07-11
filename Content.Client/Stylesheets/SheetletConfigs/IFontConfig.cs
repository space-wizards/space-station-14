using Content.Client.Stylesheets.Fonts;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IFontConfig : ISheetletConfig
{
    List<(string?, int)> CommonFontSizes { get; }
    FontFamilyStack BaseFont { get; }
    FontFamilyStack MonoFont { get; }
    FontFamilyStack DisplayFont { get; }
    FontFamilyStack DecorativeFont { get; }
}
