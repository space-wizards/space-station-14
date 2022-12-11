using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Interaction.Components;

/// <summary>
/// Relays an entities interactions to another entity.
/// This doesn't raise the same events, but just relays
/// the clicks of the mouse.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedInteractionSystem))]
public sealed class InteractionRelayComponent : Component
{
    /// <summary>
    /// The entity the interactions are being relayed to.
    /// </summary>
    [ViewVariables]
    public EntityUid? RelayEntity;
}

/// <summary>
/// Contains network state for <see cref="InteractionRelayComponent"/>
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
