using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Storage.Components;

/// <summary>
/// A generic entity storage that does not spawn entities in its storage until they're needed to be entities for other systems.
/// Useful for when an item stores a lot of entities without needing them to be entities in said storage.
/// Entities that are inserted back into the storage will remain entities, and are prioritized over non-spawned entities.
/// Refilling other entity providers will not spawn entities, as the end destination is also an entity provider storage.
/// </summary>
/// <example>Light replacers don't need their lights to be entities while in storage, but when they are used to replace a light.</example>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(EntityProviderSystem))]
public sealed partial class EntityProviderComponent : Component
{
    /// <summary>
    /// The counter for what entities are currently stored.
    /// Each EntProtoId key corresponds to an entity, of which the value corresponds to how many are stored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, int> EntityCounter = [];

    /// <summary>
    /// The whitelist that entities have to pass in order to be inserted.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist;

    /// <summary>
    /// Whether this provider can transfer its storage to other providers.
    /// </summary>
    [DataField]
    public bool CanTransfer = true;

    /// <summary>
    /// Whether this provider can receive entities to its storage.
    /// </summary>
    [DataField]
    public bool CanReceive = true;

    /// <summary>
    /// Whether this provider should be deleted after being emptied.
    /// </summary>
    [DataField]
    public bool DeleteIfEmpty;

    /// <summary>
    /// The container where items will be shortly spawned into when being materialized from the counter.
    /// </summary>
    [ViewVariables]
    public Container Container;
}
