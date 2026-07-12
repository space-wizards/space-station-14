using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class LineEditSheetlet<T> : ISheetlet<T>
    where T : ILineEditConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        var lineEditStylebox = factory.GetTexture(config.LineEditPath)
            .IntoPatch(StyleBox.Margin.All, 3);
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
