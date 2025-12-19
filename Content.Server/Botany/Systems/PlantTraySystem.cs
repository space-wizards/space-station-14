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
using Content.Shared.Random;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Systems;

public sealed class PlantTraySystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly ProtoId<TagPrototype> HoeTag = "Hoe";

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantTrayComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<PlantTrayComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlantTrayComponent, SolutionTransferredEvent>(OnSolutionTransferred);
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
            Update((uid, tray));
        }
    }

    private void OnExamine(Entity<PlantTrayComponent> ent, ref ExaminedEvent args)
    {
        var (uid, component) = ent;

        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(PlantTrayComponent)))
        {
            if (component.PlantEntity == null || Deleted(component.PlantEntity))
                args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));

            if (component.WeedLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-weed-high-level-message"));

            if (component.PestLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-pest-high-level-message"));

            args.PushMarkup(Loc.GetString($"plant-holder-component-water-level-message",
                ("waterLevel", (int)component.WaterLevel)));
            args.PushMarkup(Loc.GetString($"plant-holder-component-nutrient-level-message",
                ("nutritionLevel", (int)component.NutritionLevel)));

            if (component.DrawWarnings)
            {
                if (component.Toxins > 40f)
                    args.PushMarkup(Loc.GetString("plant-holder-component-toxins-high-warning"));

                if (component.ImproperHeat)
                    args.PushMarkup(Loc.GetString("plant-holder-component-heat-improper-warning"));

                if (component.ImproperPressure)
                    args.PushMarkup(Loc.GetString("plant-holder-component-pressure-improper-warning"));

                if (component.MissingGas > 0)
                    args.PushMarkup(Loc.GetString("plant-holder-component-gas-missing-warning"));
            }
        }
    }

    private void OnInteractUsing(Entity<PlantTrayComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var (uid, tray) = ent;

        // Planting seeds is tray interaction.
        if (TryComp(args.Used, out SeedComponent? seeds))
        {
            if (tray.PlantEntity == null || Deleted(tray.PlantEntity))
            {
                args.Handled = true;
                var plantUid = Spawn(seeds.PlantProtoId, Transform(uid).Coordinates);

                if (seeds.PlantData != null)
                    _botany.ApplyPlantSnapshotData(plantUid, seeds.PlantData);

                if (!TryComp<PlantDataComponent>(plantUid, out var plantData))
                    return;

                var name = Loc.GetString(plantData.DisplayName);
                var noun = Loc.GetString(plantData.Noun);
                _popup.PopupCursor(Loc.GetString("plant-holder-component-plant-success-message",
                        ("seedName", name),
                        ("seedNoun", noun)),
                    args.User,
                    PopupType.Medium);

                PlantingPlant(uid, plantUid);

                if (TryComp<PlantHolderComponent>(tray.PlantEntity!.Value, out var plantHolder)
                    && seeds.HealthOverride != null)
                {
                    plantHolder.Health = seeds.HealthOverride.Value;
                }

                if (TryComp<PaperLabelComponent>(args.Used, out var paperLabel))
                    _itemSlots.TryEjectToHands(args.Used, paperLabel.LabelSlot, args.User);

                QueueDel(args.Used);

                if (plantData.PlantLogImpact != null)
                    _adminLogger.Add(LogType.Botany, plantData.PlantLogImpact.Value,
                        $"{ToPrettyString(args.User):player} planted {Loc.GetString(plantData.DisplayName):seed} at Pos:{Transform(uid).Coordinates}.");

                return;
            }

            args.Handled = true;
            _popup.PopupCursor(
                Loc.GetString("plant-holder-component-already-seeded-message", ("name", MetaData(uid).EntityName)),
                args.User,
                PopupType.Medium);
            return;
        }

        // Hoe uproots weeds on the tray.
        if (_tag.HasTag(args.Used, HoeTag))
        {
            args.Handled = true;
            if (tray.WeedLevel > 0)
            {
                _popup.PopupCursor(
                    Loc.GetString("plant-holder-component-remove-weeds-message", ("name", MetaData(uid).EntityName)),
                    args.User,
                    PopupType.Medium);
                _popup.PopupEntity(
                    Loc.GetString("plant-holder-component-remove-weeds-others-message",
                        ("otherName", MetaData(args.User).EntityName)),
                    uid,
                    Filter.PvsExcept(args.User),
                    true);
                tray.WeedLevel = 0;
                UpdateSprite(ent.AsNullable());
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-no-weeds-message"), args.User);
            }

            return;
        }

        // Shovel removes the currently planted entity.
        if (HasComp<ShovelComponent>(args.Used))
        {
            args.Handled = true;
            if (tray.PlantEntity != null && !Deleted(tray.PlantEntity))
            {
                _popup.PopupCursor(
                    Loc.GetString("plant-holder-component-remove-plant-message", ("name", MetaData(uid).EntityName)),
                    args.User,
                    PopupType.Medium);
                _popup.PopupEntity(
                    Loc.GetString("plant-holder-component-remove-plant-others-message",
                        ("name", MetaData(args.User).EntityName)),
                    uid,
                    Filter.PvsExcept(args.User),
                    true);
                RemovePlant(ent.AsNullable());
            }
            else
            {
                _popup.PopupCursor(
                    Loc.GetString("plant-holder-component-no-plant-message", ("name", MetaData(uid).EntityName)),
                    args.User);
            }

            return;
        }

        // Composting produce is tray interaction (tray stores reagents/resources).
        if (TryComp<ProduceComponent>(args.Used, out var produce))
        {
            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("plant-holder-component-compost-message",
                    ("owner", uid),
                    ("usingItem", args.Used)),
                args.User,
                PopupType.Medium);
            _popup.PopupEntity(Loc.GetString("plant-holder-component-compost-others-message",
                    ("user", Identity.Entity(args.User, EntityManager)),
                    ("usingItem", args.Used),
                    ("owner", uid)),
                uid,
                Filter.PvsExcept(args.User),
                true);

            if (_solutionContainer.TryGetSolution(args.Used, produce.SolutionName, out var soln2, out var solution2))
            {
                if (_solutionContainer.ResolveSolution(uid, tray.SoilSolutionName, ref tray.SoilSolution, out var solution1))
                {
                    // We try to fit as much of the composted plant's contained solution into the hydroponics tray as we can,
                    // since the plant will be consumed anyway.
                    var fillAmount = FixedPoint2.Min(solution2.Volume, solution1.AvailableVolume);
                    _solutionContainer.TryAddSolution(tray.SoilSolution.Value, _solutionContainer.SplitSolution(soln2.Value, fillAmount));

                    ForceUpdateByExternalCause(ent.AsNullable());
                }
            }

            var plantData = produce.PlantData;
            if (plantData != null &&
                plantData.TryGetComponent(_componentFactory, out PlantComponent? compostPlant))
            {
                var nutrientBonus = compostPlant.Potency / 2.5f;
                AdjustNutrient(ent.AsNullable(), nutrientBonus);
            }

            QueueDel(args.Used);
        }
    }

    /// <summary>
    /// Planting a plant in a tray.
    /// </summary>
    [PublicAPI]
    public void PlantingPlant(Entity<PlantTrayComponent?> trayEnt, Entity<PlantComponent?> plantEnt)
    {
        var (trayUid, trayComp) = trayEnt;
        var (plantUid, plantComp) = plantEnt;

        if (!Resolve(trayUid, ref trayComp, false) || !Resolve(plantUid, ref plantComp, false))
            return;

        if (!TryComp<PlantHolderComponent>(plantUid, out var plantHolder))
            return;

        plantHolder.Dead = false;
        plantHolder.Age = 1;
        plantHolder.Health = plantComp.Endurance;

        if (TryComp<PlantHarvestComponent>(plantUid, out var harvest))
        {
            harvest.ReadyForHarvest = false;
            harvest.LastHarvest = 0;
        }

        trayComp.LastCycle = _gameTiming.CurTime;

        _transform.SetCoordinates(plantUid, Transform(trayUid).Coordinates);
        _transform.SetParent(plantUid, trayUid);
        trayComp.PlantEntity = plantUid;

        UpdateSprite(trayEnt.AsNullable());
    }

    private void OnSolutionTransferred(Entity<PlantTrayComponent> ent, ref SolutionTransferredEvent args)
    {
        _audio.PlayPvs(ent.Comp.WateringSound, ent.Owner);
    }

    private void Mutate(Entity<PlantTrayComponent?> ent, float severity)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        if (component.PlantEntity != null && !Deleted(component.PlantEntity))
            _mutation.MutatePlant(ent, component.PlantEntity.Value, severity);
    }

    public void Update(Entity<PlantTrayComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        UpdateReagents(ent);

        var curTime = _gameTiming.CurTime;

        // ForceUpdate is used for external triggers like swabbing
        if (component.ForceUpdate)
            component.ForceUpdate = false;
        else if (curTime < component.LastCycle + component.CycleDelay)
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(ent);
            return;
        }

        component.LastCycle = curTime;

        if (component.PlantEntity == null || Deleted(component.PlantEntity))
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(ent);
            return;
        }

        var plantUid = component.PlantEntity.Value;
        if (!TryComp<PlantHolderComponent>(plantUid, out var plantHolder))
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(ent);
            return;
        }

        if (plantHolder.Dead)
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(ent);
            return;
        }

        var plantGrow = new OnPlantGrowEvent((uid, component));
        RaiseLocalEvent(plantUid, ref plantGrow);
        RaiseLocalEvent(uid, ref plantGrow);

        // Process mutations.
        if (plantHolder.MutationLevel > 0)
        {
            Mutate(ent, Math.Min(plantHolder.MutationLevel, 25));
            component.UpdateSpriteAfterUpdate = true;
            plantHolder.MutationLevel = 0;
        }

        if (plantHolder.Health <= 0)
        {
            _plantHolder.Die(plantUid);
            component.UpdateSpriteAfterUpdate = true;
        }

        if (component.UpdateSpriteAfterUpdate)
            UpdateSprite(ent);
    }

    /// <summary>
    /// Removes the plant from the tray.
    /// </summary>
    /// <param name="ent">The entity tray component.</param>
    [PublicAPI]
    public void RemovePlant(Entity<PlantTrayComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        if (component.PlantEntity == null || Deleted(component.PlantEntity))
            return;

        QueueDel(component.PlantEntity.Value);
        component.PlantEntity = null;

        component.PestLevel = 0;
        component.ImproperPressure = false;
        component.ImproperHeat = false;

        UpdateSprite(ent);
    }

    /// <summary>
    /// Adjusts the nutrient level of the tray.
    /// </summary>
    /// <param name="ent">The entity tray component.</param>
    /// <param name="amount">The amount to adjust the nutrient level by.</param>
    [PublicAPI]
    public void AdjustNutrient(Entity<PlantTrayComponent?> ent, float amount)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        component.NutritionLevel += amount;
    }

    /// <summary>
    /// Adjusts the water level of the tray.
    /// </summary>
    /// <param name="ent">The entity tray component.</param>
    /// <param name="amount">The amount to adjust the water level by.</param>
    [PublicAPI]
    public void AdjustWater(Entity<PlantTrayComponent?> ent, float amount)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        component.WaterLevel += amount;

        // Water dilutes toxins.
        if (amount > 0)
        {
            component.Toxins -= amount * 4f;
        }
    }

    /// <summary>
    /// Updates the reagents of the tray.
    /// </summary>
    /// <param name="ent">The entity tray component.</param>
    [PublicAPI]
    public void UpdateReagents(Entity<PlantTrayComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        if (!_solutionContainer.ResolveSolution(uid, component.SoilSolutionName, ref component.SoilSolution, out var solution))
            return;

        if (component.PlantEntity == null || Deleted(component.PlantEntity))
            return;

        if (!TryComp<PlantHolderComponent>(component.PlantEntity.Value, out var plantHolder))
            return;

        if (solution.Volume > 0 && (plantHolder == null || plantHolder.MutationLevel < 25))
        {
            foreach (var entry in component.SoilSolution.Value.Comp.Solution.Contents)
            {
                var reagentProto = _prototype.Index<ReagentPrototype>(entry.Reagent.Prototype);
                _entityEffects.ApplyEffects(uid, [.. reagentProto.PlantMetabolisms], entry.Quantity.Float());
                _entityEffects.ApplyEffects(component.PlantEntity.Value, [.. reagentProto.PlantMetabolisms], entry.Quantity.Float());
            }

            _solutionContainer.RemoveEachReagent(component.SoilSolution.Value, FixedPoint2.New(1));
        }
    }

    /// <summary>
    /// Updates the sprite of the tray.
    /// </summary>
    [PublicAPI]
    public void UpdateSprite(Entity<PlantTrayComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        PlantHarvestComponent? harvest = null;
        PlantComponent? plant = null;
        PlantHolderComponent? plantHolder = null;
        PlantDataComponent? plantData = null;
        if (component.PlantEntity != null && !Deleted(component.PlantEntity))
        {
            TryComp(component.PlantEntity.Value, out harvest);
            TryComp(component.PlantEntity.Value, out plant);
            TryComp(component.PlantEntity.Value, out plantHolder);
            TryComp(component.PlantEntity.Value, out plantData);
        }

        component.UpdateSpriteAfterUpdate = false;

        // Tray should never render plant sprite.
        _appearance.SetData(uid, PlantVisuals.PlantState, string.Empty, app);

        if (component.PlantEntity != null && !Deleted(component.PlantEntity) && harvest != null && plant != null && plantHolder != null && plantData != null)
        {
            if (TryComp<AppearanceComponent>(component.PlantEntity.Value, out var plantApp))
            {
                _appearance.SetData(component.PlantEntity.Value, PlantVisuals.PlantRsi, plantData.PlantRsi.ToString(), plantApp);

                if (plantHolder.Dead)
                    _appearance.SetData(component.PlantEntity.Value, PlantVisuals.PlantState, "dead", plantApp);
                else if (harvest.ReadyForHarvest)
                    _appearance.SetData(component.PlantEntity.Value, PlantVisuals.PlantState, "harvest", plantApp);
                else
                {
                    if (plantHolder.Age < plant.Maturation)
                    {
                        var growthStage = Math.Max(1, (int)(plantHolder.Age * plant.GrowthStages / plant.Maturation));
                        _appearance.SetData(component.PlantEntity.Value, PlantVisuals.PlantState, $"stage-{growthStage}", plantApp);
                    }
                    else
                    {
                        _appearance.SetData(component.PlantEntity.Value, PlantVisuals.PlantState, $"stage-{plant.GrowthStages}", plantApp);
                    }
                }
            }
        }

        if (!component.DrawWarnings)
            return;

        // TODO: dehardcode those alert levels.
        _appearance.SetData(uid, PlantHolderVisuals.HealthLight,
            plantHolder != null && plant != null && plantHolder.Health <= plant.Endurance / 2f, app);
        _appearance.SetData(uid, PlantHolderVisuals.WaterLight, component.WaterLevel <= 15, app);
        _appearance.SetData(uid, PlantHolderVisuals.NutritionLight, component.NutritionLevel <= 8, app);
        _appearance.SetData(uid,
            PlantHolderVisuals.AlertLight,
            component.WeedLevel >= 5 || component.PestLevel >= 5 || component.Toxins >= 40 || component.ImproperHeat
            || component.ImproperPressure || component.MissingGas > 0,
            app);
        _appearance.SetData(uid, PlantHolderVisuals.HarvestLight, harvest is { ReadyForHarvest: true }, app);
    }

    /// <summary>
    /// Forces an update of the tray by external cause.
    /// </summary>
    [PublicAPI]
    public void ForceUpdateByExternalCause(Entity<PlantTrayComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        if (component.PlantEntity != null && !Deleted(component.PlantEntity) &&
            TryComp<PlantHolderComponent>(component.PlantEntity.Value, out var plantHolder))
        {
            plantHolder.SkipAging++;
        }

        component.ForceUpdate = true;
        Update(ent);
    }

    /// <summary>
    /// Checks if the tray contains a plant entity.
    /// </summary>
    [PublicAPI]
    public bool TryGetPlant(Entity<PlantTrayComponent?> ent, [NotNullWhen(true)] out EntityUid? plant)
    {
        plant = null;
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        plant = ent.Comp.PlantEntity;
        if (plant == null || Deleted(plant))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if the tray contains a living plant entity.
    /// </summary>
    [PublicAPI]
    public bool HasPlantAlive(Entity<PlantTrayComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!TryGetPlant(ent.Owner, out var plant))
            return false;

        if (!TryComp<PlantHolderComponent>(plant, out var holder))
            return false;

        if (holder.Dead)
            return false;

        return true;
    }
}
