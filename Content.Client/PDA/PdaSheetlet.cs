using Content.Client.PDA;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Sheetlets;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.PDA;

[CommonSheetlet]
public sealed class PdaSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        IPanelConfig panelCfg = sheet;
        IButtonConfig btnCfg = sheet;

        // TODO: This should have its own set of images, instead of using button cfg directly.
        var angleBorderRect =
            sheet.GetTexture(panelCfg.GeometricPanelBorderPath).IntoPatch(StyleBox.Margin.All, 10);

        return
        [
            //PDA - Backgrounds
            E<PanelContainer>()
                .Class("PdaContentBackground")
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.SquareStyleBox(sheet))
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#25252a")),

            E<PanelContainer>()
                .Class("PdaBackground")
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.SquareStyleBox(sheet))
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#000000")),

            E<PanelContainer>()
                .Class("PdaBackgroundRect")
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.BaseStyleBox((sheet)))
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#717059")),

            E<PanelContainer>()
                .Class("PdaBorderRect")
                .Prop(PanelContainer.StylePropertyPanel, angleBorderRect),

            //PDA - Buttons
            E<PdaSettingsButton>()
                .Pseudo(ContainerButton.StylePseudoClassNormal)
                .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.NormalBgColor))
                .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.EnabledFgColor)),

            E<PdaSettingsButton>()
                .Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.HoverColor))
                .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.EnabledFgColor)),

            E<PdaSettingsButton>()
                .Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.PressedColor))
                .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.EnabledFgColor)),

            E<PdaSettingsButton>()
                .Pseudo(ContainerButton.StylePseudoClassDisabled)
                .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.NormalBgColor))
                .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.DisabledFgColor)),

            E<PdaProgramItem>()
                .Pseudo(ContainerButton.StylePseudoClassNormal)
                .Prop(PdaProgramItem.StylePropertyBgColor, Color.FromHex(PdaProgramItem.NormalBgColor)),

            E<PdaProgramItem>()
                .Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(PdaProgramItem.StylePropertyBgColor, Color.FromHex(PdaProgramItem.HoverColor)),

            E<PdaProgramItem>()
                .Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(PdaProgramItem.StylePropertyBgColor, Color.FromHex(PdaProgramItem.HoverColor)),

            //PDA - Text
            E<Label>()
                .Class("PdaContentFooterText")
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#757575")),

            E<Label>()
                .Class("PdaWindowFooterText")
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#333d3b")),
        ];
    }
}

