using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// When consuming food or drink with this component, a status effect will be imposed on the entity.
/// </summary>
/// <remarks>Works with both DrinkComponent and FoodComponent</remarks>
[RegisterComponent, Access(typeof(FoodSystem))]
public sealed partial class StatusEffectConsumableComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Effect = new();

    /// <summary>
    /// How long will the status effect last. The effect scales with the amount of reagent absorbed from the source.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan DurationPerUnit = TimeSpan.FromSeconds(1);
}
