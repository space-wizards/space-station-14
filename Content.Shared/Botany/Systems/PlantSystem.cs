using System.Linq;
using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles plant behavior and growth processing.
/// </summary>
public sealed partial class PlantSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private PlantMutationSystem _mutation = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;

    [Dependency] private EntityQuery<PlantHolderComponent> _holderQuery = default!;
    [Dependency] private EntityQuery<PlantToxinsComponent> _toxinsQuery = default!;
    [Dependency] private EntityQuery<PlantWeedPestComponent> _weedPestQuery = default!;
    [Dependency] private EntityQuery<PlantTrayComponent> _trayQuery = default!;

    private readonly List<Entity<PlantHolderComponent>> _holdersToUpdate = [];

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _holdersToUpdate.Clear();
        var query = EntityQueryEnumerator<PlantHolderComponent>();
        while (query.MoveNext(out var uid, out var plantHolder))
        {
            if (plantHolder.NextUpdate > _gameTiming.CurTime)
                continue;

            plantHolder.NextUpdate = _gameTiming.CurTime;
            DirtyField(uid, plantHolder, nameof(plantHolder.NextUpdate));
            _holdersToUpdate.Add((uid, plantHolder));
        }

        foreach (var ent in _holdersToUpdate)
        {
            UpdatePlant(ent.AsNullable());
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<PlantComponent> ent, ref MapInitEvent args)
    {
        PlantingPlant(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnCrossPollinate(Entity<PlantComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossInt(ref ent.Comp.Yield, pollenData.Yield);
        _mutation.CrossFloat(ref ent.Comp.Endurance, pollenData.Endurance);
        _mutation.CrossFloat(ref ent.Comp.Lifespan, pollenData.Lifespan);
        _mutation.CrossFloat(ref ent.Comp.Maturation, pollenData.Maturation);
        _mutation.CrossFloat(ref ent.Comp.Production, pollenData.Production);
        _mutation.CrossFloat(ref ent.Comp.Potency, pollenData.Potency);
        _mutation.CrossTrait(ent.Owner, args.PollenData);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnPlantGrow(Entity<PlantComponent> ent, ref PlantGrowEvent args)
    {
        if (!_holderQuery.TryComp(ent.Owner, out var holder))
            return;

        // Check if plant is too old.
        if (holder.Age > ent.Comp.Lifespan)
            _plantHolder.AdjustsHealth(ent.Owner, -ent.Comp.OldAgeDamage);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<PlantComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!_holderQuery.HasComp(ent.Owner))
            return;

        using (args.PushGroup(nameof(PlantComponent)))
        {
            args.PushMarkup(GetPlantExamineMarkup(ent.AsNullable()));
            foreach (var mutation in GetPlantMutationDescriptions(ent.Owner))
            {
                args.PushMarkup(Loc.GetString(mutation));
            }
        }
    }

    /// <summary>
    /// Processes one plant's growth cycle and related effects.
    /// </summary>
    public void UpdatePlant(Entity<PlantHolderComponent?> ent, bool force = false)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var curTime = _gameTiming.CurTime;

        if (!force && curTime < ent.Comp.LastCycle + ent.Comp.CycleDelay)
            return;

        ent.Comp.LastCycle = curTime;
        DirtyField(ent, ent.Comp, nameof(ent.Comp.LastCycle));

        if (_plantHolder.IsDead(ent.Owner))
            return;

        TryGetTray(ent.Owner, out var trayEnt);
        var plantGrow = new PlantGrowEvent(GetNetEntity(trayEnt.Owner));
        RaiseLocalEvent(ent.Owner, ref plantGrow);

        // Process mutations.
        if (ent.Comp.MutationLevel > 0)
        {
            _mutation.CheckRandomMutations(ent.Owner, Math.Min(ent.Comp.MutationLevel, ent.Comp.MaxMutationLevel));
            ent.Comp.MutationLevel = 0;
            DirtyField(ent, ent.Comp, nameof(ent.Comp.MutationLevel));
        }

        if (ent.Comp.Health <= 0)
            _plantHolder.KillPlant(ent.Owner);
    }

    /// <summary>
    /// Forces an update of the tray by external cause.
    /// </summary>
    [PublicAPI]
    public void ForceUpdate(Entity<PlantComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        _plantHolder.AdjustsSkipAging(ent.Owner, 1);
        UpdatePlant(ent.Owner, force: true);
    }

    /// <summary>
    /// Tries to get the tray entity that the plant is in.
    /// </summary>
    [PublicAPI]
    public bool TryGetTray(Entity<PlantComponent?> ent, out Entity<PlantTrayComponent> trayEnt)
    {
        trayEnt = default!;
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        trayEnt.Owner = Transform(ent.Owner).ParentUid;
        if (!_trayQuery.TryComp(trayEnt.Owner, out var trayComp))
            return false;

        trayEnt.Comp = trayComp;
        return true;
    }

    /// <summary>
    /// Adjusts the potency of a plant component.
    /// </summary>
    [PublicAPI]
    public void AdjustPotency(Entity<PlantComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Potency = Math.Max(0, ent.Comp.Potency + amount);
        DirtyField(ent, nameof(ent.Comp.Potency));
    }

    /// <summary>
    /// Adjusts the lifespan of a plant component.
    /// </summary>
    [PublicAPI]
    public void AdjustLifespan(Entity<PlantComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Lifespan = Math.Max(0, ent.Comp.Lifespan + amount);
        DirtyField(ent, nameof(ent.Comp.Lifespan));
    }

    /// <summary>
    /// Adjusts the endurance of a plant component.
    /// </summary>
    [PublicAPI]
    public void AdjustEndurance(Entity<PlantComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Endurance = MathF.Max(0f, ent.Comp.Endurance + amount);
        DirtyField(ent, nameof(ent.Comp.Endurance));
    }

    /// <summary>
    /// Adjusts the yield of a plant component.
    /// </summary>
    [PublicAPI]
    public void AdjustYield(Entity<PlantComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Yield = Math.Max(0, ent.Comp.Yield + amount);
        DirtyField(ent, nameof(ent.Comp.Yield));
    }

    /// <summary>
    /// Adjusts the maturation time of a plant component.
    /// Must be at least 1 to prevent divide-by-zero in growth stage calculations.
    /// </summary>
    [PublicAPI]
    public void AdjustMaturation(Entity<PlantComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Maturation = MathF.Max(1f, ent.Comp.Maturation + amount);
        DirtyField(ent, nameof(ent.Comp.Maturation));

        if (ent.Comp.Production < ent.Comp.Maturation)
        {
            ent.Comp.Production = ent.Comp.Maturation;
            DirtyField(ent, nameof(ent.Comp.Production));
        }
    }

    /// <summary>
    /// Adjusts the production time of a plant component.
    /// Should not be lower than <see cref="PlantComponent.Maturation"/>.
    /// </summary>
    [PublicAPI]
    public void AdjustProduction(Entity<PlantComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Production = MathF.Max(ent.Comp.Maturation, ent.Comp.Production + amount);
        DirtyField(ent, nameof(ent.Comp.Production));
    }

    /// <summary>
    /// Removes the plant from the tray.
    /// </summary>
    [PublicAPI]
    public void RemovePlant(Entity<PlantComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        PredictedQueueDel(ent);
    }

    /// <summary>
    /// Gets the growth stage value of the plant.
    /// </summary>
    [PublicAPI]
    public int GetGrowthStageValue(Entity<PlantComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return 1;

        if (!_holderQuery.TryComp(ent.Owner, out var plantHolder))
            return 1;

        int growthStage;
        if (plantHolder.Age < ent.Comp.Maturation)
            growthStage = (int)(plantHolder.Age * ent.Comp.GrowthStages / ent.Comp.Maturation);
        else
            growthStage = ent.Comp.GrowthStages;

        return Math.Max(1, growthStage);
    }

    /// <summary>
    /// Gets the age at which the plant will next become ready for harvest.
    /// </summary>
    /// <returns>The plant age at the next harvest, or zero if the plant is unavailable.</returns>
    [PublicAPI]
    public int GetNextHarvestAge(Entity<PlantComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false)
            || !_holderQuery.TryComp(ent.Owner, out var holder))
            return 0;

        if (holder.ReadyForHarvest)
            return holder.Age;

        var productionCycles = (int)MathF.Floor(ent.Comp.Production) + 1;
        var lastHarvest = holder.Age < ent.Comp.Maturation ? (int)MathF.Ceiling(ent.Comp.Maturation) - 1 : holder.LastHarvest;

        return Math.Max(holder.Age, lastHarvest + productionCycles);
    }

    /// <summary>
    /// Planting a plant.
    /// </summary>
    [PublicAPI]
    public void PlantingPlant(Entity<PlantComponent?> ent, float? healthOverride = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!_holderQuery.TryComp(ent.Owner, out var plantHolder))
            return;

        plantHolder.Health = healthOverride ?? ent.Comp.Endurance;
        DirtyFields(ent, plantHolder, null, nameof(plantHolder.Health));
    }

    /// <summary>
    /// Gets the warnings markup of the plant.
    /// </summary>
    [PublicAPI]
    public string GetPlantWarningsMarkup(Entity<PlantHolderComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return string.Empty;

        var markup = new List<string>();
        if (ent.Comp.ImproperHeat)
            markup.Add(Loc.GetString("plant-component-improper-heat-warning"));

        if (ent.Comp.ImproperPressure)
            markup.Add(Loc.GetString("plant-component-improper-pressure-warning"));

        if (ent.Comp.MissingGas)
            markup.Add(Loc.GetString("plant-component-missing-gas-warning"));

        if (TryGetTray(ent.Owner, out var tray))
        {
            if (_toxinsQuery.TryComp(ent.Owner, out var toxins)
                && tray.Comp.ToxinLevel > toxins.ToxinsTolerance)
            {
                markup.Add(Loc.GetString("plant-component-toxins-high-warning"));
            }

            if (_weedPestQuery.TryComp(ent.Owner, out var weedPest))
            {
                if (tray.Comp.WeedLevel > weedPest.WeedTolerance)
                    markup.Add(Loc.GetString("plant-component-weeds-high-warning"));

                if (tray.Comp.PestLevel > weedPest.PestTolerance)
                    markup.Add(Loc.GetString("plant-component-pests-high-warning"));
            }
        }

        return string.Join("\n", markup);
    }

    /// <summary>
    /// Gets the non-mutation markup shown when examining a growing plant.
    /// </summary>
    [PublicAPI]
    public string GetPlantExamineMarkup(Entity<PlantComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !_holderQuery.TryComp(ent.Owner, out var holder))
            return string.Empty;

        var markup = new List<string>();
        if (_plantHolder.IsDead(ent.Owner))
            markup.Add(Loc.GetString("plant-component-dead-plant-matter-message"));

        if (_plantHolder.GetHealthThreshold(ent.Owner))
        {
            markup.Add(Loc.GetString(
                "plant-component-something-already-growing-low-health-message",
                ("healthState",
                    Loc.GetString(holder.Age > ent.Comp.Lifespan
                        ? "plant-component-plant-old-adjective"
                        : "plant-component-plant-unhealthy-adjective"))));
        }

        return string.Join("\n", markup);
    }

    /// <summary>
    /// Gets the mutation descriptions shown when examining a growing plant.
    /// </summary>
    [PublicAPI]
    public IEnumerable<LocId> GetPlantMutationDescriptions(EntityUid uid)
    {
        return AllComps<PlantTraitsComponent>(uid)
            .OrderBy(trait => trait.GetType().FullName)
            .Select(trait => trait.TraitState)
            .OfType<LocId>();
    }

    /// <summary>
    /// Gets the states markup of the plant.
    /// </summary>
    [PublicAPI]
    public string GetPlantStateMarkup(EntityUid uid, PlantComponent? component = null)
    {
        if (component == null && !Resolve(uid, ref component, false))
            return string.Empty;

        var markup = new List<string>();
        markup.Add(Loc.GetString("seed-component-plant-yield-text", ("seedYield", component.Yield)));
        markup.Add(Loc.GetString("seed-component-plant-potency-text", ("seedPotency", component.Potency)));

        return string.Join("\n", markup);
    }
}
