using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PowerProviderComponent : Component
{
    /// <summary>
    /// Node ID to which the power will be provided to.
    /// </summary>
    [DataField]
    public string NodeId = "output";

    [DataField]
    public HashSet<EntityUid> LinkedReceivers = new();
}
