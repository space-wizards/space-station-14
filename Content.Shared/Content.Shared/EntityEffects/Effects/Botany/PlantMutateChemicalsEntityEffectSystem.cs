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
        _plantChemicals.MutateRandomChemical(entity.Owner, args.Effect.RandomChemTables);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateChemicals : EntityEffectBase<PlantMutateChemicals>
{
    /// <summary>
    /// Chemical tables from which this mutation can select.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<WeightedRandomFillSolutionPrototype>> RandomChemTables = [];

    /// <inheritdoc/>
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var list = new List<string>();

        // If your table doesn't exist, no guidebook for you!
        foreach (var tableId in RandomChemTables)
        {
            if (!prototype.Resolve(tableId, out var table))
                continue;

            foreach (var fill in table.Fills)
            {
                foreach (var reagent in fill.Reagents)
                {
                    if (!prototype.Resolve(reagent, out var reagentPrototype))
                        continue;

                    list.Add(reagentPrototype.LocalizedName);
                }
            }
        }

        var names = ContentLocalizationManager.FormatListToOr(list);

        return Loc.GetString("entity-effect-guidebook-plant-mutate-chemicals", ("chance", Probability), ("name", names));
    }
}
