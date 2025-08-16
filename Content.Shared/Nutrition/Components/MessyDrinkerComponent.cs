using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Entities with this component occasionally spill some of the solution they're ingesting.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MessyDrinkerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpillChance = 0.2f;

    /// <summary>
    /// The amount of solution that is spilled when <see cref="SpillChance"/> procs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 SpillAmount = 1.0;

    /// <summary>
    /// The types of food prototypes we can spill
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<EdiblePrototype>> SpillableTypes = new List<ProtoId<EdiblePrototype>> { "Drink" };

    [DataField, AutoNetworkedField]
    public LocId? SpillMessagePopup;
}
