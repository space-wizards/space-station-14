using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Storage.Components;

/// <summary>
/// A generic item storage that does not store entities, but instead evaporates inserted entities into a counter.
/// This deletes entities when inserted and increments the counter, and spawns entities when ejected and decrements the counter.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(EntityProviderSystem))]
public sealed partial class EntityProviderComponent : Component
{
    /// <summary>
    /// The counter for what entities are currently stored.
    /// Each EntProtoId key corresponds to an entity, of which the value corresponds to how many are stored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, int> EntityCounter = [];

    /// <summary>
    /// The whitelist that the entities have to pass in order to be inserted.
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
    /// Whether this provider should delete after being emptied.
    /// </summary>
    [DataField]
    public bool DeleteIfEmpty;

    /// <summary>
    /// The container where items will be shortly spawned into when being materialized from the counter.
    /// </summary>
    public Container Container;
}
