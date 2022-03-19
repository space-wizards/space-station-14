using Content.Client.CombatMode;
using Content.Client.CombatMode.UI;
using Content.Client.Gameplay;
using Content.Client.HUD.Widgets;
using Content.Client.Sandbox;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.HUD.Presets;
[UsedImplicitly]
public sealed class DefaultHud : HudPreset
{
    protected override Thickness Margins => new Thickness(10);

    protected override void DefinePreset()
    {
        RegisterAllowedState<GameplayState>();
        RegisterLinkedEntitySystem<SandboxSystem>();
        RegisterLinkedEntitySystem<CombatModeSystem>();

        RegisterWidget<ButtonBar>().SetAnchorAndMarginPreset(LayoutContainer.LayoutPreset.TopLeft, margin: 10);
        RegisterWidget<CombatPanelWidget>();
    }
}
