using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class OptionButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IIconConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IIconConfig iconCfg = sheet;

        var invertedTriangleTex =
            sheet.GetTextureOr(iconCfg.InvertedTriangleIconPath, NanotrasenStylesheet.TextureRoot);

        return
        [
            E<TextureRect>()
                .Class(OptionButton.StyleClassOptionTriangle)
                .Prop(TextureRect.StylePropertyTexture, invertedTriangleTex),
            E<Label>().Class(OptionButton.StyleClassOptionButton).AlignMode(Label.AlignMode.Center),
            E<PanelContainer>()
                .Class(OptionButton.StyleClassOptionsBackground)
                .Panel(new StyleBoxFlat(sheet.PrimaryPalette.Background)),
        ];
    }
}
