using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Interaction.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedInteractionSystem))]
public sealed class InteractionRelayComponent : Component
{
    [ViewVariables]
    public EntityUid? RelayEntity;
}

/// <summary>
/// Contains network state for InteractionRelayComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed class InteractionRelayComponentState : ComponentState
{
    public EntityUid? RelayEntity;

    public InteractionRelayComponentState(EntityUid? relayEntity)
    {
        RelayEntity = relayEntity;
    }
}
