using Content.Shared.Silicons.Bots;
using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Bots.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SecuritronComponent : Component
{
    [DataField("baseLayer"), AutoNetworkedField]
    public string BaseLayer = "enum.DamageStateVisualLayers.Base";

    [DataField("onlineState"), AutoNetworkedField]
    public string OnlineState = "secbot-on";

    [DataField("combatState"), AutoNetworkedField]
    public string CombatState = "secbot-combat";

    [ViewVariables(VVAccess.ReadOnly)]
    public SecuritronVisualState CurrentState = SecuritronVisualState.Combat;
}
