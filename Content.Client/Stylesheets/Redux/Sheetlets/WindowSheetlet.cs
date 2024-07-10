using Content.Client.Stylesheets.Redux.Fonts;
using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class WindowSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var buttonCfg = (IButtonConfig) sheet;
        var windowCfg = (IWindowConfig) sheet;
        var iconCfg = (IIconConfig) sheet;

        var boxFont = new SingleFont(ResCache, "/Fonts/Boxfont-round/Boxfont Round.ttf");

        var headerStylebox = new StyleBoxTexture
        {
            Texture = sheet.GetTexture(windowCfg.WindowHeaderTexturePath),
            PatchMarginBottom = 3,
            ExpandMarginBottom = 3,
            ContentMarginBottomOverride = 0,
        };
        // TODO: This would probably be better palette-based but we can leave it for now.
        var headerAlertStylebox = new StyleBoxTexture
        {
            Texture = sheet.GetTexture(windowCfg.WindowHeaderAlertTexturePath),
            PatchMarginBottom = 3,
            ExpandMarginBottom = 3,
            ContentMarginBottomOverride = 0,
        };
        var backgroundBox = new StyleBoxTexture()
        {
            Texture = sheet.GetTexture(windowCfg.WindowBackgroundPath),
        };
        backgroundBox.SetPatchMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
        backgroundBox.SetExpandMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
        var borderedBackgroundBox = new StyleBoxTexture
        {
            Texture = sheet.GetTexture(windowCfg.WindowBackgroundBorderedPath),
        };
        borderedBackgroundBox.SetPatchMargin(StyleBox.Margin.All, 2);
        var closeButtonTex = sheet.GetTexture(iconCfg.CrossIconPath);

        var leftPanel = buttonCfg.ConfigureOpenLeftButton(sheet);
        leftPanel.SetPadding(StyleBox.Margin.All, 0.0f);

        // TODO: maybe also change everything here to `NanoWindow` or something
        var rules = new List<StyleRule>()
        {
            // TODO: KILL DEFAULT WINDOW (in a bit)
            E<Label>()
                .Class(DefaultWindow.StyleClassWindowTitle)
                .FontColor(sheet.HighlightPalette.Text)
                .Font(sheet.BaseFont.GetFont(14, FontStack.FontKind.Bold)),
            E<Label>()
                .Class("windowTitleAlert")
                .FontColor(Color.White)
                .Font(sheet.BaseFont.GetFont(14, FontStack.FontKind.Bold)),
            // TODO: maybe also change everything here to `NanoWindow` or something
            E()
                .Class(DefaultWindow.StyleClassWindowPanel)
                .Panel(backgroundBox),
            E()
                .Class(DefaultWindow.StyleClassWindowHeader)
                .Panel(headerStylebox),
            E()
                .Class(StyleClass.AlertWindowHeader)
                .Panel(headerAlertStylebox),
            E()
                .Class(StyleClass.BorderedWindowPanel)
                .Panel(borderedBackgroundBox),
            E<TextureButton>()
                .Class(DefaultWindow.StyleClassWindowCloseButton)
                .Prop(TextureButton.StylePropertyTexture, closeButtonTex)
                .Margin(3),

            // Title
            E<Label>()
                .Class("FancyWindowTitle") // TODO: HARDCODING AAAAAA (theres a lot more in this file)
                .Font(boxFont.GetFont(13, FontStack.FontKind.Bold))
                .FontColor(sheet.HighlightPalette.Text),

            // Help Button
            E<TextureButton>()
                .Class(FancyWindow.StyleClassWindowHelpButton)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(iconCfg.HelpIconPath))
                .Prop(Control.StylePropertyModulateSelf, sheet.PrimaryPalette.Element),
            E<TextureButton>()
                .Class(FancyWindow.StyleClassWindowHelpButton)
                .Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(Control.StylePropertyModulateSelf, sheet.PrimaryPalette.HoveredElement),
            E<TextureButton>()
                .Class(FancyWindow.StyleClassWindowHelpButton)
                .Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(Control.StylePropertyModulateSelf, sheet.PrimaryPalette.PressedElement),

            // Close Button
            E<TextureButton>()
                .Class(FancyWindow.StyleClassWindowCloseButton)
                .Margin(new Thickness(0, 0, -3, 0)),

            // Footer
            E<Label>()
                .Class("WindowFooterText")
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(8))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#757575")),
        };

        ButtonSheetlet.MakeButtonRules(buttonCfg,
            rules,
            buttonCfg.NegativeButtonPalette,
            DefaultWindow.StyleClassWindowCloseButton);

        return rules.ToArray();
    }
}
