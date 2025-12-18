using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.Sheetlets;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.Stylesheets.SheetletConfigs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.UserInterface.Controls;

[CommonSheetlet]
public sealed class LayeredImageContainerSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        IPanelConfig panelCfg = sheet;
        var panelMountBaseTex = ResCache.GetTexture("/Textures/Interface/Diegetic/PanelMountBase.svg.96dpi.png");
        var panelMountHighlightTex = ResCache.GetTexture("/Textures/Interface/Diegetic/PanelMountHighlight.svg.96dpi.png");
        var panelMountBaseStyleBox = new StyleBoxTexture
        {
            Texture = panelMountBaseTex,
            PatchMarginLeft = 16,
            PatchMarginTop = 16,
            PatchMarginRight = 24,
            PatchMarginBottom = 24
        };
        var panelMountHighlightStyleBox = new StyleBoxTexture
        {
            Texture = panelMountHighlightTex,
            PatchMarginLeft = 16,
            PatchMarginTop = 16,
            PatchMarginRight = 24,
            PatchMarginBottom = 24
        };

        var borderTex = sheet.GetTexture(panelCfg.GeometricPanelBorderPath).IntoPatch(StyleBox.Margin.All, 10);

        return [
            // Adds a raised border with rounded corners around a UI element
            E<LayeredImageContainer>().Class(LayeredImageContainer.StyleClassPanelMount)
                .Prop(LayeredImageContainer.StylePropertyMinimumContentMargin, new Thickness(10, 10, 16, 16)),

            E<LayeredImageContainer>().Class(LayeredImageContainer.StyleClassPanelMount)
                .ParentOf(E<PanelContainer>().Identifier("Foreground1"))
                .Prop(PanelContainer.StylePropertyPanel, panelMountBaseStyleBox),

            E<LayeredImageContainer>().Class(LayeredImageContainer.StyleClassPanelMount)
                .ParentOf(E<PanelContainer>().Identifier("Foreground2"))
                .Prop(PanelContainer.StylePropertyPanel, panelMountHighlightStyleBox),

            E<LayeredImageContainer>().Class(StyleClass.PanelDark)
                .ParentOf(E<PanelContainer>().Identifier("Foreground1"))
                .Prop(Control.StylePropertyModulateSelf, sheet.SecondaryPalette.BackgroundDark),

            /// Bright AngleRect with a subtle outline
            E<LayeredImageContainer>().Class(LayeredImageContainer.StyleClassBrightAngleRect)
                .ParentOf(E<PanelContainer>().Identifier("Background1"))
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.BaseStyleBox(sheet))
                .Prop(Control.StylePropertyModulateSelf, Palettes.Cyan.BackgroundLight),

            E<LayeredImageContainer>().Class(LayeredImageContainer.StyleClassBrightAngleRect)
                .ParentOf(E<PanelContainer>().Identifier("Background2"))
                .Prop(PanelContainer.StylePropertyPanel, borderTex)
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#00000040")),
        ];
    }
}
