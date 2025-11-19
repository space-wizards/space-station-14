using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class PlaceholderSheetlet<T> : Sheetlet<T> where T: PalettedStylesheet, IPlaceholderConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IPlaceholderConfig placeholderCfg = sheet;

        var placeholderBox = sheet.GetTextureOr(placeholderCfg.PlaceholderPath, NanotrasenStylesheet.TextureRoot)
            .IntoPatch(StyleBox.Margin.All, 19);
        placeholderBox.SetExpandMargin(StyleBox.Margin.All, -5);
        placeholderBox.Mode = StyleBoxTexture.StretchMode.Tile;

        return
        [
            E<Placeholder>()
                // ReSharper disable once AccessToStaticMemberViaDerivedType
                .Prop(Placeholder.StylePropertyPanel, placeholderBox),
            E<Label>()
                .Class(Placeholder.StyleClassPlaceholderText)
                .Font(sheet.BaseFont.GetFont(16))
                .FontColor(new Color(103, 103, 103, 128)), // TODO: fix hardcoded color
        ];
    }
}
