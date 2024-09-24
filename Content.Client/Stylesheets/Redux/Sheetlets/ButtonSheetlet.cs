using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class ButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig, IIconConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig buttonCfg = sheet;
        IIconConfig iconCfg = sheet;

        var crossTex = sheet.GetTextureOr(iconCfg.CrossIconPath, NanotrasenStylesheet.TextureRoot);

        var rules = new List<StyleRule>
        {
            // Set textures for the kinds of buttons
            CButton()
                .Box(buttonCfg.ConfigureBaseButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenLeft)
                .Box(buttonCfg.ConfigureOpenLeftButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenRight)
                .Box(buttonCfg.ConfigureOpenRightButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenBoth)
                .Box(buttonCfg.ConfigureOpenBothButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonSquare)
                .Box(buttonCfg.ConfigureOpenSquareButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonSmall)
                .Box(buttonCfg.ConfigureSmallButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonSmall)
                .ParentOf(E<Label>())
                .Font(sheet.BaseFont.GetFont(8)),
            CButton().Class(StyleClass.ButtonBig).ParentOf(E<Label>()).Font(sheet.BaseFont.GetFont(16)),

            // Cross Button (Red)
            E<TextureButton>()
                .Class(StyleClass.CrossButtonRed)
                .Prop(TextureButton.StylePropertyTexture, crossTex),

            // Ensure labels in buttons are aligned.
            E<Label>()
                // ReSharper disable once AccessToStaticMemberViaDerivedType
                .Class(Button.StyleClassButton)
                .AlignMode(Label.AlignMode.Center),
        };
        // Texture button modulation
        MakeButtonRules<TextureButton>(rules, Palettes.AlphaModulate, null);
        MakeButtonRules<TextureButton>(rules, sheet.NegativePalette, StyleClass.CrossButtonRed);

        MakeButtonRules(rules, buttonCfg.ButtonPalette, null);
        MakeButtonRules(rules, buttonCfg.PositiveButtonPalette, StyleClass.Positive);
        MakeButtonRules(rules, buttonCfg.NegativeButtonPalette, StyleClass.Negative);

        return rules.ToArray();
    }

    public static void MakeButtonRules<T>(
        List<StyleRule> rules,
        ColorPalette palette,
        string? styleclass)
        where T : Control
    {
        rules.AddRange(new StyleRule[]
        {
            E<T>().MaybeClass(styleclass).PseudoNormal().Modulate(palette.Element),
            E<T>().MaybeClass(styleclass).PseudoHovered().Modulate(palette.HoveredElement),
            E<T>().MaybeClass(styleclass).PseudoPressed().Modulate(palette.PressedElement),
            E<T>().MaybeClass(styleclass).PseudoDisabled().Modulate(palette.DisabledElement),
        });
    }

    public static void MakeButtonRules(
        List<StyleRule> rules,
        ColorPalette palette,
        string? styleclass)
    {
        rules.AddRange(new StyleRule[]
        {
            E().MaybeClass(styleclass).PseudoNormal().Prop(Control.StylePropertyModulateSelf, palette.Element),
            E().MaybeClass(styleclass).PseudoHovered().Prop(Control.StylePropertyModulateSelf, palette.HoveredElement),
            E().MaybeClass(styleclass).PseudoPressed().Prop(Control.StylePropertyModulateSelf, palette.PressedElement),
            E()
                .MaybeClass(styleclass)
                .PseudoDisabled()
                .Prop(Control.StylePropertyModulateSelf, palette.DisabledElement),
        });
    }

    private static MutableSelectorElement CButton()
    {
        return E<ContainerButton>().Class(ContainerButton.StyleClassButton);
    }
}
