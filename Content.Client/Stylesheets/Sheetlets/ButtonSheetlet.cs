using System.Numerics;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class ButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig, IIconConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig buttonCfg = sheet;
        IIconConfig iconCfg = sheet;

        var crossTex = sheet.GetTextureOr(iconCfg.CrossIconPath, NanotrasenStylesheet.TextureRoot);
        var refreshTex = sheet.GetTextureOr(iconCfg.RefreshIconPath, NanotrasenStylesheet.TextureRoot);

        var rules = new List<StyleRule>
        {
            // Set textures for the kinds of buttons
            CButton()
                .Box(StyleBoxHelpers.BaseStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenLeft)
                .Box(StyleBoxHelpers.OpenLeftStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenRight)
                .Box(StyleBoxHelpers.OpenRightStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenBoth)
                .Box(StyleBoxHelpers.SquareStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonSquare)
                .Box(StyleBoxHelpers.SquareStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonSmall)
                .Box(StyleBoxHelpers.SmallStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonSmall)
                .ParentOf(E<Label>())
                .Font(sheet.BaseFont.GetFont(8)),
            CButton().Class(StyleClass.ButtonBig).ParentOf(E<Label>()).Font(sheet.BaseFont.GetFont(16)),

            // Cross Button (Red)
            E<TextureButton>()
                .Class(StyleClass.CrossButtonRed)
                .Prop(TextureButton.StylePropertyTexture, crossTex),

            // Refresh Button
            E<TextureButton>()
                .Class(StyleClass.RefreshButton)
                .Prop(TextureButton.StylePropertyTexture, refreshTex),

            // Ensure labels in buttons are aligned.
            E<Label>()
                // ReSharper disable once AccessToStaticMemberViaDerivedType
                .Class(Button.StyleClassButton)
                .AlignMode(Label.AlignMode.Center),

            // Have disabled button's text be faded
            CButton().PseudoDisabled().ParentOf(E<Label>()).FontColor(Color.FromHex("#E5E5E581")),
            CButton().PseudoDisabled().ParentOf(E()).ParentOf(E<Label>()).FontColor(Color.FromHex("#E5E5E581")),
        };
        // Texture button modulation
        MakeButtonRules<TextureButton>(rules, Palettes.AlphaModulate, null);
        MakeButtonRules<TextureButton>(rules, sheet.NegativePalette, StyleClass.CrossButtonRed);

        MakeButtonRules(rules, buttonCfg.ButtonPalette, null);
        MakeButtonRules(rules, buttonCfg.PositiveButtonPalette, StyleClass.Positive);
        MakeButtonRules(rules, buttonCfg.NegativeButtonPalette, StyleClass.Negative);

        return rules.ToArray();
    }

    public static void MakeButtonRules<TC>(
        List<StyleRule> rules,
        ColorPalette palette,
        string? styleclass)
        where TC : Control
    {
        rules.AddRange([
            E<TC>().MaybeClass(styleclass).PseudoNormal().Modulate(palette.Element),
            E<TC>().MaybeClass(styleclass).PseudoHovered().Modulate(palette.HoveredElement),
            E<TC>().MaybeClass(styleclass).PseudoPressed().Modulate(palette.PressedElement),
            E<TC>().MaybeClass(styleclass).PseudoDisabled().Modulate(palette.DisabledElement),
        ]);
    }

    public static void MakeButtonRules(
        List<StyleRule> rules,
        ColorPalette palette,
        string? styleclass)
    {
        rules.AddRange([
            E().MaybeClass(styleclass).PseudoNormal().Prop(Control.StylePropertyModulateSelf, palette.Element),
            E().MaybeClass(styleclass).PseudoHovered().Prop(Control.StylePropertyModulateSelf, palette.HoveredElement),
            E().MaybeClass(styleclass).PseudoPressed().Prop(Control.StylePropertyModulateSelf, palette.PressedElement),
            E()
                .MaybeClass(styleclass)
                .PseudoDisabled()
                .Prop(Control.StylePropertyModulateSelf, palette.DisabledElement),
        ]);
    }

    private static MutableSelectorElement CButton()
    {
        return E<ContainerButton>().Class(ContainerButton.StyleClassButton);
    }
}

// this is currently the only other "helper" type class, if any more crop up consider making a specific directory for them
public static class StyleBoxHelpers
{
    // TODO: Figure out a nicer way to store/represent these hardcoded margins. This is icky.
    public static StyleBoxTexture BaseStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
    {
        var baseBox = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(sheet.BaseButtonPath, NanotrasenStylesheet.TextureRoot),
        };
        baseBox.SetPatchMargin(StyleBox.Margin.All, 10);
        baseBox.SetPadding(StyleBox.Margin.All, 1);
        baseBox.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        baseBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 14);
        return baseBox;
    }

    public static StyleBoxTexture OpenLeftStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
    {
        var openLeftBox = new StyleBoxTexture(BaseStyleBox(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTextureOr(sheet.OpenLeftButtonPath, NanotrasenStylesheet.TextureRoot),
                UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(14, 24))),
        };
        openLeftBox.SetPatchMargin(StyleBox.Margin.Left, 0);
        openLeftBox.SetContentMarginOverride(StyleBox.Margin.Left, 8);
        // openLeftBox.SetPadding(StyleBox.Margin.Left, 1);
        return openLeftBox;
    }

    public static StyleBoxTexture OpenRightStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
    {
        var openRightBox = new StyleBoxTexture(BaseStyleBox(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTextureOr(sheet.OpenRightButtonPath, NanotrasenStylesheet.TextureRoot),
                UIBox2.FromDimensions(new Vector2(0, 0), new Vector2(14, 24))),
        };
        openRightBox.SetPatchMargin(StyleBox.Margin.Right, 0);
        openRightBox.SetContentMarginOverride(StyleBox.Margin.Right, 8);
        openRightBox.SetPadding(StyleBox.Margin.Right, 1);
        return openRightBox;
    }

    public static StyleBoxTexture SquareStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
    {
        var openBothBox = new StyleBoxTexture(BaseStyleBox(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTextureOr(sheet.OpenBothButtonPath, NanotrasenStylesheet.TextureRoot),
                UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(3, 24))),
        };
        openBothBox.SetPatchMargin(StyleBox.Margin.Horizontal, 0);
        openBothBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
        openBothBox.SetPadding(StyleBox.Margin.Horizontal, 1);
        return openBothBox;
    }

    public static StyleBoxTexture SmallStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
    {
        var smallBox = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(sheet.SmallButtonPath, NanotrasenStylesheet.TextureRoot),
        };
        return smallBox;
    }
}
