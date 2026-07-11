using System.Numerics;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class MenuButtonSheetlet<T> : ISheetlet<T>
    where T : IButtonConfig, IIconConfig, IFontConfig, IPaletteConfig
{
    private static MutableSelectorElement CButton()
    {
        return E<MenuButton>();
    }

    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        var buttonTex = factory.GetTexture(config.BaseButtonPath);
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
            CButton().Box(StyleBoxHelpers.BaseStyleBox(factory, config)),
            CButton()
                .Class(StyleClass.ButtonOpenLeft)
                .Prop(ContainerButton.StylePropertyStyleBox, StyleBoxHelpers.OpenLeftStyleBox(factory, config)),
            CButton()
                .Class(StyleClass.ButtonOpenRight)
                .Prop(ContainerButton.StylePropertyStyleBox, StyleBoxHelpers.OpenRightStyleBox(factory, config)),
            CButton()
                .Class(StyleClass.ButtonOpenBoth)
                .Prop(ContainerButton.StylePropertyStyleBox, StyleBoxHelpers.SquareStyleBox(factory, config)),
            CButton()
                .Class(StyleClass.ButtonSquare)
                .Prop(ContainerButton.StylePropertyStyleBox, StyleBoxHelpers.SquareStyleBox(factory, config)),
            E<Label>()
                .Class(MenuButton.StyleClassLabelTopButton)
                .Prop(Label.StylePropertyFont, config.BaseFont.GetFont(14, FontKind.Bold)),
            // new StyleProperty(Label.StylePropertyFont, notoSansDisplayBold14),
        };

        ButtonSheetlet<T>.MakeButtonRules<MenuButton>(rules, config.ButtonPalette, null);
        ButtonSheetlet<T>.MakeButtonRules<MenuButton>(rules, config.PositiveButtonPalette, StyleClass.Positive);
        ButtonSheetlet<T>.MakeButtonRules<MenuButton>(rules, config.NegativeButtonPalette, StyleClass.Negative);

        return rules.ToArray();
    }
}
