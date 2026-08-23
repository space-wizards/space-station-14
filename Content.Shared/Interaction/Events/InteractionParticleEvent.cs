using Robust.Shared.Serialization;

namespace Content.Shared.Interaction.Events;

/// <summary>
/// Data for interaction particles
/// </summary>
[Serializable, NetSerializable]
public sealed class InteractionParticleEvent(NetEntity performer, NetEntity? used, NetEntity target, bool isClientEvent, InteractionParticleType type) : EntityEventArgs
{
    /// <summary>
    /// The performer of the interaction
    /// </summary>
    public NetEntity Performer = performer;

    /// <summary>
    /// The item used to interact
    /// </summary>
    public NetEntity? Used = used;

    /// <summary>
    /// The target of the interaction
    /// </summary>
    public NetEntity Target = target;

    /// <summary>
    /// Workaround for event subscription not working w/ the session overload
    /// </summary>
    public bool IsClientEvent = isClientEvent;

    /// <summary>
    /// The type of interaction
    /// </summary>
    public InteractionParticleType Type = type;
}

[Serializable, NetSerializable]
public enum InteractionParticleType : byte
{
    Use,
    Pull,
    InHand,
}
