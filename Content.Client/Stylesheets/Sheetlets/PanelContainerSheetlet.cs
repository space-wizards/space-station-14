using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class PanelContainerSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig buttonConfig = sheet;

        var insetBack = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(buttonConfig.BaseButtonPath, NanotrasenStylesheet.TextureRoot),
            Modulate = sheet.SecondaryPalette.BackgroundLight,
        };
        insetBack.SetPatchMargin(StyleBox.Margin.All, 10);

        return
        [
            E<PanelContainer>()
                .Class(StyleClass.Inset)
                .Prop(PanelContainer.StylePropertyPanel, insetBack),
        ];
    }
}
