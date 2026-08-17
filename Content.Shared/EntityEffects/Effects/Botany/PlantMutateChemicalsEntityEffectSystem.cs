using System.Linq;
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
    [Dependency] private PlantChemicalsSystem _plantChemicals = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateChemicals> args)
    {
        var randomChems = args.Effect.RandomPickBotanyReagents
            .Select(id => ProtoMan.Index(id))
            .ToList();
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
    public List<ProtoId<WeightedRandomFillSolutionPrototype>> RandomPickBotanyReagents = new()
    {
        "RandomPickBotanyReagent",
        "RandomPickFarmReagent",
    };

    /// <inheritdoc/>
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var list = new List<string>();

        foreach (var tableId in RandomPickBotanyReagents)
        {
            // If your table doesn't exist, no guidebook entry for it!
            if (!prototype.Resolve(tableId, out var table))
                continue;

            foreach (var fill in table.Fills)
            {
                foreach (var reagent in fill.Reagents)
                {
                    if (!prototype.Resolve(reagent, out var proto))
                        continue;

                    list.Add(proto.LocalizedName);
                }
            }
        }

        var names = ContentLocalizationManager.FormatListToOr(list);

        return Loc.GetString("entity-effect-guidebook-plant-mutate-chemicals", ("chance", Probability), ("name", names));
    }
}
