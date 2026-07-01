using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PowerProviderComponent : Component
{
    [DataField]
    public HashSet<EntityUid> LinkedReceivers = new();
}
