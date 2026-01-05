using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Botany.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

public sealed class PlantTraySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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

            args.PushMarkup(GetTrayWarningsMarkup(ent.AsNullable()));
            args.PushMarkup(Loc.GetString("plant-holder-component-water-level-message",
                ("waterLevel", (int)ent.Comp.WaterLevel)));
            args.PushMarkup(Loc.GetString("plant-holder-component-nutrient-level-message",
                ("nutritionLevel", (int)ent.Comp.NutritionLevel)));

            if (plantUid != null && ent.Comp.DrawWarnings)
                args.PushMarkup(_plant.GetPlantWarningsMarkup(plantUid.Value));
        }
    }

    private void OnSolutionTransferred(Entity<PlantTrayComponent> ent, ref SolutionTransferredEvent args)
    {
        _audio.PlayPredicted(ent.Comp.WateringSound, ent, args.User);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlantTrayComponent>();
        while (query.MoveNext(out var uid, out var tray))
        {
            if (tray.NextUpdate > _timing.CurTime)
                continue;

            tray.NextUpdate = _timing.CurTime + tray.UpdateDelay;
            DirtyField(uid, tray, nameof(tray.NextUpdate));
            UpdateReagents(uid);
            GrowthWeeds(uid);

            var ev = new TrayUpdateEvent();
            RaiseLocalEvent(uid, ref ev);
        }
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

        if (solution.Volume <= 0)
            return;

        var contents = trayComp.SoilSolution.Value.Comp.Solution.Contents.ToArray();

        foreach (var entry in contents)
        {
            var reagentProto = _prototype.Index<ReagentPrototype>(entry.Reagent.Prototype);
            _entityEffects.ApplyEffects(trayUid, [.. reagentProto.PlantMetabolisms], entry.Quantity.Float());
            _entityEffects.ApplyEffects(plantUid.Value, [.. reagentProto.PlantMetabolisms], entry.Quantity.Float());
        }

        _solutionContainer.RemoveEachReagent(trayComp.SoilSolution.Value, FixedPoint2.New(1));
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


        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        if (rand.Prob(ent.Comp.WeedGrowthChance))
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
        DirtyField(trayEnt, nameof(trayComp.PlantEntity));
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
        DirtyField(ent, nameof(ent.Comp.NutritionLevel));
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
        DirtyField(ent, nameof(ent.Comp.WaterLevel));

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
        DirtyField(ent, nameof(ent.Comp.WeedLevel));
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
            DirtyField(ent, nameof(ent.Comp.PlantEntity));
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
        if (GetWeedThreshold(ent))
            markup += "\n" + Loc.GetString("plant-holder-component-weed-high-level-message");

        return markup;
    }

    [PublicAPI]
    public bool GetWeedThreshold(Entity<PlantTrayComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return ent.Comp.WeedLevel >= ent.Comp.MaxWeedLevel * 0.5f;
    }
}

/// <summary>
/// Event raised when a tray is updated.
/// </summary>
[ByRefEvent]
public readonly record struct TrayUpdateEvent;
