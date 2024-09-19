

using Robust.Shared.GameStates;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RevenantRegenModifierComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public HashSet<NetEntity> Witnesses = new();
}