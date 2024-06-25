using Content.Client.Stylesheets.Redux.Fonts;
using Content.Client.Stylesheets.Redux.NTSheetlets;
using Content.Client.Stylesheets.Redux.Sheetlets;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class MenuButtonSheetlet : Sheetlet<PalettedStylesheet>
{
    private static MutableSelectorElement CButton()
    {
        return E<MenuButton>();
    }

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var cfg = (IButtonConfig) sheet;

        var rules = new List<StyleRule>
        {
            CButton()
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureBaseButton(sheet)),
            CButton().Class(StyleClasses.ButtonOpenLeft)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenLeftButton(sheet)),
            CButton().Class(StyleClasses.ButtonOpenRight)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenRightButton(sheet)),
            CButton().Class(StyleClasses.ButtonOpenBoth)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenBothButton(sheet)),
            CButton().Class(StyleClasses.ButtonSquare)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenSquareButton(sheet)),
            E<Label>().Class(MenuButton.StyleClassLabelTopButton)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(14, FontStack.FontKind.Bold))
            // new StyleProperty(Label.StylePropertyFont, notoSansDisplayBold14),
        };

        ButtonSheetlet.MakeButtonRules<MenuButton>(cfg, rules, cfg.ButtonPalette, null);
        ButtonSheetlet.MakeButtonRules<MenuButton>(cfg, rules, cfg.NegativeButtonPalette, MenuButton.StyleClassRedTopButton);

        return rules.ToArray();
    }
}
