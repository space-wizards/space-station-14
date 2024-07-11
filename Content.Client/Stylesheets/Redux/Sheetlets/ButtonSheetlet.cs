using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class ButtonSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var buttonCfg = (IButtonConfig) sheet;
        var iconCfg = (IIconConfig) sheet;

        var crossTex = sheet.GetTextureOr(iconCfg.CrossIconPath, NanotrasenStylesheet.TextureRoot);

        var rules = new List<StyleRule>
        {
            // Set textures for the kinds of buttons
            CButton()
                .Prop(ContainerButton.StylePropertyStyleBox, buttonCfg.ConfigureBaseButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenLeft)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonCfg.ConfigureOpenLeftButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenRight)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonCfg.ConfigureOpenRightButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenBoth)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonCfg.ConfigureOpenBothButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonSquare)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonCfg.ConfigureOpenSquareButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonSmall)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonCfg.ConfigureSmallButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonSmall)
                .ParentOf(E<Label>())
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(8)),
            CButton().Class(StyleClass.ButtonBig).ParentOf(E<Label>()).Font(sheet.BaseFont.GetFont(16)),

            // Cross Button (Red)
            E<TextureButton>()
                .Class(StyleClass.CrossButtonRed)
                .Prop(TextureButton.StylePropertyTexture, crossTex),

            // Ensure labels in buttons are aligned.
            E<Label>()
                .Class(Button.StyleClassButton)
                .AlignMode(Label.AlignMode.Center),
        };
        // Texture button modulation
        MakeButtonRules<TextureButton>(buttonCfg, rules, Palettes.AlphaModulate, null);
        MakeButtonRules<TextureButton>(buttonCfg, rules, sheet.NegativePalette, StyleClass.CrossButtonRed);

        MakeButtonRules(buttonCfg, rules, buttonCfg.ButtonPalette, null);
        MakeButtonRules(buttonCfg, rules, buttonCfg.PositiveButtonPalette, StyleClass.Positive);
        MakeButtonRules(buttonCfg, rules, buttonCfg.NegativeButtonPalette, StyleClass.Negative);

        return rules.ToArray();
    }

    public static void MakeButtonRules<T>(IButtonConfig _,
        List<StyleRule> rules,
        ColorPalette palette,
        string? styleclass)
        where T : Control
    {
        rules.AddRange(new StyleRule[]
        {
            E<T>().MaybeClass(styleclass).ButtonNormal().Modulate(palette.Element),
            E<T>().MaybeClass(styleclass).ButtonHovered().Modulate(palette.HoveredElement),
            E<T>().MaybeClass(styleclass).ButtonPressed().Modulate(palette.PressedElement),
            E<T>().MaybeClass(styleclass).ButtonDisabled().Modulate(palette.DisabledElement),
        });
    }

    public static void MakeButtonRules(IButtonConfig _,
        List<StyleRule> rules,
        ColorPalette palette,
        string? styleclass)
    {
        rules.AddRange(new StyleRule[]
        {
            E().MaybeClass(styleclass).ButtonNormal().Prop(Control.StylePropertyModulateSelf, palette.Element),
            E().MaybeClass(styleclass).ButtonHovered().Prop(Control.StylePropertyModulateSelf, palette.HoveredElement),
            E().MaybeClass(styleclass).ButtonPressed().Prop(Control.StylePropertyModulateSelf, palette.PressedElement),
            E()
                .MaybeClass(styleclass)
                .ButtonDisabled()
                .Prop(Control.StylePropertyModulateSelf, palette.DisabledElement),
        });
    }

    internal static MutableSelectorElement CButton()
    {
        return E<ContainerButton>().Class(ContainerButton.StyleClassButton);
    }
}
