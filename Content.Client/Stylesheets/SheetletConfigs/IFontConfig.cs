using Content.Client.Stylesheets.Fonts;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IFontConfig : ISheetletConfig
{
    List<(string?, int)> CommonFontSizes { get; }
    NotoFontFamilyStack BaseFont { get; }
}
