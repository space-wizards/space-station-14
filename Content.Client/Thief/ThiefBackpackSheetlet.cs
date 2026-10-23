using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Colorspace;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Thief;

[CommonSheetlet]
public sealed class ThiefBackpackSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IPanelConfig, IButtonConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        StyleBoxFlat TheftStyleBox(Color backgroundColor) => new()
        {
            BackgroundColor = backgroundColor,
            BorderColor = sheet.DeepPanelBorderColor,
            BorderThickness = new Thickness(2f)
        };

        var theftSetBackground = TheftStyleBox(sheet.DeepPanelBackgroundColor);
        var theftSetBackgroundHovered = TheftStyleBox(sheet.DeepPanelBackgroundColor.NudgeLightness(0.06f));
        // Guess some color shift that will align the pressed colour with our dark background:
        var theftSetBackgroundPressed = TheftStyleBox(
            sheet.ButtonPalette.PressedElement.NudgeLightness(-0.2f).NudgeChroma(-0.07f));
        var checkmarkTex = ResCache.GetTexture("/Textures/Interface/Nano/checkmark.svg.96dpi.png");

        return
        [
            E<ThiefBackpackSet>()
                .PseudoNormal()
                .Prop(ContainerButton.StylePropertyStyleBox, theftSetBackground),
            E<ThiefBackpackSet>()
                .PseudoHovered()
                .Prop(ContainerButton.StylePropertyStyleBox, theftSetBackgroundHovered),
            E<ThiefBackpackSet>()
                .PseudoPressed()
                .Prop(ContainerButton.StylePropertyStyleBox, theftSetBackgroundPressed),

            E<ThiefBackpackSet>()
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<Separator>())
                .Prop(Separator.StylePropertyColor, sheet.DeepPanelBorderColor),

            E<ThiefBackpackSet>()
                .PseudoPressed()
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<TextureRect>().Identifier("SelectedIndicator"))
                .Prop(TextureRect.StylePropertyTexture, checkmarkTex)
                .Prop(Control.StylePropertyModulateSelf, sheet.PositivePalette.Base)
        ];
    }
}
