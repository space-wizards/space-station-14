using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Basically, monkey cubes.
/// But specifically, this component deletes the entity and spawns in a new entity when the entity is exposed to a certain amount of a given reagent.
/// </summary>
[RegisterComponent, Access(typeof(RehydratableSystem))]
public sealed partial class RehydratableComponent : Component
{
    /// <summary>
    /// The reagent(s) which must be present to count as hydrated.
    /// </summary>
    [DataField("catalysts")]
    public HashSet<ProtoId<ReagentPrototype>> CatalystPrototypes = new(){ "Water", "Holywater" };

    /// <summary>
    /// The minimum amount of catalyst that must be present to be hydrated. If there are multiple valid catalysts, the total amount of all catalysts in the solution will be compared to CatalystMinimum.
    /// </summary>
    [DataField]
    public FixedPoint2 CatalystMinimum = FixedPoint2.Zero;

    /// <summary>
    /// The entity to create when hydrated.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> PossibleSpawns = new();
}

/// <summary>
/// Raised on the rehydrated entity with target being the new entity it became.
/// </summary>
[ByRefEvent]
public readonly record struct GotRehydratedEvent(EntityUid Target);
