using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
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

        return new StyleRule[]
        {
            E<Placeholder>()
                .Prop(Placeholder.StylePropertyPanel, placeholderBox)
        };
    }
}
