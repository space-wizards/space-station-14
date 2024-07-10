using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class PlaceholderSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var placeholderBox = sheet.GetTexture("placeholder.png").IntoPatch(StyleBox.Margin.All, 19);
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
