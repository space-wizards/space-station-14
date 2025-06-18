using Content.Shared.DoAfter;
using Content.Shared.Item;
using Robust.Shared.Serialization;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared.Storage;

/// <summary>
/// This event is raised on an entity with <see cref="AreaPickupComponent"/> when it's finished attempting to pick up
/// entities. A storage-like system which wants to implement area pickup into its storage MUST also handle
/// <see cref="BeforeAreaPickupEvent"/> and SHOULD use <see cref="AreaPickupSystem.TryDoAreaPickup"/> to handle this
/// event.
/// </summary>
/// <seealso cref="BeforeAreaPickupEvent"/>
/// <seealso cref="AreaPickupSystem"/>
/// <seealso cref="AreaPickupSystem.TryDoAreaPickup"/>
[Serializable, NetSerializable]
public sealed partial class AreaPickupDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// The entities that will be picked up when this event is handled.
    /// </summary>
    [DataField(required: true)]
    public IReadOnlyList<NetEntity> Entities = default!;

    public AreaPickupDoAfterEvent(List<NetEntity> entities)
    {
        Entities = entities;
    }

    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// This event is raised on an entity with <see cref="AreaPickupComponent"/> to allow storage-like systems to inform
/// the area pickup logic which entities can actually be picked up by the entity. A storage-like system which wants to
/// implement area pickup into its storage MUST also handle <see cref="AreaPickupDoAfterEvent"/> and SHOULD use
/// <see cref="AreaPickupSystem.DoBeforeAreaPickup"/> to handle this event.
/// </summary>
/// <seealso cref="AreaPickupDoAfterEvent"/>
/// <seealso cref="AreaPickupSystem"/>
/// <seealso cref="AreaPickupSystem.DoBeforeAreaPickup"/>
public sealed partial class BeforeAreaPickupEvent(
    IReadOnlyList<Entity<ItemComponent>> pickupCandidates,
    int maxPickups
) : HandledEntityEventArgs
{
    public readonly IReadOnlyList<Entity<ItemComponent>> PickupCandidates = pickupCandidates;
    public readonly List<Entity<ItemComponent>> EntitiesToPickUp = [];
    public readonly int MaxPickups = maxPickups;
}

/// <summary>
/// This event is raised on an entity with <see cref="QuickPickupComponent"/> when it attempts to pick up an item.
/// Storage-like systems which want to quick pickup into their storage SHOULD handle the event using
/// <see cref="QuickPickupSystem.TryDoQuickPickup"/>.
/// </summary>
/// <seealso cref="QuickPickupSystem"/>
/// <seealso cref="QuickPickupSystem.TryDoQuickPickup"/>
public sealed partial class QuickPickupEvent(
    Entity<QuickPickupComponent> entity,
    Entity<ItemComponent> pickedUp,
    EntityUid user
) : HandledEntityEventArgs
{
    public readonly Entity<QuickPickupComponent> QuickPickupEntity = entity;
    public readonly Entity<ItemComponent> PickedUp = pickedUp;
    public readonly EntityUid User = user;
}
