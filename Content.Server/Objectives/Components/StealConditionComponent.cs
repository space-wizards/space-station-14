using Content.Server.Objectives.Systems;
using Content.Shared.Objectives;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that you steal a certain item (or several)
/// </summary>
[RegisterComponent, Access(typeof(StealConditionSystem))]
public sealed partial class StealConditionComponent : Component
{
    /// <summary>
    /// A group of items to be stolen
    /// </summary>
    [DataField(required: true)]
    public ProtoId<StealTargetGroupPrototype> StealGroup;

    /// <summary>
    /// When enabled, disables generation of this target if there is no entity on the map (disable for objects that can be created mid-round).
    /// </summary>
    [DataField]
    public bool VerifyMapExistence = true;

    /// <summary>
    /// If true, counts objects that are close to steal areas.
    /// </summary>
    [DataField]
    public bool CheckStealAreas = false;

    /// <summary>
    /// If the target may be alive but has died, it will not be counted
    /// </summary>
    [DataField]
    public bool CheckAlive = false;

    /// <summary>
    /// The minimum number of items you need to steal to fulfill a objective
    /// </summary>
    [DataField]
    public int MinCollectionSize = 1;

    /// <summary>
    /// The maximum number of items you need to steal to fulfill a objective
    /// </summary>
    [DataField]
    public int MaxCollectionSize = 1;

    /// <summary>
    /// Target collection size after calculation
    /// </summary>
    [DataField]
    public int CollectionSize;

    /// <summary>
    /// OPTIONAL: When set, StealCondition switches to a "carry N of a stacked item" mode.
    /// The value must match the StackTypeId used by the stackable item (e.g., "Credit")
    /// If null, the condition behaves exactly as before (non-currency/object-steal path).
    /// </summary>
    [DataField]
    public string? CurrencyStackId;

    /// <summary>
    /// OPTIONAL: Target amount of the stacked item the mob must carry to satisfy the condition.
    /// If null, the condition behaves exactly as before.
    /// </summary>
    [DataField]
    public int? CurrencyTargetAmount;

    /// <summary>
    /// OPTIONAL: Per-unit value multiplier if one stack "unit" represents multiple logical units.
    /// Leave as 1 for most stacks (including Spesos if 1 unit = 1 Speso).
    /// </summary>
    [DataField]
    public int CurrencyValuePerUnit = 1;

    /// <summary>
    /// Help newer players by saying e.g. "steal the chief engineer's advanced magboots"
    /// instead of "steal advanced magboots. Should be a loc string.
    /// </summary>
    [DataField("owner")]
    public string? OwnerText;

    // All this need to be loc string
    [DataField(required: true)]
    public LocId ObjectiveText;

    [DataField(required: true)]
    public LocId ObjectiveNoOwnerText;

    [DataField(required: true)]
    public LocId DescriptionText;

    [DataField(required: true)]
    public LocId DescriptionMultiplyText;
}
