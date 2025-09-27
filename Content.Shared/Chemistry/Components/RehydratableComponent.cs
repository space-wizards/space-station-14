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
    /// The reagent that must be present to count as hydrated.
    /// </summary>
    [DataField("catalyst")]
    public ProtoId<ReagentPrototype> CatalystPrototype = "Water";

    /// <summary>
    /// The minimum amount of catalyst that must be present to be hydrated.
    /// </summary>
    [DataField]
    public FixedPoint2 CatalystMinimum = FixedPoint2.Zero;

    /// <summary>
    /// If true, transfers into this entity will enforce a whitelist consisting of at least the catalyst reagent.
    /// If false, the catalyst will still be contributed to the transfer whitelist, but enforcement will only occur
    /// if another system also sets enforcement.
    /// </summary>
    [DataField]
    public bool EnforceCatalystWhitelist = true;

    /// <summary>
    /// The entity to create when hydrated.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> PossibleSpawns = [];
}

/// <summary>
/// Raised on the rehydrated entity with target being the new entity it became.
/// </summary>
[ByRefEvent]
public readonly record struct GotRehydratedEvent(EntityUid Target);
