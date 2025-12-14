using System.Linq;
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
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Systems;

public sealed class PlantHolderSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISerializationManager _copier = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> HoeTag = "Hoe";
    private static readonly ProtoId<TagPrototype> PlantSampleTakerTag = "PlantSampleTaker";

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantHolderComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<PlantHolderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlantHolderComponent, SolutionTransferredEvent>(OnSolutionTransferred);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlantHolderComponent>();
        while (query.MoveNext(out var uid, out var plantHolder))
        {
            if (plantHolder.NextUpdate > _gameTiming.CurTime)
                continue;

            plantHolder.NextUpdate = _gameTiming.CurTime + plantHolder.UpdateDelay;

            Update(uid, plantHolder);
        }
    }

    private int GetCurrentGrowthStage(Entity<PlantHolderComponent> entity)
    {
        var (uid, component) = entity;

        if (component.Seed == null)
            return 0;

        if (!TryComp<PlantComponent>(uid, out var plant))
            return 0;

        var result = Math.Max(1, (int)(component.Age * plant.GrowthStages / plant.Maturation));
        return result;
    }

    private void OnExamine(Entity<PlantHolderComponent> entity, ref ExaminedEvent args)
    {
        var (uid, component) = entity;

        PlantComponent? plant = null;
        PlantTraitsComponent? traits = null;
        if (!Resolve(uid, ref plant, ref traits))
            return;

        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(PlantHolderComponent)))
        {
            if (component.Seed == null)
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));
            }
            else if (!component.Dead)
            {
                var displayName = Loc.GetString(component.Seed.DisplayName);
                args.PushMarkup(Loc.GetString("plant-holder-component-something-already-growing-message",
                    ("seedName", displayName),
                    ("toBeForm", displayName.EndsWith('s') ? "are" : "is")));

                if (component.Health <= plant.Endurance / 2)
                {
                    args.PushMarkup(Loc.GetString(
                        "plant-holder-component-something-already-growing-low-health-message",
                        ("healthState",
                            Loc.GetString(component.Age > plant.Lifespan
                                ? "plant-holder-component-plant-old-adjective"
                                : "plant-holder-component-plant-unhealthy-adjective"))));
                }

                // For future reference, mutations should only appear on examine if they apply to a plant, not to produce.
                if (traits.Ligneous)
                    args.PushMarkup(Loc.GetString("mutation-plant-ligneous"));

                if (traits.TurnIntoKudzu)
                    args.PushMarkup(Loc.GetString("mutation-plant-kudzu"));

                if (traits.CanScream)
                    args.PushMarkup(Loc.GetString("mutation-plant-scream"));

                if (!traits.Viable)
                    args.PushMarkup(Loc.GetString("mutation-plant-unviable"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-dead-plant-matter-message"));
            }

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

    private void OnInteractUsing(Entity<PlantHolderComponent> entity, ref InteractUsingEvent args)
    {
        var (uid, plantHolder) = entity;

        if (TryComp(args.Used, out SeedComponent? seeds))
        {
            if (plantHolder.Seed == null)
            {
                if (!_botany.TryGetSeed(seeds, out var seed) || !BotanySystem.TryGetPlant(seed, out var seedPlant))
                    return;

                args.Handled = true;
                var name = Loc.GetString(seed.Name);
                var noun = Loc.GetString(seed.Noun);
                _popup.PopupCursor(Loc.GetString("plant-holder-component-plant-success-message",
                    ("seedName", name),
                    ("seedNoun", noun)),
                    args.User,
                    PopupType.Medium);

                plantHolder.Seed = seed.Clone();
                plantHolder.Dead = false;
                plantHolder.Age = 1;

                plantHolder.Health = seeds.HealthOverride ?? seedPlant.Endurance;

                plantHolder.LastCycle = _gameTiming.CurTime;

                // Ensure no existing growth components before adding new ones
                RemoveAllGrowthComponents(uid);

                // Fill missing components with defaults
                seed.GrowthComponents.EnsureGrowthComponents();

                foreach (var prop in GrowthComponentsHolder.ComponentGetters)
                {
                    if (prop.GetValue(seed.GrowthComponents) is Component growthComp)
                    {
                        EntityManager.AddComponent(uid, _copier.CreateCopy(growthComp, notNullableOverride: true), overwrite: true);
                    }
                }

                if (TryComp<PaperLabelComponent>(args.Used, out var paperLabel))
                {
                    _itemSlots.TryEjectToHands(args.Used, paperLabel.LabelSlot, args.User);
                }
                QueueDel(args.Used);

                UpdateSprite(uid, plantHolder);

                if (seed.PlantLogImpact != null)
                    _adminLogger.Add(LogType.Botany, seed.PlantLogImpact.Value, $"{ToPrettyString(args.User):player} planted  {Loc.GetString(seed.Name):seed} at Pos:{Transform(uid).Coordinates}.");

                return;
            }

            args.Handled = true;
            _popup.PopupCursor(
                Loc.GetString("plant-holder-component-already-seeded-message", ("name", MetaData(uid).EntityName)),
                args.User,
                PopupType.Medium);
            return;
        }

        if (_tag.HasTag(args.Used, HoeTag))
        {
            args.Handled = true;
            if (plantHolder.WeedLevel > 0)
            {
                _popup.PopupCursor(
                    Loc.GetString("plant-holder-component-remove-weeds-message", ("name", MetaData(uid).EntityName)),
                    args.User,
                    PopupType.Medium);
                _popup.PopupEntity(
                    Loc.GetString("plant-holder-component-remove-weeds-others-message", ("otherName", MetaData(args.User).EntityName)),
                    uid,
                    Filter.PvsExcept(args.User),
                    true);
                plantHolder.WeedLevel = 0;
                UpdateSprite(uid, plantHolder);
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-no-weeds-message"), args.User);
            }

            return;
        }

        if (HasComp<ShovelComponent>(args.Used))
        {
            args.Handled = true;
            if (plantHolder.Seed != null)
            {
                _popup.PopupCursor(
                    Loc.GetString("plant-holder-component-remove-plant-message", ("name", MetaData(uid).EntityName)),
                    args.User,
                    PopupType.Medium);
                _popup.PopupEntity(
                    Loc.GetString("plant-holder-component-remove-plant-others-message", ("name", MetaData(args.User).EntityName)),
                    uid,
                    Filter.PvsExcept(args.User),
                    true);
                RemovePlant(uid, plantHolder);
            }
            else
            {
                _popup.PopupCursor(
                    Loc.GetString("plant-holder-component-no-plant-message", ("name", MetaData(uid).EntityName)),
                    args.User);
            }

            return;
        }

        if (_tag.HasTag(args.Used, PlantSampleTakerTag))
        {
            args.Handled = true;
            if (plantHolder.Seed == null)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-nothing-to-sample-message"), args.User);
                return;
            }

            if (plantHolder.Sampled)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-already-sampled-message"), args.User);
                return;
            }

            if (plantHolder.Dead)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-dead-plant-message"), args.User);
                return;
            }

            if (GetCurrentGrowthStage(entity) <= 1)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-early-sample-message"), args.User);
                return;
            }

            plantHolder.Health -= _random.Next(3, 5) * 10;

            float? healthOverride;
            if (TryComp<PlantHarvestComponent>(uid, out var harvest) && harvest.ReadyForHarvest)
            {
                healthOverride = null;
            }
            else
            {
                healthOverride = plantHolder.Health;
            }

            var packetSeed = plantHolder.Seed;
            if (packetSeed != null)
            {
                // Copy growth components from the plant to the seed before creating seed packet
                var holder = new GrowthComponentsHolder();

                foreach (var prop in typeof(GrowthComponentsHolder).GetProperties())
                {
                    if (EntityManager.TryGetComponent(uid, prop.PropertyType, out var growthComponent))
                    {
                        var copiedComponent = _copier.CreateCopy((Component)growthComponent, notNullableOverride: true);
                        prop.SetValue(holder, copiedComponent);
                    }
                }

                packetSeed.GrowthComponents = holder;

                var seed = _botany.SpawnSeedPacket(packetSeed, Transform(args.User).Coordinates, args.User, healthOverride);
                _randomHelper.RandomOffset(seed, 0.25f);
                var displayName = Loc.GetString(plantHolder.Seed.DisplayName);
                _popup.PopupCursor(Loc.GetString("plant-holder-component-take-sample-message",
                    ("seedName", displayName)),
                    args.User);

                if (_random.Prob(0.3f))
                    plantHolder.Sampled = true;

                ForceUpdateByExternalCause(uid, plantHolder);
            }

            return;
        }

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
                if (_solutionContainer.ResolveSolution(uid, plantHolder.SoilSolutionName, ref plantHolder.SoilSolution, out var solution1))
                {
                    // We try to fit as much of the composted plant's contained solution into the hydroponics tray as we can,
                    // since the plant will be consumed anyway.

                    var fillAmount = FixedPoint2.Min(solution2.Volume, solution1.AvailableVolume);
                    _solutionContainer.TryAddSolution(plantHolder.SoilSolution.Value, _solutionContainer.SplitSolution(soln2.Value, fillAmount));

                    ForceUpdateByExternalCause(uid, plantHolder);
                }
            }
            var seed = produce.Seed;
            if (seed != null && BotanySystem.TryGetPlant(seed, out var seedPlant))
            {
                var nutrientBonus = seedPlant.Potency / 2.5f;
                AdjustNutrient(uid, nutrientBonus, plantHolder);
            }
            QueueDel(args.Used);
        }
    }

    private void OnSolutionTransferred(Entity<PlantHolderComponent> ent, ref SolutionTransferredEvent args)
    {
        _audio.PlayPvs(ent.Comp.WateringSound, ent.Owner);
    }

    public void Update(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        UpdateReagents(uid, component);

        var curTime = _gameTiming.CurTime;

        // ForceUpdate is used for external triggers like swabbing
        if (component.ForceUpdate)
            component.ForceUpdate = false;
        else if (curTime < component.LastCycle + component.CycleDelay)
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);

            return;
        }

        component.LastCycle = curTime;

        if (component is { Seed: not null, Dead: false })
        {
            var plantGrow = new OnPlantGrowEvent();
            RaiseLocalEvent(uid, ref plantGrow);
        }

        // Process mutations. All plants can mutate, so this stays here.
        if (component.MutationLevel > 0)
        {
            Mutate(uid, Math.Min(component.MutationLevel, 25), component);
            component.UpdateSpriteAfterUpdate = true;
            component.MutationLevel = 0;
        }

        // If we have no seed planted, or the plant is dead, stop processing here.
        if (component.Seed == null || component.Dead)
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);

            return;
        }

        CheckHealth(uid, component);

        if (component.UpdateSpriteAfterUpdate)
            UpdateSprite(uid, component);
    }

    public void CheckHealth(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Health <= 0)
        {
            Die(uid, component);
        }
    }

    public void Die(EntityUid uid, PlantHolderComponent component)
    {
        PlantHarvestComponent? harvest = null;
        if (!Resolve(uid, ref harvest))
            return;

        component.Dead = true;
        harvest.ReadyForHarvest = false;
        component.MutationLevel = 0;
        component.YieldMod = 1;
        component.MutationMod = 1;
        component.ImproperPressure = false;
        component.WeedLevel += 1 * BasicGrowthSystem.HydroponicsSpeedMultiplier;
        component.PestLevel = 0;

        UpdateSprite(uid, component);
    }

    public void RemovePlant(EntityUid uid, PlantHolderComponent? component = null)
    {
        PlantHarvestComponent? harvest = null;
        if (!Resolve(uid, ref component, ref harvest))
            return;

        if (component.Seed == null)
            return;

        // Remove all growth components before planting new seed
        RemoveAllGrowthComponents(uid);

        component.YieldMod = 1;
        component.MutationMod = 1;
        component.PestLevel = 0;
        component.Seed = null;
        component.Dead = false;
        component.Age = 0;
        harvest.LastHarvest = 0;
        component.Sampled = false;
        harvest.ReadyForHarvest = false;
        component.ImproperPressure = false;
        component.ImproperHeat = false;

        UpdateSprite(uid, component);
    }

    public void AdjustNutrient(EntityUid uid, float amount, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.NutritionLevel += amount;
    }

    public void AdjustWater(EntityUid uid, float amount, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.WaterLevel += amount;

        // Water dilutes toxins.
        if (amount > 0)
        {
            component.Toxins -= amount * 4f;
        }
    }

    public void UpdateReagents(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_solutionContainer.ResolveSolution(uid, component.SoilSolutionName, ref component.SoilSolution, out var solution))
            return;

        if (solution.Volume > 0 && component.MutationLevel < 25)
        {
            foreach (var entry in component.SoilSolution.Value.Comp.Solution.Contents)
            {
                var reagentProto = _prototype.Index<ReagentPrototype>(entry.Reagent.Prototype);
                _entityEffects.ApplyEffects(uid, [.. reagentProto.PlantMetabolisms], entry.Quantity.Float());
            }

            _solutionContainer.RemoveEachReagent(component.SoilSolution.Value, FixedPoint2.New(1));
        }
    }

    private void Mutate(EntityUid uid, float severity, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Seed != null)
            _mutation.MutateSeed(uid, ref component.Seed, severity);
    }

    public void UpdateSprite(EntityUid uid, PlantHolderComponent? component = null)
    {
        PlantHarvestComponent? harvest = null;
        PlantComponent? plant = null;
        Resolve(uid, ref harvest, ref plant, ref component, false);

        if (!TryComp<AppearanceComponent>(uid, out var app)
            || component == null)
            return;


        component.UpdateSpriteAfterUpdate = false;

        // If no seed, clear visuals regardless of traits.
        if (component.Seed == null)
        {
            _appearance.SetData(uid, PlantHolderVisuals.PlantState, string.Empty, app);
            _appearance.SetData(uid, PlantHolderVisuals.HealthLight, false, app);
            _appearance.SetData(uid, PlantHolderVisuals.HarvestLight, false, app);
        }
        else if (harvest != null && plant != null)
        {
            if (component.DrawWarnings)
            {
                _appearance.SetData(uid, PlantHolderVisuals.HealthLight, component.Health <= plant.Endurance / 2f);
            }

            if (component.Dead)
            {
                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, "dead", app);
            }
            else if (harvest.ReadyForHarvest)
            {
                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, "harvest", app);
            }
            else if (component.Age < plant.Maturation)
            {
                var growthStage = GetCurrentGrowthStage((uid, component));

                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, $"stage-{growthStage}", app);
                harvest.LastHarvest = component.Age;
            }
            else
            {
                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, $"stage-{plant.GrowthStages}", app);
            }
        }

        if (!component.DrawWarnings)
            return;

        // TODO: dehardcode those alert levels.
        // Not obvious where they go, as plant holder have alerts, sure, but some plants could have
        // very different consumption rates so it would make sense to have different thresholds
        _appearance.SetData(uid, PlantHolderVisuals.WaterLight, component.WaterLevel <= 15, app);
        _appearance.SetData(uid, PlantHolderVisuals.NutritionLight, component.NutritionLevel <= 8, app);
        _appearance.SetData(uid,
            PlantHolderVisuals.AlertLight,
            component.WeedLevel >= 5 || component.PestLevel >= 5 || component.Toxins >= 40 || component.ImproperHeat
            || component.ImproperPressure || component.MissingGas > 0,
            app);
        _appearance.SetData(uid, PlantHolderVisuals.HarvestLight, harvest is { ReadyForHarvest: true }, app);
    }

    public void ForceUpdateByExternalCause(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.SkipAging++; // We're forcing an update cycle, so one age hasn't passed.
        component.ForceUpdate = true;
        Update(uid, component);
    }

    /// <summary>
    /// Removes all growth-related components from a plant.
    /// TODO: Delete after plants transition to entities
    /// </summary>
    private void RemoveAllGrowthComponents(EntityUid uid)
    {
        foreach (var comp in EntityManager.GetComponents(uid))
        {
            if (GrowthComponentsHolder.GrowthComponentTypes.Contains(comp.GetType()))
            {
                RemComp(uid, comp);
            }
        }
    }
}
