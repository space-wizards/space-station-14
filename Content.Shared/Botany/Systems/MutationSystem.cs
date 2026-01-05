using JetBrains.Annotations;
using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

public sealed class MutationSystem : EntitySystem
{
    private static readonly ProtoId<RandomPlantMutationListPrototype> RandomPlantMutations = "RandomPlantMutations";
    private RandomPlantMutationListPrototype _randomMutations = default!;

    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly PlantTraitsSystem _plantTraits = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        _randomMutations = _prototypeManager.Index(RandomPlantMutations);
    }

    /// <summary>
    /// For each random mutation, see if it occurs on this plant this check.
    /// </summary>
    [PublicAPI]
    public void CheckRandomMutations(Entity<PlantComponent?> ent, float severity)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var mutation in _randomMutations.Mutations)
        {
            if (Random(Math.Min(mutation.BaseOdds * severity, 1.0f)))
            {
                if (mutation.AppliesToPlant)
                    _entityEffects.TryApplyEffect(ent, mutation.Effect);

                // Stat adjustments do not persist by being an attached effect, they just change the stat.
                if (mutation.Persists && ent.Comp.Mutations.All(m => m.Name != mutation.Name))
                    ent.Comp.Mutations.Add(mutation);

                DirtyField(ent, nameof(ent.Comp.Mutations));
            }
        }
    }

    /// <summary>
    /// Replaces the current plant species with a new one from prototype,
    /// preserving lifecycle state.
    /// </summary>
    [PublicAPI]
    public void SpeciesChange(Entity<PlantDataComponent?> oldPlant, EntProtoId newPlantEnt)
    {
        if (!Resolve(oldPlant, ref oldPlant.Comp, false))
            return;

        if (oldPlant.Comp.MutationPrototypes.Count == 0)
            return;

        // Clone state via snapshot and apply to new plant.
        var snapshot = _botany.ClonePlantSnapshotData(oldPlant.Owner, cloneLifecycle: true);
        var newPlantUid = EntityManager.PredictedSpawn(newPlantEnt, _transform.GetMapCoordinates(oldPlant.Owner), snapshot);

        if (_plant.TryGetTray(oldPlant.Owner, out var trayEnt))
            _plantTray.PlantingPlantInTray(trayEnt, newPlantUid);
        else
            _plant.PlantingPlant(newPlantUid);

        PredictedQueueDel(oldPlant);
        _plant.ForceUpdateByExternalCause(newPlantUid);
    }

    [PublicAPI]
    public void CrossMutations(ComponentRegistry pollenPlant, EntProtoId? pollenProtoId, EntityUid targetPlant)
    {
        if (!_botany.TryGetPlantComponent<PlantComponent>(pollenPlant, pollenProtoId, out var pollenCore) ||
            !TryComp<PlantComponent>(targetPlant, out var targetCore))
            return;

        // LINQ Explanation
        // For the list of mutation effects on both plants, use a 50% chance to pick each one.
        // Union all of the chosen mutations into one list, and pick ones with a Distinct (unique) name.
        targetCore.Mutations = targetCore.Mutations.Where(_ => Random(0.5f)).Union(pollenCore.Mutations.Where(_ => Random(0.5f))).DistinctBy(m => m.Name).ToList();

        // Hybrids have a high chance of being seedless. Balances very
        // effective hybrid crossings.
        if (pollenProtoId != null
            && pollenProtoId != MetaData(targetPlant).EntityPrototype?.ID
            && Random(0.7f))
        {
            _plantTraits.AddTrait(targetPlant, new TraitSeedless());
        }
    }

    [PublicAPI]
    public void CrossChemicals(ref Dictionary<ProtoId<ReagentPrototype>, PlantChemQuantity> val, Dictionary<ProtoId<ReagentPrototype>, PlantChemQuantity> other)
    {
        // Go through chemicals from the pollen in swab
        foreach (var otherChem in other)
        {
            // if both have same chemical, randomly pick potency ratio from the two.
            if (val.TryGetValue(otherChem.Key, out var value))
            {
                val[otherChem.Key] = Random(0.5f) ? otherChem.Value : value;
            }
            // if target plant doesn't have this chemical, has 50% chance to add it.
            else
            {
                if (Random(0.5f))
                {
                    var fixedChem = otherChem.Value;
                    fixedChem.Inherent = false;
                    val.Add(otherChem.Key, fixedChem);
                }
            }
        }

        // if the target plant has chemical that the pollen in swab does not, 50% chance to remove it.
        foreach (var thisChem in val)
        {
            if (!other.ContainsKey(thisChem.Key))
            {
                if (Random(0.5f))
                {
                    if (val.Count > 1)
                    {
                        val.Remove(thisChem.Key);
                    }
                }
            }
        }
    }

    [PublicAPI]
    public void CrossGasses(ref Dictionary<Gas, float> val, Dictionary<Gas, float> other)
    {
        // Go through gasses from the pollen in swab
        foreach (var otherGas in other)
        {
            // if both have same gas, randomly pick ammount from the two.
            if (val.TryGetValue(otherGas.Key, out var value))
            {
                val[otherGas.Key] = Random(0.5f) ? otherGas.Value : value;
            }
            // if target plant doesn't have this gas, has 50% chance to add it.
            else
            {
                if (Random(0.5f))
                {
                    val.Add(otherGas.Key, otherGas.Value);
                }
            }
        }
        // if the target plant has gas that the pollen in swab does not, 50% chance to remove it.
        foreach (var thisGas in val)
        {
            if (!other.ContainsKey(thisGas.Key))
            {
                if (Random(0.5f))
                {
                    val.Remove(thisGas.Key);
                }
            }
        }
    }

    [PublicAPI]
    public void CrossFloat(ref float val, float other)
    {
        val = Random(0.5f) ? val : other;
    }

    [PublicAPI]
    public void CrossInt(ref int val, int other)
    {
        val = Random(0.5f) ? val : other;
    }

    [PublicAPI]
    public void CrossBool(ref bool val, bool other)
    {
        val = Random(0.5f) ? val : other;
    }

    [PublicAPI]
    public void CrossTrait(ref Entity<PlantTraitsComponent> val, List<PlantTrait> other)
    {
        foreach (var trait in other.ToArray())
        {
            if (val.Comp.Traits.Any(t => t.GetType() == trait.GetType()))
                continue;

            if (Random(0.5f))
                _plantTraits.AddTrait(val.AsNullable(), trait);
        }
    }

    private bool Random(float p)
    {
        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, 1);
        var rand = new System.Random(seed);
        return rand.Prob(p);
    }
}
