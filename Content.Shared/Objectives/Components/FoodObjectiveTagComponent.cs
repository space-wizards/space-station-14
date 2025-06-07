using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Objectives.Components;

/// <summary>
/// Tags an entity to be compatible with the EatSpecificFoodConditionComponent objective.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FoodObjectiveTagComponent : Component
{
    /// <summary>
    /// The eat group to which this item belongs.
    /// Matches up with EatSpecificFoodConditionComponent.GroupTag.
    /// </summary>
    [DataField(required: true)]
    public List<string> Tags;
}
