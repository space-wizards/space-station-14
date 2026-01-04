using JetBrains.Annotations;
using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Content.Server.Popups;
using Content.Shared.Botany;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Random;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Handles plant behavior and growth processing.
/// </summary>
public sealed class PlantSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantTraySystem _tray = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;

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

        // Check if plant is too old.
        if (holder.Age > ent.Comp.Lifespan)
            _plantHolder.AdjustsHealth(ent.Owner, -_random.Next(3, 5));
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
            args.PushMarkup(Loc.GetString("plant-holder-component-something-already-growing-message",
                ("seedName", displayName),
                ("toBeForm", displayName.EndsWith('s') ? "are" : "is")));

            if (_plantHolder.IsDead(ent.Owner))
                args.PushMarkup(Loc.GetString("plant-holder-component-dead-plant-message"));

            if (holder.Health <= ent.Comp.Endurance / 2f)
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
                    foreach (var markup in trait.GetPlantStateMarkup())
                    {
                        args.PushMarkup(markup);
                    }
                }
            }
        }
    }

    private void Mutate(Entity<PlantComponent?> ent, float severity)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        _mutation.MutatePlant(ent, severity);
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

        // ForceUpdate is used for external triggers like swabbing
        if (plantHolder.ForceUpdate)
            plantHolder.ForceUpdate = false;
        else if (curTime < plantHolder.LastCycle + plantHolder.CycleDelay)
            return;

        plantHolder.LastCycle = curTime;

        if (_plantHolder.IsDead(ent.Owner))
            return;

        TryGetTray(ent, out var trayEnt);
        var plantGrow = new OnPlantGrowEvent(trayEnt);
        RaiseLocalEvent(ent.Owner, ref plantGrow);

        // Process mutations.
        if (plantHolder.MutationLevel > 0)
        {
            Mutate(ent, Math.Min(plantHolder.MutationLevel, 25));
            plantHolder.MutationLevel = 0;
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
    }

    /// <summary>
    /// Adjusts the endurance of a plant component.
    /// </summary>
    [PublicAPI]
    public void AdjustEndurance(Entity<PlantComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Endurance = Math.Max(0, ent.Comp.Endurance);
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
    }

    /// <summary>
    /// Removes the plant from the tray.
    /// </summary>
    [PublicAPI]
    public void RemovePlant(Entity<PlantComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        QueueDel(uid);
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

        var maturation = Math.Max(ent.Comp.Maturation, 1f);
        return Math.Max(1, (int)(plantHolder.Age * ent.Comp.GrowthStages / maturation));
    }

    /// <summary>
    /// Updates the sprite of the plant.
    /// </summary>
    [PublicAPI]
    public void UpdateSprite(Entity<PlantComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!TryComp<PlantHolderComponent>(ent.Owner, out var plantHolder)
            || !TryComp<PlantDataComponent>(ent.Owner, out var plantData)
            || !TryComp<PlantHarvestComponent>(ent.Owner, out var harvest))
            return;

        if (!TryComp<AppearanceComponent>(ent.Owner, out var plantApp))
            return;

        _appearance.SetData(ent.Owner, PlantVisuals.PlantRsi, plantData.PlantRsi.ToString(), plantApp);

        if (_plantHolder.IsDead(ent.Owner))
        {
            _appearance.SetData(ent.Owner, PlantVisuals.PlantState, "dead", plantApp);
        }
        else if (harvest.ReadyForHarvest)
        {
            _appearance.SetData(ent.Owner, PlantVisuals.PlantState, "harvest", plantApp);
        }
        else
        {
            if (plantHolder.Age < ent.Comp.Maturation)
            {
                var growthStage = Math.Max(1, (int)(plantHolder.Age * ent.Comp.GrowthStages / ent.Comp.Maturation));
                _appearance.SetData(ent.Owner, PlantVisuals.PlantState, $"stage-{growthStage}", plantApp);
            }
            else
            {
                _appearance.SetData(ent.Owner, PlantVisuals.PlantState, $"stage-{ent.Comp.GrowthStages}", plantApp);
            }
        }

        if (TryGetTray(ent, out var trayEnt))
            _tray.UpdateWarnings(trayEnt);
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

        if (TryComp<PlantHarvestComponent>(ent.Owner, out var harvest))
        {
            harvest.ReadyForHarvest = false;
            harvest.LastHarvest = 0;
        }

        UpdateSprite(ent.AsNullable());
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
        if (ent.Comp.Toxins > 40f)
            markup += "\n" + Loc.GetString("plant-holder-component-toxins-high-warning");

        if (ent.Comp.ImproperHeat)
            markup += "\n" + Loc.GetString("plant-holder-component-heat-improper-warning");

        if (ent.Comp.ImproperPressure)
            markup += "\n" + Loc.GetString("plant-holder-component-pressure-improper-warning");

        if (ent.Comp.MissingGas)
            markup += "\n" + Loc.GetString("plant-holder-component-gas-missing-warning");

        if (ent.Comp.PestLevel >= 5)
            markup += "\n" + Loc.GetString("plant-holder-component-pest-high-level-message");

        return markup;
    }

    public string GetPlantStateMarkup(EntityUid uid, PlantComponent? component = null)
    {
        if (component == null && !Resolve(uid, ref component, false))
            return string.Empty;

        var markup = Loc.GetString("seed-component-plant-yield-text", ("seedYield", component.Yield));
        markup += "\n" + Loc.GetString("seed-component-plant-potency-text", ("seedPotency", component.Potency));

        return markup;
    }
}
