using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Botany;
using Content.Shared.Burial.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Systems;

public sealed class PlantTraySystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantTrayComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<PlantTrayComponent, SolutionTransferredEvent>(OnSolutionTransferred);
    }

    private void OnExamine(Entity<PlantTrayComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(PlantTrayComponent)))
        {
            if (TryGetPlant(ent.AsNullable(), out var plantUid))
                args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));

            args.PushMarkup(Loc.GetString("plant-holder-component-water-level-message",
                ("waterLevel", (int)ent.Comp.WaterLevel)));
            args.PushMarkup(Loc.GetString("plant-holder-component-nutrient-level-message",
                ("nutritionLevel", (int)ent.Comp.NutritionLevel)));

            args.PushMarkup(GetTrayWarningsMarkup(ent.AsNullable()));
            if (plantUid != null && ent.Comp.DrawWarnings)
                args.PushMarkup(_plant.GetPlantWarningsMarkup(plantUid.Value));
        }
    }

    private void OnSolutionTransferred(Entity<PlantTrayComponent> ent, ref SolutionTransferredEvent args)
    {
        _audio.PlayPvs(ent.Comp.WateringSound, ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlantTrayComponent>();
        while (query.MoveNext(out var uid, out var tray))
        {
            if (tray.NextUpdate > _gameTiming.CurTime)
                continue;

            tray.NextUpdate = _gameTiming.CurTime + tray.UpdateDelay;
            UpdateWarnings(uid);
            UpdateReagents(uid);
            GrowthWeeds(uid);

            var ev = new TrayUpdateEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    /// <summary>
    /// Updates the sprite of the tray.
    /// </summary>
    [PublicAPI]
    public void UpdateWarnings(Entity<PlantTrayComponent?> ent)
    {
        var (trayUid, trayComp) = ent;

        if (!Resolve(trayUid, ref trayComp, false))
            return;

        if (!trayComp.DrawWarnings)
            return;

        if (!TryComp<AppearanceComponent>(trayUid, out var app))
            return;

        if (!TryGetPlant(ent, out var plantUid))
        {
            _appearance.SetData(trayUid, PlantTrayVisuals.HealthLight, false, app);
            _appearance.SetData(trayUid, PlantTrayVisuals.AlertLight, false, app);
            _appearance.SetData(trayUid, PlantTrayVisuals.HarvestLight, false, app);
            return;
        }

        if (!TryComp<PlantHolderComponent>(plantUid, out var plantHolder)
            || !TryComp<PlantComponent>(plantUid, out var plant)
            || !TryComp<PlantHarvestComponent>(plantUid, out var harvest))
            return;

        // TODO: dehardcode those alert levels.
        _appearance.SetData(trayUid, PlantTrayVisuals.HealthLight, plantHolder.Health <= plant.Endurance / 2f, app);
        _appearance.SetData(trayUid, PlantTrayVisuals.WaterLight, trayComp.WaterLevel <= 15, app);
        _appearance.SetData(trayUid, PlantTrayVisuals.NutritionLight, trayComp.NutritionLevel <= 8, app);
        _appearance.SetData(trayUid,
            PlantTrayVisuals.AlertLight,
            trayComp.WeedLevel >= 5 || plantHolder.PestLevel >= 5 || plantHolder.Toxins >= 40 || plantHolder.ImproperHeat
            || plantHolder.ImproperPressure || plantHolder.MissingGas,
            app);
        _appearance.SetData(trayUid, PlantTrayVisuals.HarvestLight, harvest is { ReadyForHarvest: true }, app);
    }

    /// <summary>
    /// Updates the reagents of the tray.
    /// </summary>
    [PublicAPI]
    public void UpdateReagents(Entity<PlantTrayComponent?> ent)
    {
        var (trayUid, trayComp) = ent;

        if (!Resolve(trayUid, ref trayComp, false))
            return;

        if (!_solutionContainer.ResolveSolution(trayUid, trayComp.SoilSolutionName, ref trayComp.SoilSolution, out var solution))
            return;

        if (!TryGetPlant(ent, out var plantUid))
            return;

        if (!TryComp<PlantHolderComponent>(plantUid, out var plantHolder))
            return;

        if (solution.Volume > 0 && plantHolder.MutationLevel < 25)
        {
            var contents = trayComp.SoilSolution.Value.Comp.Solution.Contents.ToArray();

            foreach (var entry in contents)
            {
                var reagentProto = _prototype.Index<ReagentPrototype>(entry.Reagent.Prototype);
                _entityEffects.ApplyEffects(trayUid, [.. reagentProto.PlantMetabolisms], entry.Quantity.Float());
                _entityEffects.ApplyEffects(plantUid.Value, [.. reagentProto.PlantMetabolisms], entry.Quantity.Float());
            }

            _solutionContainer.RemoveEachReagent(trayComp.SoilSolution.Value, FixedPoint2.New(1));
        }
    }

    private void GrowthWeeds(Entity<PlantTrayComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp is not { WaterLevel: > 10, NutritionLevel: > 5 })
            return;

        if (TryGetPlant(ent, out var plantUid))
        {
            if (!TryComp<WeedPestGrowthComponent>(plantUid.Value, out var weedPestGrowth))
                return;

            if (ent.Comp.WeedLevel > weedPestGrowth.WeedTolerance)
                _plantHolder.AdjustsHealth(plantUid.Value, -weedPestGrowth.WeedDamageAmount);
        }

        if (_random.Prob(ent.Comp.WeedGrowthChance))
            AdjustWeed(ent, ent.Comp.WeedGrowthAmount);
    }

    /// <summary>
    /// Planting a plant in a tray.
    /// </summary>
    [PublicAPI]
    public void PlantingPlantInTray(Entity<PlantTrayComponent?> trayEnt, EntityUid plantUid, float? healthOverride = null)
    {
        var (trayUid, trayComp) = trayEnt;

        if (!Resolve(trayUid, ref trayComp, false))
            return;

        _plant.PlantingPlant(plantUid, healthOverride);
        _transform.SetCoordinates(plantUid, Transform(trayUid).Coordinates);
        _transform.SetParent(plantUid, trayUid);
        trayComp.PlantEntity = plantUid;
    }

    /// <summary>
    /// Adjusts the nutrient level of the tray.
    /// </summary>
    [PublicAPI]
    public void AdjustNutrient(Entity<PlantTrayComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.NutritionLevel += amount;
        ent.Comp.NutritionLevel = MathHelper.Clamp(ent.Comp.NutritionLevel, 0f, ent.Comp.MaxNutritionLevel);
    }

    /// <summary>
    /// Adjusts the water level of the tray.
    /// </summary>
    [PublicAPI]
    public void AdjustWater(Entity<PlantTrayComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.WaterLevel += amount;
        ent.Comp.WaterLevel = MathHelper.Clamp(ent.Comp.WaterLevel, 0f, ent.Comp.MaxWaterLevel);

        // Water dilutes toxins.
        if (TryGetPlant(ent, out var plantUid))
            _plantHolder.AdjustsToxins(plantUid.Value, -amount * 4f);
    }

    /// <summary>
    /// Adjusts the weed level of the tray.
    /// </summary>
    [PublicAPI]
    public void AdjustWeed(Entity<PlantTrayComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.WeedLevel += amount * ent.Comp.WeedCoefficient;
        ent.Comp.WeedLevel = MathHelper.Clamp(ent.Comp.WeedLevel, 0f, ent.Comp.MaxWeedLevel);
    }

    /// <summary>
    /// Tries to get the plant entity in the tray.
    /// </summary>
    [PublicAPI]
    public bool TryGetPlant(Entity<PlantTrayComponent?> ent, [NotNullWhen(true)] out EntityUid? plant)
    {
        plant = null;
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        plant = ent.Comp.PlantEntity;
        if (plant == null || Deleted(plant))
        {
            ent.Comp.PlantEntity = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to get the living plant entity in the tray.
    /// </summary>
    [PublicAPI]
    public bool TryGetAlivePlant(Entity<PlantTrayComponent?> ent, [NotNullWhen(true)] out EntityUid? plant)
    {
        plant = null;
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!TryGetPlant(ent.Owner, out plant))
            return false;

        return !_plantHolder.IsDead(ent.Owner);
    }

    /// <summary>
    /// Gets the warnings markup of the tray.
    /// </summary>
    [PublicAPI]
    public string GetTrayWarningsMarkup(Entity<PlantTrayComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return string.Empty;

        var markup = string.Empty;
        if (ent.Comp.WeedLevel >= 5)
            markup += "\n" + Loc.GetString("plant-holder-component-weed-high-level-message");

        return markup;
    }
}

/// <summary>
/// Event raised when a tray is updated.
/// </summary>
[ByRefEvent]
public readonly record struct TrayUpdateEvent;
