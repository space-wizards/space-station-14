using Content.Client.CombatMode.UI;
using Content.Client.HUD.Widgets;
using JetBrains.Annotations;

namespace Content.Client.HUD.Presets;

[UsedImplicitly]
public sealed class DefaultHud : HudPreset
{
    protected override void DefineWidgets()
    {
        RegisterWidget<CombatPanel>();
        RegisterWidget<ButtonBar>();
    }
}
