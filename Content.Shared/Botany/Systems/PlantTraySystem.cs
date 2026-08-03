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
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles plant tray state, including plant management, resource consumption,
/// reagent processing, and periodic tray updates.
/// </summary>
public sealed partial class PlantTraySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private PlantSystem _plant = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnExamine(Entity<PlantTrayComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(PlantTrayComponent)))
        {
            if (!TryGetPlant(ent.AsNullable(), out var plantUid))
            {
                args.PushMarkup(Loc.GetString("tray-component-nothing-planted-message"));
                if (TryComp<PlantDataComponent>(plantUid, out var plantData))
                {
                    var name = Loc.GetString(plantData.Name);
                    args.PushMarkup(Loc.GetString("plant-component-something-already-growing-message", ("seedName", name)));
                }
            }

            args.PushMarkup(GetTrayWarningsMarkup(ent.AsNullable()));
            args.PushMarkup(Loc.GetString("tray-component-water-level-message",
                ("waterLevel", (int)ent.Comp.WaterLevel)));
            args.PushMarkup(Loc.GetString("tray-component-nutrient-level-message",
                ("nutritionLevel", (int)ent.Comp.NutritionLevel)));

            if (plantUid != null && ent.Comp.DrawWarnings)
                args.PushMarkup(_plant.GetPlantWarningsMarkup(plantUid.Value));
        }
    }

    [SubscribeLocalEvent]
    private void OnSolutionTransferred(Entity<PlantTrayComponent> ent, ref SolutionTransferredEvent args)
    {
        _audio.PlayPredicted(ent.Comp.WateringSound, ent, args.User);
    }

    // Workaround for https://github.com/space-wizards/space-station-14/pull/35314
    [SubscribeLocalEvent]
    private void OnEntRemoved(Entity<PlantTrayComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution and clear our cached reference
        if (args.Entity == ent.Comp.SoilSolution?.Owner)
            ent.Comp.SoilSolution = null;
    }

    /// <summary>
    /// Updates trays whose periodic processing is due.
    /// </summary>
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
            var reagentProto = ProtoMan.Index<ReagentPrototype>(entry.Reagent.Prototype);
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
            if (!TryComp<PlantWeedPestComponent>(plantUid.Value, out var weedPestGrowth))
                return;

            if (ent.Comp.WeedLevel > weedPestGrowth.WeedTolerance)
                _plantHolder.AdjustsHealth(plantUid.Value, -weedPestGrowth.WeedDamageAmount);
        }

        if (SharedRandomExtensions.PredictedProb(_timing, ent.Comp.WeedGrowthChance, GetNetEntity(ent)))
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
        if (amount > 0)
            AdjustToxin(ent, -amount * 4f);
    }

    /// <summary>
    /// Adjusts the pest level of the tray.
    /// </summary>
    [PublicAPI]
    public void AdjustPest(Entity<PlantTrayComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.PestLevel = MathHelper.Clamp(ent.Comp.PestLevel + amount, 0f, ent.Comp.MaxPestLevel);
        DirtyField(ent, nameof(ent.Comp.PestLevel));
    }

    /// <summary>
    /// Adjusts the toxin level of the tray.
    /// </summary>
    [PublicAPI]
    public void AdjustToxin(Entity<PlantTrayComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.ToxinLevel = MathHelper.Clamp(ent.Comp.ToxinLevel + amount, 0f, ent.Comp.MaxToxinLevel);
        DirtyField(ent, nameof(ent.Comp.ToxinLevel));
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
    /// Checks whether the tray's toxin level has reached half its maximum.
    /// </summary>
    [PublicAPI]
    public bool GetToxinThreshold(Entity<PlantTrayComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return ent.Comp.ToxinLevel >= ent.Comp.MaxToxinLevel * 0.5f;
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

    public bool TryGetAlivePlant(Entity<PlantTrayComponent?> ent)
    {
        return TryGetAlivePlant(ent, out _);
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

        return !_plantHolder.IsDead(plant.Value);
    }

    /// <summary>
    /// Gets the warnings markup of the tray.
    /// </summary>
    [PublicAPI]
    public string GetTrayWarningsMarkup(Entity<PlantTrayComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return string.Empty;

        var markup = new List<string>();
        if (GetWeedThreshold(ent))
            markup.Add(Loc.GetString("tray-component-weed-high-level-warning"));

        if (GetWaterThreshold(ent))
            markup.Add(Loc.GetString("tray-component-water-low-warning"));

        if (GetNutrientThreshold(ent))
            markup.Add(Loc.GetString("tray-component-nutrient-low-warning"));

        if (GetToxinThreshold(ent))
            markup.Add(Loc.GetString("tray-component-toxin-high-level-warning"));

        if (GetPestThreshold(ent))
            markup.Add(Loc.GetString("tray-component-pest-high-level-warning"));

        return string.Join("\n", markup);
    }

    /// <summary>
    /// Checks whether the tray's weed level has reached half its maximum.
    /// </summary>
    [PublicAPI]
    public bool GetWeedThreshold(Entity<PlantTrayComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return ent.Comp.WeedLevel >= ent.Comp.MaxWeedLevel * 0.5f;
    }

    /// <summary>
    /// Checks whether the tray's pest level has reached half its maximum.
    /// </summary>
    [PublicAPI]
    public bool GetPestThreshold(Entity<PlantTrayComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return ent.Comp.PestLevel >= ent.Comp.MaxPestLevel * 0.5f;
    }

    /// <summary>
    /// Checks whether the tray's water level is critically low.
    /// </summary>
    [PublicAPI]
    public bool GetWaterThreshold(Entity<PlantTrayComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return ent.Comp.WaterLevel <= ent.Comp.MaxWaterLevel * 0.1f;
    }

    /// <summary>
    /// Checks whether the tray's nutrient level is critically low.
    /// </summary>
    [PublicAPI]
    public bool GetNutrientThreshold(Entity<PlantTrayComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return ent.Comp.NutritionLevel <= ent.Comp.MaxNutritionLevel * 0.1f;
    }
}

/// <summary>
/// Event raised when a tray is updated.
/// </summary>
[ByRefEvent]
public readonly record struct TrayUpdateEvent;
