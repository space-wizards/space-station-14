using Content.Shared.Whitelist;
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

    /// <summary>
    /// Name of the food being eaten.
    /// </summary>
    [DataField]
    public LocId Name = string.Empty;

    /// <summary>
    /// Sprite to use for the condition.
    /// </summary>
    [DataField]
    public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;

    /// <summary>
    /// Title string to use. <see cref="Name"/> is inserted as {itemName}.
    /// </summary>
    [DataField]
    public LocId TitleText = string.Empty;

    /// <summary>
    /// Description text for a singular entity needing to be consumed.
    /// </summary>
    [DataField]
    public LocId DescriptionText = string.Empty;

    /// <summary>
    /// Description text for a multiple entities needing to be consumed.
    /// </summary>
    [DataField]
    public LocId DescriptionTextMultiple = string.Empty;

    /// <summary>
    /// The amount of chosen food eaten.
    /// </summary>
    [DataField]
    public int FoodEaten;
}
