using JetBrains.Annotations;
using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles plant mutations, including random mutation effects, crossbreeding, and
/// inheritance of plant properties and traits from pollen.
/// </summary>
public sealed partial class MutationSystem : EntitySystem
{
    private static readonly ProtoId<RandomPlantMutationListPrototype> RandomPlantMutations = "RandomPlantMutations";
    private RandomPlantMutationListPrototype _randomMutations = default!;

    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private PlantSystem _plant = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;

    public override void Initialize()
    {
        _randomMutations = ProtoMan.Index(RandomPlantMutations);
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
            if (Random(ent, Math.Min(mutation.BaseOdds * severity, 1.0f)))
            {
                if (mutation.AppliesToPlant)
                    _entityEffects.TryApplyEffect(ent, mutation.Effect);

                // Stat adjustments do not persist by being an attached effect, they just change the stat.
                if (mutation.Persists && ent.Comp.Mutations.All(m => m.Name != mutation.Name))
                {
                    ent.Comp.Mutations.Add(mutation);
                    DirtyField(ent, nameof(ent.Comp.Mutations));
                }
            }
        }
    }

    /// <summary>
    /// Replaces the current plant species with a new one from prototype,
    /// preserving lifecycle state.
    /// </summary>
    [PublicAPI]
    public void SpeciesChange(Entity<PlantDataComponent?> oldPlant, EntProtoId newPlantProto)
    {
        if (!Resolve(oldPlant, ref oldPlant.Comp, false))
            return;

        if (oldPlant.Comp.MutationPrototypes.Count == 0)
            return;

        if (!_net.IsServer)
            return;

        // Clone state via snapshot and apply to new plant.
        var snapshot = _botany.ClonePlantSnapshotData(oldPlant.Owner, cloneLifecycle: true);
        if (snapshot == null)
            return;

        var newPlantUid = SpawnAtPosition(newPlantProto, Transform(oldPlant.Owner).Coordinates);
        _botany.ApplyPlantSnapshotData(snapshot, newPlantUid, cloneLifecycle: true);
        _botany.DeletePlantSnapshot(snapshot);

        ChemicalsSpeciesChange(newPlantUid, newPlantProto);

        if (_plant.TryGetTray(oldPlant.Owner, out var trayEnt))
            _plantTray.PlantingPlantInTray(trayEnt.AsNullable(), newPlantUid);
        else
            _plant.PlantingPlant(newPlantUid);

        _plant.ForceUpdate(newPlantUid);
        QueueDel(oldPlant);
    }

    private void ChemicalsSpeciesChange(EntityUid plantUid, EntProtoId plantProto)
    {
        if (!_botany.TryGetPlantComponent<PlantChemicalsComponent>(null, plantProto, out var newPlantChemicals)
            || !TryComp<PlantChemicalsComponent>(plantUid, out var oldPlantChemicals))
            return;

        var oldPlant = oldPlantChemicals.Chemicals;
        var newPlant = newPlantChemicals.Chemicals;

        // Adding the new chemicals from the new species.
        foreach (var otherChem in newPlant)
        {
            oldPlant.TryAdd(otherChem.Key, otherChem.Value);
        }

        // Removing the inherent chemicals from the old species. Leaving mutated/crossbred ones intact.
        foreach (var originalChem in oldPlant)
        {
            if (!newPlant.ContainsKey(originalChem.Key) && originalChem.Value.Inherent)
                oldPlant.Remove(originalChem.Key);
        }

        Dirty(plantUid, oldPlantChemicals);
    }

    /// <summary>
    /// Combines mutations from the pollen and target plants.
    /// </summary>
    [PublicAPI]
    public void CrossMutations(EntityUid pollenPlant, EntProtoId? pollenProtoId, EntityUid targetPlant)
    {
        if (!_botany.TryGetPlantComponent<PlantComponent>(pollenPlant, pollenProtoId, out var pollenCore) ||
            !TryComp<PlantComponent>(targetPlant, out var targetCore))
            return;

        // LINQ Explanation
        // For the list of mutation effects on both plants, use a 50% chance to pick each one.
        // Union all of the chosen mutations into one list, and pick ones with a Distinct (unique) name.
        targetCore.Mutations = targetCore.Mutations.Where(_ => Random(pollenPlant, 0.5f)).Union(pollenCore.Mutations.Where(_ => Random(pollenPlant, 0.5f))).DistinctBy(m => m.Name).ToList();

        // Hybrids have a high chance of being seedless. Balances very
        // effective hybrid crossings.
        if (pollenProtoId != null
            && pollenProtoId != MetaData(targetPlant).EntityPrototype?.ID
            && Random(pollenPlant, 0.7f))
        {
            EnsureComp<PlantTraitSeedlessComponent>(targetPlant);
        }
    }

    /// <summary>
    /// Combines chemical properties from the pollen and target plants.
    /// </summary>
    [PublicAPI]
    public void CrossChemicals(EntityUid uid, ref Dictionary<ProtoId<ReagentPrototype>, PlantChemQuantity> val, Dictionary<ProtoId<ReagentPrototype>, PlantChemQuantity> other)
    {
        // Go through chemicals from the pollen in swab
        foreach (var otherChem in other)
        {
            // if both have same chemical, randomly pick potency ratio from the two.
            if (val.TryGetValue(otherChem.Key, out var value))
            {
                val[otherChem.Key] = Random(uid, 0.5f) ? otherChem.Value : value;
            }
            // if target plant doesn't have this chemical, has 50% chance to add it.
            else
            {
                if (Random(uid, 0.5f))
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
                if (Random(uid, 0.5f))
                {
                    if (val.Count > 1)
                    {
                        val.Remove(thisChem.Key);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Combines gas properties from the pollen and target plants.
    /// </summary>
    [PublicAPI]
    public void CrossGasses(EntityUid uid, ref Dictionary<Gas, float> val, Dictionary<Gas, float> other)
    {
        // Go through gasses from the pollen in swab
        foreach (var otherGas in other)
        {
            // if both have same gas, randomly pick ammount from the two.
            if (val.TryGetValue(otherGas.Key, out var value))
            {
                val[otherGas.Key] = Random(uid, 0.5f) ? otherGas.Value : value;
            }
            // if target plant doesn't have this gas, has 50% chance to add it.
            else
            {
                if (Random(uid, 0.5f))
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
                if (Random(uid, 0.5f))
                {
                    val.Remove(thisGas.Key);
                }
            }
        }
    }

    /// <summary>
    /// Selects a floating value from the plant or pollen.
    /// </summary>
    [PublicAPI]
    public void CrossFloat(EntityUid uid, ref float val, float other)
    {
        val = Random(uid, 0.5f) ? val : other;
    }

    /// <summary>
    /// Selects an integer value from the plant or pollen.
    /// </summary>
    [PublicAPI]
    public void CrossInt(EntityUid uid, ref int val, int other)
    {
        val = Random(uid, 0.5f) ? val : other;
    }

    /// <summary>
    /// Selects a Boolean value from the plant or pollen.
    /// </summary>
    [PublicAPI]
    public void CrossBool(EntityUid uid, ref bool val, bool other)
    {
        val = Random(uid, 0.5f) ? val : other;
    }

    /// <summary>
    /// Crosses plant trait components from pollen to the target plant.
    /// </summary>
    [PublicAPI]
    public void CrossTrait(EntityUid val, EntityUid pollenData)
    {
        foreach (var component in AllComps(pollenData))
        {
            if (component is not PlantTraitsComponent)
                continue;

            if (HasComp(val, component.GetType()))
                continue;

            if (Random(val, 0.5f))
                AddComp(val, _serialization.CreateCopy(component, notNullableOverride: true));
        }
    }

    private bool Random(EntityUid uid, float p)
    {
        return SharedRandomExtensions.PredictedProb(_timing, p, GetNetEntity(uid));
    }
}
