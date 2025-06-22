using Content.Shared.Objectives.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that requires the player to eat a specific food item.
/// </summary>
[RegisterComponent]
public sealed partial class EatSpecificFoodConditionComponent : Component
{
    /// <summary>
    /// Which entities this condition should target.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist;

    /// Text and sprites for the condition.
    [DataField]
    public LocId Name = string.Empty;
    [DataField]
    public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;
    [DataField]
    public LocId TitleText = string.Empty;
    [DataField]
    public LocId DescriptionText = string.Empty;
    [DataField]
    public LocId DescriptionTextMultiple = string.Empty;

    /// <summary>
    /// The amount of chosen food eaten.
    /// </summary>
    [DataField]
    public int FoodEaten;
}
