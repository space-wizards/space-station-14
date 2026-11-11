using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles plant mutations, including random mutation effects, crossbreeding, and
/// inheritance of plant properties and traits from pollen.
/// </summary>
public sealed partial class PlantMutationSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private PlantSystem _plant = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;

    [Dependency] private EntityQuery<PlantChemicalsComponent> _chemicalsQuery;
    [Dependency] private EntityQuery<PlantComponent> _plantQuery;

    /// <summary>
    /// Resolves mutation data from overrides, checks passed override (typically from mutation list) then the plant, and finally uses the original prototype for default values.
    /// </summary>
    [PublicAPI]
    public static ResolvedPlantMutation ResolveMutation(
        RandomPlantMutationOverride mutation,
        PlantComponent plant,
        IPrototypeManager prototypes)
    {
        var prototype = prototypes.Index(mutation.Prototype);
        plant.MutationOverrides.TryGetValue(mutation.Prototype, out var plantOverride);

        return new ResolvedPlantMutation(
            plantOverride?.Name ?? mutation.Name ?? prototype.Name,
            plantOverride?.Description ?? mutation.Description ?? prototype.Description,
            plantOverride?.Effect ?? mutation.Effect ?? prototype.Effect);
    }

    /// <summary>
    /// For each mutation table, go through each mutation.
    /// For each random mutation, see if it occurs on this plant this check.
    /// </summary>
    [PublicAPI]
    public void CheckRandomMutations(Entity<PlantComponent?> ent, ProtoId<RandomPlantMutationListPrototype> mutationTableId, float severity)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var mutationTable = ProtoMan.Index(mutationTableId);
        foreach (var entry in mutationTable.Mutations)
        {
            var triggers = 0;
            ResolvedPlantMutation? mutation = null;
            while (entry.BaseOdds * severity > triggers) // If the odds are greater than one, potentially trigger the mutation multiple times
            {
                mutation ??= ResolveMutation(entry.Mutation, ent.Comp, ProtoMan);
                var odds = entry.BaseOdds * severity - triggers;
                triggers++;
                if (Random(Math.Min(odds, 1.0f)))
                {
                    _entityEffects.TryApplyEffect(ent, mutation.Value.Effect);
                }
            }
        }

        var ev = new PlantMutationsChangedEvent(ent.Owner);
        RaiseLocalEvent(ref ev);
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
        PlantSpeciesChange(newPlantUid, newPlantProto);

        if (_plant.TryGetTray(oldPlant.Owner, out var trayEnt))
            _plantTray.PlantingPlantInTray(trayEnt.AsNullable(), newPlantUid);
        else
            _plant.PlantingPlant(newPlantUid);

        _plant.ForceUpdate(newPlantUid);
        var ev = new PlantSpeciesChangedEvent(oldPlant.Owner, newPlantUid, newPlantProto);
        RaiseLocalEvent(ref ev);
        QueueDel(oldPlant);
    }

    private void ChemicalsSpeciesChange(EntityUid plantUid, EntProtoId plantProto)
    {
        if (!_botany.TryGetPlantComponent<PlantChemicalsComponent>(null, plantProto, out var newPlantChemicals)
            || !_chemicalsQuery.TryComp(plantUid, out var oldPlantChemicals))
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
    /// Copies information from the PlantComponent on a prototype over to a given plant.
    /// </summary>
    /// <remarks>
    /// Growth stages and mutation overrides belong to the new species rather than the cloned plant state.
    /// TODO: better support excluding fields depending on clone context.
    /// </remarks>
    private void PlantSpeciesChange(EntityUid plantUid, EntProtoId plantProto)
    {
        if (!_botany.TryGetPlantComponent<PlantComponent>(null, plantProto, out var newSpecies)
            || !_plantQuery.TryComp(plantUid, out var plant))
            return;

        plant.GrowthStages = newSpecies.GrowthStages;
        plant.MutationOverrides = _serialization.CreateCopy(newSpecies.MutationOverrides, notNullableOverride: true);
        DirtyField(plantUid, plant, nameof(PlantComponent.GrowthStages));
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

    /// <summary>
    /// Selects a floating value from the plant or pollen.
    /// </summary>
    [PublicAPI]
    public void CrossFloat(ref float val, float other)
    {
        val = Random(0.5f) ? val : other;
    }

    /// <summary>
    /// Selects an integer value from the plant or pollen.
    /// </summary>
    [PublicAPI]
    public void CrossInt(ref int val, int other)
    {
        val = Random(0.5f) ? val : other;
    }

    /// <summary>
    /// Selects a Boolean value from the plant or pollen.
    /// </summary>
    [PublicAPI]
    public void CrossBool(ref bool val, bool other)
    {
        val = Random(0.5f) ? val : other;
    }

    /// <summary>
    /// Crosses plant trait components from pollen to the target plant.
    /// </summary>
    [PublicAPI]
    public void CrossTrait(EntityUid val, EntityUid pollenData)
    {
        foreach (var component in AllComps<PlantTraitsComponent>(val))
        {
            if (component.PollinationProtected)
                continue;

            if (HasComp(pollenData, component.GetType()))
                continue;

            if (Random(0.5f))
                RemComp(val, component);
        }

        foreach (var component in AllComps<PlantTraitsComponent>(pollenData))
        {
            if (component.PollinationProtected)
                continue;

            if (HasComp(val, component.GetType()))
                continue;

            if (Random(0.5f))
                AddComp(val, _serialization.CreateCopy(component, notNullableOverride: true));
        }
    }

    private bool Random(float p)
    {
        // TODO: A reliable method for PredictedRandom in the engine is needed.
        if (!_net.IsServer)
            return false;

        return _random.Prob(p);
    }
}
