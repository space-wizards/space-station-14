using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class OptionButtonSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var iconCfg = (IIconConfig) sheet;

        return
        [
            E<TextureRect>()
                .Class(OptionButton.StyleClassOptionTriangle)
                .Prop(TextureRect.StylePropertyTexture, sheet.GetTexture(iconCfg.InvertedTriangleIconPath)),
            E<Label>().Class(OptionButton.StyleClassOptionButton).AlignMode(Label.AlignMode.Center),
        ];
    }
}
