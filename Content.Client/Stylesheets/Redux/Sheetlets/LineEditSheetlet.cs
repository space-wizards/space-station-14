using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class LineEditSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var lineEditStylebox = sheet.GetTexture("lineedit.png").IntoPatch(StyleBox.Margin.All, 3);
        lineEditStylebox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

        return new StyleRule[]
        {
            E<LineEdit>()
                .Prop(LineEdit.StylePropertyStyleBox, lineEditStylebox),
            // TODO: Hardcoded colors bad, kill.
            Element<LineEdit>().Class(LineEdit.StyleClassLineEditNotEditable)
                .Prop("font-color", new Color(192, 192, 192)),
            Element<LineEdit>().Pseudo(LineEdit.StylePseudoClassPlaceholder)
                .Prop("font-color", Color.Gray),
            Element<TextEdit>().Pseudo(TextEdit.StylePseudoClassPlaceholder)
                .Prop("font-color", Color.Gray),
        };
    }
}
