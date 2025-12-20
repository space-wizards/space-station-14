using JetBrains.Annotations;
using System.Linq;
using Content.Server.Botany.Components;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

public sealed class MutationSystem : EntitySystem
{
    private static readonly ProtoId<RandomPlantMutationListPrototype> RandomPlantMutations = "RandomPlantMutations";

    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;

    private RandomPlantMutationListPrototype _randomMutations = default!;

    public override void Initialize()
    {
        _randomMutations = _prototypeManager.Index(RandomPlantMutations);
    }

    /// <summary>
    /// For each random mutation, see if it occurs on this plant this check.
    /// </summary>
    [PublicAPI]
    public void CheckRandomMutations(Entity<PlantTrayComponent?> trayEnt, Entity<PlantComponent?> plantEnt, float severity)
    {
        if (!Resolve(trayEnt, ref trayEnt.Comp, false) || !Resolve(plantEnt, ref plantEnt.Comp, false))
            return;

        foreach (var mutation in _randomMutations.mutations)
        {
            if (Random(Math.Min(mutation.BaseOdds * severity, 1.0f)))
            {
                if (mutation.AppliesToPlant)
                    _entityEffects.TryApplyEffect(trayEnt, mutation.Effect);

                // Stat adjustments do not persist by being an attached effect, they just change the stat.
                if (mutation.Persists && !plantEnt.Comp.Mutations.Any(m => m.Name == mutation.Name))
                    plantEnt.Comp.Mutations.Add(mutation);
            }
        }
    }

    /// <summary>
    /// Checks all defined mutations against a seed to see which of them are applied.
    /// </summary>
    [PublicAPI]
    public void MutatePlant(Entity<PlantTrayComponent?> trayEnt, Entity<PlantComponent?> plantEnt, float severity)
    {
        if (!Resolve(trayEnt, ref trayEnt.Comp, false) || !Resolve(plantEnt, ref plantEnt.Comp, false))
            return;

        CheckRandomMutations(trayEnt, plantEnt, severity);
    }

    /// <summary>
    /// Replaces the current plant species with a new one from prototype,
    /// preserving lifecycle state.
    /// </summary>
    [PublicAPI]
    public void SpeciesChange(Entity<PlantDataComponent?> oldPlant, EntProtoId newPlantEnt, Entity<PlantTrayComponent?> trayEnt)
    {
        if (!Resolve(oldPlant, ref oldPlant.Comp, false) || !Resolve(trayEnt, ref trayEnt.Comp, false))
            return;

        if (oldPlant.Comp.MutationPrototypes.Count == 0)
            return;

        var newPlantUid = Spawn(newPlantEnt);
        var snapshot = _botany.ClonePlantSnapshotData(oldPlant.Owner, cloneLifecycle: true);

        // Clone state via snapshot and apply to new plant.
        QueueDel(oldPlant.Owner);
        _plantTray.PlantingPlant(trayEnt, newPlantUid);
        _botany.ApplyPlantSnapshotData(newPlantUid, snapshot);
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
        targetCore.Mutations = targetCore.Mutations.Where(m => Random(0.5f)).Union(pollenCore.Mutations.Where(m => Random(0.5f))).DistinctBy(m => m.Name).ToList();

        // Hybrids have a high chance of being seedless. Balances very
        // effective hybrid crossings.
        if (pollenProtoId != null
            && pollenProtoId != MetaData(targetPlant).EntityPrototype?.ID
            && Random(0.7f))
        {
            if (TryComp<PlantTraitsComponent>(targetPlant, out var traits))
                traits.Seedless = true;
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

    private bool Random(float p)
    {
        return _random.Prob(p);
    }
}
