using Content.Shared.Botany.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for plant entities that have produce when they are harvested.
/// Biological produce should have <c>PlantChemicalsComponent</c> as well (steelcap on the other hand should not).
/// </summary>
[RegisterComponent, Access(typeof(PlantProduceSystem))]
public sealed partial class PlantProduceComponent : Component
{
    /// <summary>
    /// The produce entity to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Produce;

    /// <summary>
    /// How many of the produce entities to spawn.
    /// </summary>
    [DataField(required: true)]
    public int Yield;
}

/// <summary>
/// Event raised on the plant when it creates a produce entity.
/// Used to apply chemicals mutation effects etc.
/// </summary>
[ByRefEvent]
public record struct ProduceCreatedEvent(EntityUid Produce);
