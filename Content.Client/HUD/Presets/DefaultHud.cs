using Content.Client.CombatMode;
using Content.Client.CombatMode.UI;
using Content.Client.Gameplay;
using Content.Client.HUD.Widgets;
using Content.Client.Sandbox;
using JetBrains.Annotations;

namespace Content.Client.HUD.Presets;
[UsedImplicitly]
public sealed class DefaultHud : HudPreset
{
    protected override void DefinePreset()
    {
        RegisterAllowedState<GameplayState>();
        RegisterLinkedEntitySystem<SandboxSystem>();
        RegisterLinkedEntitySystem<CombatModeSystem>();
        RegisterWidget<CombatPanelWidget>();
        RegisterWidget<ButtonBar>();
    }
}
