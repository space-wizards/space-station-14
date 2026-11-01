using Content.Shared.Item;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Storage.Components;

/// <summary>
/// Attach this to entities which are supposed to be stashable within <see cref="SecretStashComponent"/> entities.
/// If this is attached to an entity, this can be used to override the size
/// they present to the <see cref="Content.Shared.Storage.EntitySystems.SecretStashSystem">SecretStashSystem</see>
/// when attempting to stash this entity
/// </summary>
[RegisterComponent]
[Access(typeof(StashableSystem))]
public sealed partial class StashableComponent : Component
{
    /// <summary>
    /// Item size override to present to the SecretStashSystem.
    /// If null, uses one size smaller than the item's <see cref="ItemComponent"/> size (returns same size if there is no smaller size).
    /// If this entity does not have an ItemComponent attached to it, it will instead use the largest possible item size.
    /// </summary>
    [DataField]
    public ProtoId<ItemSizePrototype>? StashItemSizeOverride;
}