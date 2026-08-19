using Content.Shared.Nutrition.Prototypes;

namespace Content.Server.NPC.Queries.Considerations;

public sealed partial class FoodValueCon : UtilityConsideration
{
    /// <summary>
    /// This consideration will return 0 if the entity's hunger is above this value.
    /// </summary>
    [DataField(required: true)]
    public SatiationValue HungerThreshold;
}
