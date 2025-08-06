using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// Entities with this component occasionally spill some of the solution they're ingesting.
/// </summary>
[RegisterComponent]
public sealed partial class MessyDrinkerComponent : Component
{
    [DataField]
    public float SpillChance = 0.2f;

    /// <summary>
    /// The amount of solution that is spilled when <see cref="SpillChance"/> procs.
    /// </summary>
    [DataField]
    public FixedPoint2 SpillAmount = 1.0;

    /// <summary>
    /// The types of food prototypes we can spill
    /// </summary>
    [DataField]
    public List<ProtoId<EdiblePrototype>> SpillableTypes = new List<ProtoId<EdiblePrototype>> { "Drink" };

    [DataField]
    public LocId? SpillMessagePopup;
}
