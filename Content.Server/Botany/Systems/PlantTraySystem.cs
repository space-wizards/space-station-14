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
    [Dependency] private readonly PlantSystem _plant = default!;
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

    private void OnExamine(Entity<PlantTrayComponent> ent, ref ExaminedEvent args)
    {
        var (uid, component) = ent;

        if (!args.IsInDetailsRange)
            return;

        TryGetPlant(ent.AsNullable(), out var plantUid);
        TryComp<PlantHolderComponent>(plantUid, out var plantHolder);

        using (args.PushGroup(nameof(PlantTrayComponent)))
        {
            if (component.PlantEntity == null || Deleted(component.PlantEntity))
                args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));

            if (component.WeedLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-weed-high-level-message"));

            if (plantHolder != null && plantHolder.PestLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-pest-high-level-message"));

            args.PushMarkup(Loc.GetString($"plant-holder-component-water-level-message",
                ("waterLevel", (int)component.WaterLevel)));
            args.PushMarkup(Loc.GetString($"plant-holder-component-nutrient-level-message",
                ("nutritionLevel", (int)component.NutritionLevel)));

            if (plantHolder != null && component.DrawWarnings)
            {
                if (plantHolder.Toxins > 40f)
                    args.PushMarkup(Loc.GetString("plant-holder-component-toxins-high-warning"));

                if (plantHolder.ImproperHeat)
                    args.PushMarkup(Loc.GetString("plant-holder-component-heat-improper-warning"));

                if (plantHolder.ImproperPressure)
                    args.PushMarkup(Loc.GetString("plant-holder-component-pressure-improper-warning"));

                if (plantHolder.MissingGas > 0)
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
                var plantUid = Spawn(seeds.PlantProtoId, _transform.GetMapCoordinates(uid), seeds.PlantData);

                if (!TryComp<PlantDataComponent>(plantUid, out var plantData))
                    return;

                var name = Loc.GetString(plantData.DisplayName);
                var noun = Loc.GetString(plantData.Noun);
                _popup.PopupCursor(Loc.GetString("plant-holder-component-plant-success-message",
                        ("seedName", name),
                        ("seedNoun", noun)),
                    args.User,
                    PopupType.Medium);

                PlantingPlantInTray(uid, plantUid);

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
                UpdateWarnings(ent.AsNullable());
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
            if (TryGetPlant(ent.AsNullable(), out var plantUid))
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
                _plant.RemovePlant(plantUid.Value);
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

                    if (TryGetPlant(ent.AsNullable(), out var plantUid))
                        _plant.ForceUpdateByExternalCause(plantUid.Value);
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
        }
    }

    /// <summary>
    /// Updates the sprite of the tray.
    /// </summary>
    [PublicAPI]
    public void UpdateWarnings(Entity<PlantTrayComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        if (!component.DrawWarnings)
            return;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        if (!TryGetPlant(ent, out var plantUid))
        {
            _appearance.SetData(uid, PlantTrayVisuals.HealthLight, false, app);
            _appearance.SetData(uid, PlantTrayVisuals.AlertLight, false, app);
            _appearance.SetData(uid, PlantTrayVisuals.HarvestLight, false, app);
            return;
        }

        if (!TryComp<PlantHolderComponent>(plantUid, out var plantHolder)
            || !TryComp<PlantComponent>(plantUid, out var plant)
            || !TryComp<PlantHarvestComponent>(plantUid, out var harvest))
            return;

        // TODO: dehardcode those alert levels.
        _appearance.SetData(uid, PlantTrayVisuals.HealthLight, plantHolder.Health <= plant.Endurance / 2f, app);
        _appearance.SetData(uid, PlantTrayVisuals.WaterLight, component.WaterLevel <= 15, app);
        _appearance.SetData(uid, PlantTrayVisuals.NutritionLight, component.NutritionLevel <= 8, app);
        _appearance.SetData(uid,
            PlantTrayVisuals.AlertLight,
            component.WeedLevel >= 5 || plantHolder.PestLevel >= 5 || plantHolder.Toxins >= 40 || plantHolder.ImproperHeat
            || plantHolder.ImproperPressure || plantHolder.MissingGas > 0,
            app);
        _appearance.SetData(uid, PlantTrayVisuals.HarvestLight, harvest is { ReadyForHarvest: true }, app);
    }

    /// <summary>
    /// Updates the reagents of the tray.
    /// </summary>
    [PublicAPI]
    public void UpdateReagents(Entity<PlantTrayComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        if (!_solutionContainer.ResolveSolution(uid, component.SoilSolutionName, ref component.SoilSolution, out var solution))
            return;

        if (!TryGetPlant(ent, out var plantUid))
            return;

        if (!TryComp<PlantHolderComponent>(plantUid, out var plantHolder))
            return;

        if (solution.Volume > 0 && plantHolder.MutationLevel < 25)
        {
            var contents = component.SoilSolution.Value.Comp.Solution.Contents.ToArray();

            foreach (var entry in contents)
            {
                var reagentProto = _prototype.Index<ReagentPrototype>(entry.Reagent.Prototype);
                _entityEffects.ApplyEffects(uid, [.. reagentProto.PlantMetabolisms], entry.Quantity.Float());
                _entityEffects.ApplyEffects(plantUid.Value, [.. reagentProto.PlantMetabolisms], entry.Quantity.Float());
            }

            _solutionContainer.RemoveEachReagent(component.SoilSolution.Value, FixedPoint2.New(1));
        }
    }

    /// <summary>
    /// Planting a plant in a tray.
    /// </summary>
    [PublicAPI]
    public void PlantingPlantInTray(Entity<PlantTrayComponent?> trayEnt, EntityUid plantUid)
    {
        var (trayUid, trayComp) = trayEnt;

        if (!Resolve(trayUid, ref trayComp, false))
            return;

        _plant.PlantingPlant(plantUid);
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
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        component.NutritionLevel += amount;
    }

    /// <summary>
    /// Adjusts the water level of the tray.
    /// </summary>
    [PublicAPI]
    public void AdjustWater(Entity<PlantTrayComponent?> ent, float amount)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        component.WaterLevel += amount;

        // Water dilutes toxins.
        if (TryGetPlant(ent, out var plantUid))
            _plantHolder.AdjustsToxins(plantUid.Value, -amount * 4f);
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
            return false;

        return true;
    }

    /// <summary>
    /// Tries to get the living plant entity in the tray.
    /// </summary>
    [PublicAPI]
    public bool TryGetAlivePlant(
        Entity<PlantTrayComponent?> ent,
        [NotNullWhen(true)] out EntityUid? plant,
        [NotNullWhen(true)] out PlantHolderComponent? holder
    )
    {
        plant = null;
        holder = null;
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!TryGetPlant(ent.Owner, out plant))
            return false;

        if (!TryComp(plant, out holder))
            return false;

        if (holder.Dead)
            return false;

        return true;
    }
}
