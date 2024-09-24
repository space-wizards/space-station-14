using System.Numerics;
using Content.Client.Stylesheets.Redux.Fonts;
using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class MenuButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig, IIconConfig
{
    private static MutableSelectorElement CButton()
    {
        return E<MenuButton>();
    }

    public override StyleRule[] GetRules(T sheet, object config)
    {
        var cfg = (IButtonConfig)sheet;

        var buttonTex = sheet.GetTextureOr(cfg.BaseButtonPath, NanotrasenStylesheet.TextureRoot);
        var topButtonBase = new StyleBoxTexture
        {
            Texture = buttonTex,
        };
        topButtonBase.SetPatchMargin(StyleBox.Margin.All, 10);
        topButtonBase.SetPadding(StyleBox.Margin.All, 0);
        topButtonBase.SetContentMarginOverride(StyleBox.Margin.All, 0);

        var topButtonOpenRight = new StyleBoxTexture(topButtonBase)
        {
            Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(0, 0), new Vector2(14, 24))),
        };
        topButtonOpenRight.SetPatchMargin(StyleBox.Margin.Right, 0);

        var topButtonOpenLeft = new StyleBoxTexture(topButtonBase)
        {
            Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(14, 24))),
        };
        topButtonOpenLeft.SetPatchMargin(StyleBox.Margin.Left, 0);

        var topButtonSquare = new StyleBoxTexture(topButtonBase)
        {
            Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(3, 24))),
        };
        topButtonSquare.SetPatchMargin(StyleBox.Margin.Horizontal, 0);

        var rules = new List<StyleRule>
        {
            CButton().Class(StyleClass.ButtonSquare).Box(topButtonSquare),
            CButton().Class(StyleClass.ButtonOpenLeft).Box(topButtonOpenLeft),
            CButton().Class(StyleClass.ButtonOpenRight).Box(topButtonOpenRight),
            CButton().Box(cfg.ConfigureBaseButton(sheet)),
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
            E<Label>()
                .Class(MenuButton.StyleClassLabelTopButton)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(14, FontKind.Bold)),
            // new StyleProperty(Label.StylePropertyFont, notoSansDisplayBold14),
        };

        ButtonSheetlet<T>.MakeButtonRules<MenuButton>(rules, cfg.ButtonPalette, null);

        return rules.ToArray();
    }
}
