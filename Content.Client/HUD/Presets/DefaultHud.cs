using Content.Client.CombatMode.UI;
using Content.Client.HUD.Widgets;
using Content.Client.Sandbox;
using JetBrains.Annotations;

namespace Content.Client.HUD.Presets;

[UsedImplicitly]
public sealed class DefaultHud : HudPreset
{
    protected override void DefineWidgetsAndLinkedSystems()
    {
        RegisterLinkedEntitySystem<SandboxSystem>();
        RegisterWidget<CombatPanelWidget>();
        RegisterWidget<ButtonBar>();
    }
}
