using Content.Client.Gameplay;
using Content.Client.HUD.Widgets;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using MenuBar = Content.Client.HUD.Widgets.MenuBar;

namespace Content.Client.HUD.Presets;
[UsedImplicitly]
public sealed class DefaultHud : HudPreset
{
    protected override Thickness Margins => new Thickness(10);


    //TODO: refactor to support HudPresetZones (Main gamescreen) and sidebar or more!
    protected override void DefinePreset()
    {
        RegisterAllowedState<GameplayState>(); //only allow this hud to initialize during gameplay state (Ie: Ingame not lobby)
        RegisterWidget<MenuBar>().SetAnchorAndMarginPreset(LayoutContainer.LayoutPreset.TopLeft, margin: 10);
        RegisterWidget<HandsGui>().SetAnchorAndMarginPreset(LayoutContainer.LayoutPreset.CenterBottom, margin: 10);
        RegisterWidget<ActionsBar>().SetAnchorAndMarginPreset(LayoutContainer.LayoutPreset.BottomLeft, margin: 10);
    }
}
