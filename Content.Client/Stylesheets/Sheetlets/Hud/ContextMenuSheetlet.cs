using Content.Client.ContextMenu.UI;
using Content.Client.Resources;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.Verbs.UI;
using Content.Shared.Verbs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ContextMenuSheetlet<T> : Sheetlet<T>
    where T : PalettedStylesheet, IWindowConfig, IButtonConfig, IIconConfig
{
    // TODO: make this not hardcoded (I am too scared to change the context menu colors)
    private static readonly ColorPalette ContextButtonPalette = ColorPalette.FromHexBase("#000000") with
    {
        HoveredElement = Color.DarkSlateGray,
        Element = Color.FromHex("#1119"),
        PressedElement = Color.LightSlateGray,
    };

    public override StyleRule[] GetRules(T sheet, object config)
    {
        IWindowConfig windowCfg = sheet;

        var borderedWindowBackground = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(windowCfg.WindowBackgroundBorderedPath, NanotrasenStylesheet.TextureRoot),
        };
        borderedWindowBackground.SetPatchMargin(StyleBox.Margin.All, ContextMenuElement.ElementMargin);
        var buttonContext = new StyleBoxTexture { Texture = Texture.White };
        var contextMenuExpansionTexture = ResCache.GetTexture("/Textures/Interface/VerbIcons/group.svg.192dpi.png");
        var verbMenuConfirmationTexture = ResCache.GetTexture("/Textures/Interface/VerbIcons/group.svg.192dpi.png");

        var rules = new List<StyleRule>
        {
            // Context Menu window
            E<PanelContainer>()
                .Class(ContextMenuPopup.StyleClassContextMenuPopup)
                .Panel(borderedWindowBackground),

            // Context menu buttons
            E<ContextMenuElement>()
                .Class(ContextMenuElement.StyleClassContextMenuButton)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonContext),

            // Context Menu Labels
            E<RichTextLabel>()
                .Class(InteractionVerb.DefaultTextStyleClass)
                .Font(sheet.BaseFont.GetFont(12, FontKind.BoldItalic)),
            E<RichTextLabel>()
                .Class(ActivationVerb.DefaultTextStyleClass)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Bold)),
            E<RichTextLabel>()
                .Class(AlternativeVerb.DefaultTextStyleClass)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Italic)),
            E<RichTextLabel>()
                .Class(Verb.DefaultTextStyleClass)
                .Font(sheet.BaseFont.GetFont(12)),
            E<TextureRect>()
                .Class(ContextMenuElement.StyleClassContextMenuExpansionTexture)
                .Prop(TextureRect.StylePropertyTexture, contextMenuExpansionTexture),
            E<TextureRect>()
                .Class(VerbMenuElement.StyleClassVerbMenuConfirmationTexture)
                .Prop(TextureRect.StylePropertyTexture, verbMenuConfirmationTexture),

            // Context menu confirm buttons
            E<ContextMenuElement>()
                .Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonContext),
        };

        ButtonSheetlet<T>.MakeButtonRules<ContextMenuElement>(rules,
            ContextButtonPalette,
            ContextMenuElement.StyleClassContextMenuButton);
        ButtonSheetlet<T>.MakeButtonRules<ContextMenuElement>(rules,
            sheet.NegativePalette,
            ConfirmationMenuElement.StyleClassConfirmationContextMenuButton);

        return rules.ToArray();
    }
}
