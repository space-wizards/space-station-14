using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class PaperSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        // This REALLY has no business being style-independent but I don't want to touch that right now.
        var paperBackground = ResCache.GetTexture("/Textures/Interface/Paper/paper_background_default.svg.96dpi.png").IntoPatch(StyleBox.Margin.All, 16);
        paperBackground.Modulate = Color.FromHex("#eaedde");

        return new StyleRule[]
        {
            Element<PanelContainer>().Class("PaperDefaultBorder")
                .Prop(PanelContainer.StylePropertyPanel, paperBackground),
            Element<RichTextLabel>().Class("PaperWrittenText")
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12))
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#111111")),

            Element<RichTextLabel>().Class("LabelSubText")
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(10))
                .Prop(Label.StylePropertyFontColor, Color.DarkGray),

            Element<LineEdit>().Class("PaperLineEdit")
                .Prop(LineEdit.StylePropertyStyleBox, new StyleBoxEmpty()),
        };
    }
}
