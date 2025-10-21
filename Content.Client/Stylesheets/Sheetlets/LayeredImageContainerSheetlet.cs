using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.Stylesheets.Sheetlets;
using Content.Client.UserInterface.Controls;
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

        return [
            // Adds a raised border with rounded corners around a UI element
            E<LayeredImageContainer>().Class("PanelMount")
                .Prop(LayeredImageContainer.StylePropertyMinimumContentMargin, new Thickness(10, 10, 16, 16)),

            //TODO.eoin See if we can replace these with Child() - namespace conflicts!
            new MutableSelectorChild().Parent(E<LayeredImageContainer>().Class("PanelMount"))
                .Child(E<PanelContainer>().Identifier("Foreground1"))
                .Prop(PanelContainer.StylePropertyPanel, panelMountBaseStyleBox)
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#25252a")),

            new MutableSelectorChild().Parent(E<LayeredImageContainer>().Class("PanelMount"))
                .Child(E<PanelContainer>().Identifier("Foreground2"))
                .Prop(PanelContainer.StylePropertyPanel, panelMountHighlightStyleBox),

            new MutableSelectorChild().Parent(E<LayeredImageContainer>().Class("BrightAngleRectOutline"))
                .Child(E<PanelContainer>().Identifier("Background1"))
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.BaseStyleBox(sheet))
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#4a5466")),

            new MutableSelectorChild().Parent(E<LayeredImageContainer>().Class("BrightAngleRectOutline"))
                .Child(E<PanelContainer>().Identifier("Background2"))
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxTexture {
                        Texture = ResCache.GetTexture("/Textures/Interface/Nano/button_outline.svg.96dpi.png"),
                        PatchMarginLeft = 10,
                        PatchMarginTop = 10,
                        PatchMarginRight = 10,
                        PatchMarginBottom = 10,
                })
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#00000066")),
        ];
    }
}
