using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Examine;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles plant behavior and growth processing.
/// </summary>
public sealed class PlantSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly WeedPestGrowthSystem _weedPestGrowth = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlantComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
        SubscribeLocalEvent<PlantComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<PlantComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<PlantComponent> ent, ref MapInitEvent args)
    {
        PlantingPlant(ent.AsNullable());
    }

    private void OnCrossPollinate(Entity<PlantComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossInt(ref ent.Comp.Yield, pollenData.Yield);
        _mutation.CrossInt(ref ent.Comp.GrowthStages, pollenData.GrowthStages);
        _mutation.CrossFloat(ref ent.Comp.Endurance, pollenData.Endurance);
        _mutation.CrossFloat(ref ent.Comp.Lifespan, pollenData.Lifespan);
        _mutation.CrossFloat(ref ent.Comp.Maturation, pollenData.Maturation);
        _mutation.CrossFloat(ref ent.Comp.Production, pollenData.Production);
        _mutation.CrossFloat(ref ent.Comp.Potency, pollenData.Potency);
    }

    private void OnPlantGrow(Entity<PlantComponent> ent, ref OnPlantGrowEvent args)
    {
        if (!TryComp<PlantHolderComponent>(ent.Owner, out var holder))
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_gameTiming.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        // Check if plant is too old.
        if (holder.Age > ent.Comp.Lifespan)
            _plantHolder.AdjustsHealth(ent.Owner, -rand.Next(3, 5));
    }

    private void OnExamined(Entity<PlantComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryComp<PlantHolderComponent>(ent.Owner, out var holder)
            || !TryComp<PlantDataComponent>(ent.Owner, out var plantData))
            return;

        using (args.PushGroup(nameof(PlantComponent)))
        {
            args.PushMarkup(GetPlantStateMarkup(ent));

            var displayName = Loc.GetString(plantData.DisplayName);
            args.PushMarkup(Loc.GetString("plant-holder-component-something-already-growing-message", ("seedName", displayName)));

            if (_plantHolder.IsDead(ent.Owner))
                args.PushMarkup(Loc.GetString("plant-holder-component-dead-plant-message"));

            if (_plantHolder.GetHealthThreshold(ent.Owner))
            {
                args.PushMarkup(Loc.GetString(
                    "plant-holder-component-something-already-growing-low-health-message",
                    ("healthState",
                        Loc.GetString(holder.Age > ent.Comp.Lifespan
                            ? "plant-holder-component-plant-old-adjective"
                            : "plant-holder-component-plant-unhealthy-adjective"))));
            }

            if (TryComp<PlantTraitsComponent>(ent.Owner, out var traits))
            {
                var traitList = traits.Traits;
                foreach (var trait in traitList)
                {
                    foreach (var markup in trait.GetTraitStateMarkup())
                    {
                        args.PushMarkup(markup);
                    }
                }
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlantHolderComponent>();
        while (query.MoveNext(out var uid, out var plantHolder))
        {
            if (plantHolder.NextUpdate > _gameTiming.CurTime)
                continue;

            plantHolder.NextUpdate = _gameTiming.CurTime;
            DirtyField(uid, plantHolder, nameof(plantHolder.NextUpdate));
            Update(uid);
        }
    }

    public void Update(Entity<PlantComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!TryComp<PlantHolderComponent>(ent, out var plantHolder))
            return;

        var curTime = _gameTiming.CurTime;

        // ForceUpdate is used for external triggers like swabbing.
        if (plantHolder.ForceUpdate)
        {
            plantHolder.ForceUpdate = false;
            DirtyField(ent, plantHolder, nameof(plantHolder.ForceUpdate));
        }
        else if (curTime < plantHolder.LastCycle + plantHolder.CycleDelay)
        {
            return;
        }

        plantHolder.LastCycle = curTime;
        DirtyField(ent, plantHolder, nameof(plantHolder.LastCycle));

        if (_plantHolder.IsDead(ent.Owner))
            return;

        TryGetTray(ent, out var trayEnt);
        var plantGrow = new OnPlantGrowEvent(GetNetEntity(trayEnt.Owner));
        RaiseLocalEvent(ent.Owner, ref plantGrow);

        // Process mutations.
        if (plantHolder.MutationLevel > 0)
        {
            _mutation.CheckRandomMutations(ent, Math.Min(plantHolder.MutationLevel, plantHolder.MaxMutationLevel));
            plantHolder.MutationLevel = 0;
            DirtyField(ent, plantHolder, nameof(plantHolder.MutationLevel));
        }

        if (plantHolder.Health <= 0)
            _plantHolder.Die(ent.Owner);
    }

    /// <summary>
    /// Forces an update of the tray by external cause.
    /// </summary>
    [PublicAPI]
    public void ForceUpdateByExternalCause(Entity<PlantComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!TryComp<PlantHolderComponent>(ent.Owner, out var plantHolder))
            return;

        plantHolder.ForceUpdate = true;
        DirtyField(ent.Owner, plantHolder, nameof(plantHolder.ForceUpdate));

        _plantHolder.AdjustsSkipAging(ent.Owner, 1);
        Update(ent);
    }

    /// <summary>
    /// Tries to get the tray entity that the plant is in.
    /// </summary>
    [PublicAPI]
    public bool TryGetTray(Entity<PlantComponent?> ent, out Entity<PlantTrayComponent?> trayEnt)
    {
        trayEnt = default!;
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        trayEnt.Owner = Transform(ent.Owner).ParentUid;
        if (!TryComp(trayEnt.Owner, out trayEnt.Comp))
            return false;

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

        if (!TryComp<PlantHolderComponent>(ent.Owner, out var plantHolder))
            return 1;

        return Math.Max(1, (int)(plantHolder.Age * ent.Comp.GrowthStages / ent.Comp.Maturation));
    }

    /// <summary>
    /// Planting a plant.
    /// </summary>
    [PublicAPI]
    public void PlantingPlant(Entity<PlantComponent?> ent, float? healthOverride = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!TryComp<PlantHolderComponent>(ent.Owner, out var plantHolder))
            return;

        plantHolder.Health = healthOverride ?? ent.Comp.Endurance;
        plantHolder.LastCycle = _gameTiming.CurTime;
        Dirty(ent, plantHolder);

        _plantHarvest.ResetHarvest(ent.Owner);
    }

    /// <summary>
    /// Gets the warnings markup of the plant.
    /// </summary>
    [PublicAPI]
    public string GetPlantWarningsMarkup(Entity<PlantHolderComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return string.Empty;

        var markup = string.Empty;
        if (_plantHolder.GetToxinsThreshold(ent))
            markup += "\n" + Loc.GetString("plant-holder-component-toxins-high-warning");

        if (ent.Comp.ImproperHeat)
            markup += "\n" + Loc.GetString("plant-holder-component-heat-improper-warning");

        if (ent.Comp.ImproperPressure)
            markup += "\n" + Loc.GetString("plant-holder-component-pressure-improper-warning");

        if (ent.Comp.MissingGas)
            markup += "\n" + Loc.GetString("plant-holder-component-gas-missing-warning");

        if (_weedPestGrowth.GetPestThreshold(ent.Owner))
            markup += "\n" + Loc.GetString("plant-holder-component-pest-high-level-message");

        return markup;
    }

    [PublicAPI]
    public string GetPlantStateMarkup(EntityUid uid, PlantComponent? component = null)
    {
        if (component == null && !Resolve(uid, ref component, false))
            return string.Empty;

        var markup = Loc.GetString("seed-component-plant-yield-text", ("seedYield", component.Yield));
        markup += "\n" + Loc.GetString("seed-component-plant-potency-text", ("seedPotency", component.Potency));

        return markup;
    }
}
