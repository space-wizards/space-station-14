using Content.Shared.Objectives.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Use an item with the [component] near a specific entity chosen upon assignment.
/// </summary>
[RegisterComponent]
public sealed partial class UseNearEntityConditionComponent : Component
{
    /// <summary>
    /// Whitelist for what entities are eligible for this condition.
    /// E.g. using tags to mark beacons for tourists.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist = default!;

    /// <summary>
    /// The selected entity for this condition.
    /// </summary>
    public EntityUid TargetEntity = EntityUid.Invalid;

    // Text and sprites for the condition.

    /// <summary>
    /// The name of the objective entity; if left empty, uses the entity name instead.
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
