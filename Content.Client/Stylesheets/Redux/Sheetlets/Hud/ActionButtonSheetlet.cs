using Content.Client.Resources;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ActionButtonSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        // TODO: absolute texture access
        var handSlotHighlightTex = ResCache.GetTexture("/Textures/Interface/Inventory/hand_slot_highlight.png");
        var handSlotHighlight = new StyleBoxTexture
        {
            Texture = handSlotHighlightTex,
        };
        handSlotHighlight.SetPatchMargin(StyleBox.Margin.All, 2);

        return
        [
            E<PanelContainer>().Class(ActionButton.StyleClassActionHighlightRect).Panel(handSlotHighlight),
        ];
    }
}
