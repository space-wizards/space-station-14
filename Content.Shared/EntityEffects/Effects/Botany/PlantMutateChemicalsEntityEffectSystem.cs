using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Localizations;
using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// Entity effect that mutates the chemicals of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateChemicalsEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantMutateChemicals>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PlantChemicalsSystem _plantChemicals = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateChemicals> args)
    {
        var randomChems = _proto.Index(args.Effect.RandomPickBotanyReagent).Fills;
        _plantChemicals.MutateRandomChemical(entity.Owner, randomChems);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateChemicals : EntityEffectBase<PlantMutateChemicals>
{
    /// <summary>
    /// The Reagent list this mutation draws from.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomFillSolutionPrototype> RandomPickBotanyReagent = "RandomPickBotanyReagent";

    /// <inheritdoc/>
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var list = new List<string>();

        // If your table doesn't exist, no guidebook for you!
        if (!prototype.Resolve(RandomPickBotanyReagent, out var table))
            return string.Empty;

        foreach (var fill in table.Fills)
        {
            foreach (var reagent in fill.Reagents)
            {
                if (!prototype.Resolve(reagent, out var proto))
                    continue;

                list.Add(proto.LocalizedName);
            }
        }

        var names = ContentLocalizationManager.FormatListToOr(list);

        return Loc.GetString("entity-effect-guidebook-plant-mutate-chemicals", ("chance", Probability), ("name", names));
    }
}
