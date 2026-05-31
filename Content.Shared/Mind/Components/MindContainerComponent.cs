using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;

namespace Content.Shared.Mind.Components;

/// <summary>
/// This component indicates that this entity may have mind, which is simply an entity with a <see cref="MindComponent"/>.
/// The mind entity is not actually stored in a "container", but is simply stored in nullspace.
/// </summary>
[RegisterComponent, Access(typeof(SharedMindSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindContainerComponent : Component
{
    /// <summary>
    ///     The mind controlling this mob. Can be null.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Mind;

    /// <summary>
    ///     True if we have a mind, false otherwise.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool HasMind;

    /// <summary>
    ///     Whether the mind will be put on a ghost after this component is shutdown.
    /// </summary>
    [DataField]
    public bool GhostOnShutdown = true;
}

/// <summary>
/// Base event for all other mind related events.
/// </summary>
public abstract class MindEvent : EntityEventArgs
{
    /// <summary>
    /// <see cref="MindComponent"/> entity currently being handled by the event.
    /// </summary>
    public readonly Entity<MindComponent> Mind;

    /// <summary>
    /// <see cref="MindContainerComponent"/> entity currently being handled by the event.
    /// </summary>
    public readonly Entity<MindContainerComponent> Container;

    /// <summary>
    /// The target entity in case the mind is being transferred. In <see cref="MindRemovedMessage" /> it means the entity that is being transferred to, and in <see cref="MindAddedMessage" /> it means the previous entity.
    /// Null if the mind is being added for the first time or fully removed from entities.
    /// </summary>
    public readonly EntityUid? TransferEntity;

    public MindEvent(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
    {
        Mind = mind;
        Container = container;
        TransferEntity = transferEntity;
    }
}

/// <summary>
/// Event raised directed at a mind-container when a mind gets removed.
/// </summary>
/// <remarks>
/// Called after the owned entity is already set to null. TransferEntity is the entity this mind will be added to afterward, if any.
/// </remarks>
public sealed class MindRemovedMessage : MindEvent
{
    public MindRemovedMessage(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}

/// <summary>
/// Event raised directed at a mind when it gets removed from a mind-container.
/// </summary>
/// <remarks>
/// Called after the owned entity is already set to null. TransferEntity is the entity this mind will be added to afterward, if any.
/// </remarks>
public sealed class MindGotRemovedEvent : MindEvent
{
    public MindGotRemovedEvent(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}

/// <summary>
/// Event raised directed at a mind-container before a mind gets removed.
/// </summary>
/// <remarks>
/// Called before the OwnedEntity is set to null. TransferEntity is the entity this mind will be added to afterward, if any.
/// </remarks>
public sealed class BeforeMindRemovedMessage : MindEvent
{
    public BeforeMindRemovedMessage(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}

/// <summary>
/// Event raised directed at a mind before it gets removed from a mind-container.
/// </summary>
/// <remarks>
/// Called before the OwnedEntity is set to null. TransferEntity is the entity this mind will be added to afterward, if any.
/// </remarks>
public sealed class BeforeMindGotRemovedEvent : MindEvent
{
    public BeforeMindGotRemovedEvent(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}

/// <summary>
/// Event raised directed at a mind-container when a mind gets added.
/// </summary>
/// <remarks>
/// Called after the owned entity is already set to the new entity. TransferEntity is the previous entity that this mind owned, if any.
/// </remarks>
public sealed class MindAddedMessage : MindEvent
{
    public MindAddedMessage(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}

/// <summary>
/// Event raised directed at a mind when it gets added to a mind-container.
/// </summary>
/// <remarks>
/// Called after the owned entity is already set to the new entity. TransferEntity is the previous entity that this mind owned, if any.
/// </remarks>
public sealed class MindGotAddedEvent : MindEvent
{
    public MindGotAddedEvent(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}
