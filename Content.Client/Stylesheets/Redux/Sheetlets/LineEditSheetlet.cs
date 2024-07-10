using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class LineEditSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var lineEditCfg = (ILineEditConfig) sheet;

        var lineEditStylebox = sheet.GetTexture(lineEditCfg.LineEditPath).IntoPatch(StyleBox.Margin.All, 3);
        lineEditStylebox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

        return
        [
            E<LineEdit>()
                .Prop(LineEdit.StylePropertyStyleBox, lineEditStylebox),
            // TODO: Hardcoded colors bad, kill.
            E<LineEdit>()
                .Class(LineEdit.StyleClassLineEditNotEditable)
                .Prop("font-color", new Color(192, 192, 192)),
            E<LineEdit>()
                .Pseudo(LineEdit.StylePseudoClassPlaceholder)
                .Prop("font-color", Color.Gray),
            E<TextEdit>()
                .Pseudo(TextEdit.StylePseudoClassPlaceholder)
                .Prop("font-color", Color.Gray),
        ];
    }
}
