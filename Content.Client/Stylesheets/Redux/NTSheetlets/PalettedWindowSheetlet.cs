using Content.Client.Stylesheets.Redux.Fonts;
using Content.Client.Stylesheets.Redux.SheetletConfig;
using Content.Client.Stylesheets.Redux.Sheetlets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.NTSheetlets;

public sealed class PalettedWindowSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var buttonCfg = (IButtonConfig) sheet;

        var headerStylebox = new StyleBoxTexture
        {
            Texture = sheet.GetTexture("window_header.png"),
            PatchMarginBottom = 3,
            ExpandMarginBottom = 3,
            ContentMarginBottomOverride = 0
        };
        // TODO: This would probably be better palette-based but we can leave it for now.
        var headerAlertStylebox = new StyleBoxTexture
        {
            Texture = sheet.GetTexture("window_header_alert.png"),
            PatchMarginBottom = 3,
            ExpandMarginBottom = 3,
            ContentMarginBottomOverride = 0
        };
        var backgroundStylebox = new StyleBoxTexture()
        {
            Texture = sheet.GetTexture("window_background.png")
        };
        backgroundStylebox.SetPatchMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
        backgroundStylebox.SetExpandMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
        var borderedBackgroundStylebox = new StyleBoxTexture
        {
            Texture = sheet.GetTexture("window_background_bordered.png"),
        };
        borderedBackgroundStylebox.SetPatchMargin(StyleBox.Margin.All, 2);
        var closeButtonTex = sheet.GetTexture("cross.svg.png");

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
                .Panel(backgroundStylebox),
            E()
                .Class(DefaultWindow.StyleClassWindowHeader)
                .Panel(headerStylebox),
            E()
                .Class(StyleClass.AlertWindowHeader)
                .Panel(headerAlertStylebox),
            E()
                .Class(StyleClass.BorderedWindowPanel)
                .Panel(borderedBackgroundStylebox),
            E<TextureButton>()
                .Class(DefaultWindow.StyleClassWindowCloseButton)
                .Prop(TextureButton.StylePropertyTexture, closeButtonTex)
                .Margin(3),
        };

        ButtonSheetlet.MakeButtonRules(buttonCfg,
            rules,
            buttonCfg.NegativeButtonPalette,
            DefaultWindow.StyleClassWindowCloseButton);

        return rules.ToArray();
    }
}
