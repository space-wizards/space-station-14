using Content.Shared.Objectives.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Use an entity with the <see cref="UseNearObjectiveTriggerComponent"/> near an entity selected for this condition.
/// </summary>
[RegisterComponent]
public sealed partial class UseNearObjectiveConditionComponent : Component
{
    /// <summary>
    /// If true, a specific entity matching the TargetWhitelist will be selected as a target upon assigning the objective.
    /// If false, it will instead allow any entity matching the TargetWhitelist when checking.
    /// </summary>
    [DataField]
    public bool TargetSingleEntity = true;

    /// <summary>
    /// Whitelist for what entities are eligible to be selected as target for this condition.
    /// E.g. using tags to mark beacons for tourists.
    /// The <see cref="UseNearObjectiveTargetComponent"/> is always required on the entity and is not necessary to check for here.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist? TargetWhitelist = default!;

    /// <summary>
    /// Whitelist for what entities are eligible to fulfill this condition.
    /// The <see cref="UseNearObjectiveTriggerComponent"/> is always required on the entity and is not necessary to check for here.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist? UseWhitelist = default!;

    /// <summary>
    /// The selected entity for this condition, if <see cref="TargetSingleEntity"/> is true.
    /// </summary>
    [DataField]
    public EntityUid? TargetEntity;

    /// <summary>
    /// If true, the use object requires line-of-sight to the target.
    /// </summary>
    [DataField]
    public bool PreventOcclusion = false;

    // Text and sprites for the condition.

    /// <summary>
    /// The name of the objective entity; if left empty, uses the <see cref="TargetEntity"/> name instead.
    /// </summary>
    [DataField]
    public LocId Name = string.Empty;
    [DataField]
    public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;
    [DataField]
    public LocId TitleText = string.Empty;
    [DataField]
    public LocId DescriptionText = string.Empty;
    /// <summary>
    /// Description text when the entity has been deleted.
    /// </summary>
    [DataField]
    public LocId DescriptionTextDeleted = string.Empty;

    /// <summary>
    /// Keeps track on if the objective has been completed.
    /// </summary>
    [DataField]
    public bool ObjectiveCompleted;
}
