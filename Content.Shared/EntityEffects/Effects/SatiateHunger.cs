using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Attempts to find a HungerComponent on the target,
/// and to update it's hunger values.
/// </summary>
public sealed partial class SatiateHunger : EntityEffect
{
    private const float DefaultNutritionFactor = 3.0f;

    /// <summary>
    ///     How much hunger is satiated.
    ///     Is multiplied by quantity if used with EntityEffectReagentArgs.
    /// </summary>
    [DataField("factor")] public float NutritionFactor { get; set; } = DefaultNutritionFactor;

    //Remove reagent at set rate, satiate hunger if a HungerComponent can be found
    public override void Effect(EntityEffectBaseArgs args)
    {
        var entman = args.EntityManager;
        if (!entman.TryGetComponent(args.TargetEntity, out HungerComponent? hunger))
            return;
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            entman.System<HungerSystem>().ModifyHunger(reagentArgs.TargetEntity, NutritionFactor * (float) reagentArgs.Quantity, hunger);
        }
        else
        {
            entman.System<HungerSystem>().ModifyHunger(args.TargetEntity, NutritionFactor, hunger);
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-satiate-hunger", ("chance", Probability), ("relative", NutritionFactor / DefaultNutritionFactor));
}
