using Content.Shared.Nutrition.Prototypes;

namespace Content.Server.NPC.Queries.Considerations;

public sealed partial class DrinkValueCon : UtilityConsideration
{
    /// <summary>
    /// This consideration will return 0 if the entity's thirst is above this value.
    /// </summary>
    [DataField(required: true)]
    public SatiationValue ThirstThreshold;
}
