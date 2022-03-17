using Content.Client.CombatMode.UI;
using Content.Client.HUD.Widgets;

namespace Content.Client.HUD.Presets;

public sealed class DefaultHud : HudPreset
{
    protected override void DefineWidgets()
    {
        RegisterWidget(new ButtonBar());
        RegisterWidget(new CombatPanel());
    }
}
