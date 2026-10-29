using System.Numerics;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.VendingMachines.UI;

[CommonSheetlet]
public sealed class VendingMachineMenuSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<TextureRect>()
                .Class("VendingAllCategoryIcon")
                .Prop(TextureRect.StylePropertyTexture, ResCache.GetTexture("/Textures/Interface/grid.svg.96dpi.png")),
            E<TabBar>()
                .Class("VendingCategoryTabBar")
                .ParentOf(E<TabBarButton>())
                .SetSize(new Vector2(60))
        ];
    }
}
