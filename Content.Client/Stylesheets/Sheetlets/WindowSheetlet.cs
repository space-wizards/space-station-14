using Content.Client.Resources;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class WindowSheetlet<T> : Sheetlet<T>
    where T : PalettedStylesheet, IButtonConfig, IWindowConfig, IIconConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig buttonCfg = sheet;
        IWindowConfig windowCfg = sheet;
        IIconConfig iconCfg = sheet;

        var headerStylebox = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(windowCfg.WindowHeaderTexturePath, NanotrasenStylesheet.TextureRoot),
            PatchMarginBottom = 3,
            ExpandMarginBottom = 3,
            ContentMarginBottomOverride = 0,
        };
        // TODO: This would probably be better palette-based but we can leave it for now.
        var headerAlertStylebox = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(windowCfg.WindowHeaderAlertTexturePath, NanotrasenStylesheet.TextureRoot),
            PatchMarginBottom = 3,
            ExpandMarginBottom = 3,
            ContentMarginBottomOverride = 0,
        };
        var backgroundBox = new StyleBoxTexture()
        {
            Texture = sheet.GetTextureOr(windowCfg.WindowBackgroundPath, NanotrasenStylesheet.TextureRoot),
        };
        backgroundBox.SetPatchMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
        backgroundBox.SetExpandMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
        var borderedBackgroundBox = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(windowCfg.WindowBackgroundBorderedPath, NanotrasenStylesheet.TextureRoot),
        };
        borderedBackgroundBox.SetPatchMargin(StyleBox.Margin.All, 2);
        var closeButtonTex = sheet.GetTextureOr(iconCfg.CrossIconPath, NanotrasenStylesheet.TextureRoot);

        var leftPanel = StyleBoxHelpers.OpenLeftStyleBox(sheet);
        leftPanel.SetPadding(StyleBox.Margin.All, 0.0f);

        // TODO: maybe also change everything here to `NanoWindow` or something
        return
        [
            // TODO: KILL DEFAULT WINDOW (in a bit)
            E<Label>()
                .Class(DefaultWindow.StyleClassWindowTitle)
                .FontColor(sheet.HighlightPalette.Text)
                .Font(sheet.BaseFont.GetFont(14, FontKind.Bold)),
            E<Label>()
                .Class("windowTitleAlert")
                .FontColor(Color.White)
                .Font(sheet.BaseFont.GetFont(14, FontKind.Bold)),
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

            // Close button
            E<TextureButton>()
                .Class(DefaultWindow.StyleClassWindowCloseButton)
                .Prop(TextureButton.StylePropertyTexture, closeButtonTex)
                .Margin(3),
            E<TextureButton>()
                .Class(DefaultWindow.StyleClassWindowCloseButton)
                .PseudoNormal()
                .Modulate(Palettes.Neutral.Element),
            E<TextureButton>()
                .Class(DefaultWindow.StyleClassWindowCloseButton)
                .PseudoHovered()
                .Modulate(Palettes.Red.HoveredElement),
            E<TextureButton>()
                .Class(DefaultWindow.StyleClassWindowCloseButton)
                .PseudoPressed()
                .Modulate(Palettes.Red.PressedElement),
            E<TextureButton>()
                .Class(DefaultWindow.StyleClassWindowCloseButton)
                .PseudoDisabled()
                .Modulate(Palettes.Red.DisabledElement),

            // Title
            E<Label>()
                .Class("FancyWindowTitle") // TODO: hardcoding class name
                .Font(ResCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 13)) // TODO: hardcoding font
                .FontColor(sheet.HighlightPalette.Text),

            // Help Button
            E<TextureButton>()
                .Class(FancyWindow.StyleClassWindowHelpButton)
                .Prop(TextureButton.StylePropertyTexture,
                    sheet.GetTextureOr(iconCfg.HelpIconPath, NanotrasenStylesheet.TextureRoot))
                .Prop(Control.StylePropertyModulateSelf, sheet.PrimaryPalette.Element),
            E<TextureButton>()
                .Class(FancyWindow.StyleClassWindowHelpButton)
                .Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(Control.StylePropertyModulateSelf, sheet.PrimaryPalette.HoveredElement),
            E<TextureButton>()
                .Class(FancyWindow.StyleClassWindowHelpButton)
                .Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(Control.StylePropertyModulateSelf, sheet.PrimaryPalette.PressedElement),

            // Footer
            E<Label>()
                .Class("WindowFooterText") // TODO: hardcoding font
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(8))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#757575")),
        ];
    }
}
