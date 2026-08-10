using Content.Server.Animals.Systems;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.Animals.Components;

/// <summary>
/// Spawns configured entities when production is requested.
/// </summary>
[RegisterComponent, Access(typeof(EntityProducerSystem))]
public sealed partial class EntityProducerComponent : Component
{
    [DataField(required: true)]
    public EntityTableSelector Table = default!;
}

/// <summary>
/// Raised on the producer after entities have been spawned.
/// </summary>
[ByRefEvent]
public readonly record struct EntitiesProducedEvent(EntityUid Owner, IReadOnlyList<EntityUid> Entities);
