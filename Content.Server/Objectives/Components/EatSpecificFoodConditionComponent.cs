using Content.Server.Objectives.Systems;
using Content.Shared.Ninja.Systems;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that requires the player to eat a specific food item.
/// </summary>
[RegisterComponent]
public sealed partial class EatSpecificFoodConditionComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public string GroupTag;

    [DataField(required: true)]
    public LocId DescriptionText;

    [DataField]
    public string ChosenTag;
}
