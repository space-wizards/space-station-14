using Content.Client.Resources;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Client.UserInterface.Systems.Actions.Windows;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ActionSheetlet<T> : Sheetlet<T> where T: PalettedStylesheet, IPanelConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IPanelConfig panelCfg = sheet;

        // TODO: absolute texture access
        var handSlotHighlightTex = ResCache.GetTexture("/Textures/Interface/Inventory/hand_slot_highlight.png");
        var handSlotHighlight = new StyleBoxTexture
        {
            Texture = handSlotHighlightTex,
        };
        handSlotHighlight.SetPatchMargin(StyleBox.Margin.All, 2);

        var actionSearchBoxTex =
            sheet.GetTextureOr(panelCfg.BlackPanelDarkThinBorderPath, NanotrasenStylesheet.TextureRoot);
        var actionSearchBox = new StyleBoxTexture
        {
            Texture = actionSearchBoxTex,
        };
        actionSearchBox.SetPatchMargin(StyleBox.Margin.All, 3);
        actionSearchBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

        return
        [
            E<PanelContainer>().Class(ActionButton.StyleClassActionHighlightRect).Panel(handSlotHighlight),
            E<LineEdit>().Class(ActionsWindow.StyleClassActionSearchBox).Box(actionSearchBox),
        ];
    }
}
