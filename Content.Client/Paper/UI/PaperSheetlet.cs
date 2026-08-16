using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetDefinitions;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Paper.UI;

[Sheetlet(typeof(NanotrasenStylesheetDefinition))]
public sealed class PaperSheetlet<T> : ISheetlet<T>
    where T : IWindowConfig
{
    public StyleRule[] GetRules(StylesheetDefinition sheet, T config)
    {
        var paperBackground = sheet
            .GetTexture(new ResPath("Paper/paper_background_default.svg.96dpi.png"))
            .IntoPatch(StyleBox.Margin.All, 16);
        var paperBox = new StyleBoxTexture
            { Texture = sheet.GetTexture(config.TransparentWindowBackgroundBorderedPath) };
        paperBox.SetPatchMargin(StyleBox.Margin.All, 2);

        var borderedTransparentTex =
            sheet.GetTexture(new ResPath("transparent_window_background_bordered.png"));
        var borderedTransparentBackground = new StyleBoxTexture
        {
            Texture = borderedTransparentTex,
        };
        borderedTransparentBackground.SetPatchMargin(StyleBox.Margin.All, 2);

        return
        [
            E<PanelContainer>().Identifier("PaperContainer").Panel(paperBox),
            E<PanelContainer>()
                .Identifier("PaperDefaultBorder")
                .Prop(PanelContainer.StylePropertyPanel, paperBackground),
            E<PanelContainer>()
                .Identifier("PaperEditBackground")
                .Prop(PanelContainer.StylePropertyPanel, borderedTransparentBackground),
        ];
    }
}
