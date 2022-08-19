using Content.Server.Traits.Smoker;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
///     Satiates nicotine craving for players with the Smoker trait.
/// </summary>
[UsedImplicitly]
public sealed class SatiateNicotineCraving : ReagentEffect
{
    /// How much craving is satiated per unit of nicotine.
    [DataField("factor")]
    public float SatiationFactor { get; set; } = 350.0f; // quite high, so players get fast feedback and blunt sharing is encouraged

    /// <summary>
    /// Satiate craving if player has <see cref="SmokerTraitComponent"/>.
    /// </summary>
    public override void Effect(ReagentEffectArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out SmokerTraitComponent? trait))
            return;

        var smokerTraitSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SmokerTraitSystem>();
        smokerTraitSystem.UpdateCraving(trait, -args.Quantity.Float() * SatiationFactor);
    }
}
