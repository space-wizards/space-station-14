using Content.Server.Animals.Systems;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.Animals.Components;

/// <summary>
/// Spawns configured entities when production is requested.
/// </summary>
[RegisterComponent, Access(typeof(EntityProducerSystem))]
public sealed partial class EntityProducerComponent : Component
{
    /// <summary>
    /// Selects the entities spawned for each production attempt.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;
}

/// <summary>
/// Raised after entity production succeeds with the entities that were spawned.
/// </summary>
/// <param name="Owner">Entity on whose behalf the entities were produced.</param>
/// <param name="Entities">Entities spawned by the successful production attempt.</param>
[ByRefEvent]
public readonly record struct EntitiesProducedEvent(EntityUid Owner, IReadOnlyList<EntityUid> Entities);
