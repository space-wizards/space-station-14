using Content.Client.CombatMode;
using Content.Client.Gameplay;
using Content.Client.Hands;
using Content.Client.HUD.Widgets;
using Content.Client.Sandbox;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using MenuBar = Content.Client.HUD.Widgets.MenuBar;

namespace Content.Client.HUD.Presets;
[UsedImplicitly]
public sealed class DefaultHud : HudPreset
{
    protected override Thickness Margins => new Thickness(10);

    protected override void DefinePreset()
    {
        RegisterAllowedState<GameplayState>(); //only allow this hud to initialize during gameplay state (Ie: Ingame not lobby)
        RegisterLinkedEntitySystem<SandboxSystem>(); //TODO: This is depreciated, use a UIController now
        RegisterLinkedEntitySystem<CombatModeSystem>(); //TODO: this is depreciated, use a UIController now

        RegisterWidget<MenuBar>().SetAnchorAndMarginPreset(LayoutContainer.LayoutPreset.TopLeft,margin: 10);
        RegisterWidget<HandsGui>().SetAnchorAndMarginPreset(LayoutContainer.LayoutPreset.CenterBottom, margin: 10);
        //RegisterWidget<CombatPanelWidget>().SetAnchorAndMarginPreset(LayoutContainer.LayoutPreset.BottomRight);
    }
}
