using System.Numerics;
using Content.Client.Stylesheets.Redux.SheetletConfig;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

public abstract class ButtonSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var cfg = (IButtonConfig) sheet;

        var rules = new List<StyleRule>
        {
            // Set textures for the kinds of buttons
            CButton()
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureBaseButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenLeft)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenLeftButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenRight)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenRightButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenBoth)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenBothButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonSquare)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenSquareButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonSmall)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureSmallButton(sheet)),
            CButton()
                .Class(StyleClass.ButtonSmall)
                .ParentOf(E<Label>())
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(8)),
            CButton().Class(StyleClass.ButtonBig).ParentOf(E<Label>()).Font(sheet.BaseFont.GetFont(16)),

            // Cross Button (Red)
            E<TextureButton>()
                .Class(StyleClass.CrossButtonRed)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture("cross.svg.png")),

            // Ensure labels in buttons are aligned.
            E<Label>()
                .Class(Button.StyleClassButton)
                .AlignMode(Label.AlignMode.Center),
        };
        // Texture button modulation
        MakeButtonRules<TextureButton>(cfg, rules, Palettes.AlphaModulate, null);
        MakeButtonRules<TextureButton>(cfg, rules, sheet.NegativePalette, StyleClass.CrossButtonRed);

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
            E().MaybeClass(styleclass).ButtonNormal().Prop(Button.StylePropertyModulateSelf, palette.Element),
            E().MaybeClass(styleclass).ButtonHovered().Prop(Button.StylePropertyModulateSelf, palette.HoveredElement),
            E().MaybeClass(styleclass).ButtonPressed().Prop(Button.StylePropertyModulateSelf, palette.PressedElement),
            E().MaybeClass(styleclass).ButtonDisabled().Prop(Button.StylePropertyModulateSelf, palette.DisabledElement),
        });
    }

    internal static MutableSelectorElement CButton()
    {
        return E<ContainerButton>().Class(ContainerButton.StyleClassButton);
    }
}
