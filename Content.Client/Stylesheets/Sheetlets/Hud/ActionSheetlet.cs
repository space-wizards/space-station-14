using Content.Client.Resources;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Client.UserInterface.Systems.Actions.Windows;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets.Hud;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class ActionSheetlet<T> : ISheetlet<T>
    where T : IPanelConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        IPanelConfig panelCfg = config;

        // TODO: absolute texture access
        var handSlotHighlightTex = factory.GetTexture(new ResPath("/Textures/Interface/Inventory/hand_slot_highlight.png"));
        var handSlotHighlight = new StyleBoxTexture
        {
            Texture = handSlotHighlightTex,
        };
        handSlotHighlight.SetPatchMargin(StyleBox.Margin.All, 2);

        var actionSearchBoxTex =
            factory.GetTexture(panelCfg.BlackPanelDarkThinBorderPath);
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
