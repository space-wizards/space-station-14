using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Botany;
using Content.Shared.Burial.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
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
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Kitchen.Components;
using Content.Shared.Labels.Components;
using System.Linq;

namespace Content.Server.Botany.Systems;

public sealed class PlantHolderSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISerializationManager _copier = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;


    private static readonly ProtoId<TagPrototype> HoeTag = "Hoe";
    private static readonly ProtoId<TagPrototype> PlantSampleTakerTag = "PlantSampleTaker";

    public override void Initialize()
    {
        base.Initialize();
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

        if (!TryComp<PlantTraitsComponent>(uid, out var traits))
            return 1;

        var result = Math.Max(1, (int)(component.Age * traits.GrowthStages / traits.Maturation));
        return result;
    }

    private void OnExamine(Entity<PlantHolderComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var (_, component) = entity;

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

                if (!TryComp<PlantTraitsComponent>(entity, out var traits))
                    return;

                if (component.Health <= traits.Endurance / 2)
                {
                    args.PushMarkup(Loc.GetString(
                        "plant-holder-component-something-already-growing-low-health-message",
                        ("healthState",
                            Loc.GetString(component.Age > traits.Lifespan
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
                if (!_botany.TryGetSeed(seeds, out var seed))
                    return;

                args.Handled = true;
                var name = Loc.GetString(seed.Name);
                var noun = Loc.GetString(seed.Noun);
                _popup.PopupCursor(Loc.GetString("plant-holder-component-plant-success-message",
                    ("seedName", name),
                    ("seedNoun", noun)), args.User, PopupType.Medium);

                plantHolder.Seed = seed.Clone();
                plantHolder.Dead = false;
                plantHolder.Age = 1;
                plantHolder.DrawWarnings = true;

                // Get endurance from seed's PlantTraitsComponent
                var seedTraits = _botany.GetPlantTraits(seed);
                if (seeds.HealthOverride != null)
                {
                    plantHolder.Health = seeds.HealthOverride.Value;
                }
                else if (seedTraits != null)
                {
                    plantHolder.Health = seedTraits.Endurance;
                }
                plantHolder.LastCycle = _gameTiming.CurTime;

                // Ensure no existing growth components before adding new ones
                RemoveAllGrowthComponents(uid);

                // Fill missing components with defaults
                seed.GrowthComponents.EnsureGrowthComponents();

                foreach (var prop in typeof(GrowthComponentsHolder).GetProperties())
                {
                    if (prop.GetValue(seed.GrowthComponents) is PlantGrowthComponent growthComp)
                    {
                        EntityManager.AddComponent(uid, _copier.CreateCopy(growthComp, notNullableOverride: true), overwrite: true);
                    }
                }

                EnsureComp<PlantComponent>(uid);

                if (TryComp<PaperLabelComponent>(args.Used, out var paperLabel))
                {
                    _itemSlots.TryEjectToHands(args.Used, paperLabel.LabelSlot, args.User);
                }
                QueueDel(args.Used);

                CheckLevelSanity(uid, plantHolder);
                UpdateSprite(uid, plantHolder);

                if (seed.PlantLogImpact != null)
                    _adminLogger.Add(LogType.Botany, seed.PlantLogImpact.Value, $"{ToPrettyString(args.User):player} planted  {Loc.GetString(seed.Name):seed} at Pos:{Transform(uid).Coordinates}.");

                return;
            }

            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("plant-holder-component-already-seeded-message",
                ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
            return;
        }

        if (_tagSystem.HasTag(args.Used, HoeTag))
        {
            args.Handled = true;
            if (plantHolder.WeedLevel > 0)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-remove-weeds-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
                _popup.PopupEntity(Loc.GetString("plant-holder-component-remove-weeds-others-message",
                    ("otherName", Comp<MetaDataComponent>(args.User).EntityName)), uid, Filter.PvsExcept(args.User), true);
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
                _popup.PopupCursor(Loc.GetString("plant-holder-component-remove-plant-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
                _popup.PopupEntity(Loc.GetString("plant-holder-component-remove-plant-others-message",
                    ("name", Comp<MetaDataComponent>(args.User).EntityName)), uid, Filter.PvsExcept(args.User), true);
                RemovePlant(uid, plantHolder);
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-no-plant-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User);
            }

            return;
        }

        if (_tagSystem.HasTag(args.Used, PlantSampleTakerTag))
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
            if (plantHolder.Harvest)
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
                    ("seedName", displayName)), args.User);

                if (_random.Prob(0.3f))
                    plantHolder.Sampled = true;

                CheckLevelSanity(uid, plantHolder);
                ForceUpdateByExternalCause(uid, plantHolder);
            }

            return;
        }

        if (TryComp<ProduceComponent>(args.Used, out var produce))
        {
            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("plant-holder-component-compost-message",
                ("owner", uid),
                ("usingItem", args.Used)), args.User, PopupType.Medium);
            _popup.PopupEntity(Loc.GetString("plant-holder-component-compost-others-message",
                ("user", Identity.Entity(args.User, EntityManager)),
                ("usingItem", args.Used),
                ("owner", uid)), uid, Filter.PvsExcept(args.User), true);

            if (_solutionContainerSystem.TryGetSolution(args.Used, produce.SolutionName, out var soln2, out var solution2))
            {
                if (_solutionContainerSystem.ResolveSolution(uid, plantHolder.SoilSolutionName, ref plantHolder.SoilSolution, out var solution1))
                {
                    // We try to fit as much of the composted plant's contained solution into the hydroponics tray as we can,
                    // since the plant will be consumed anyway.

                    var fillAmount = FixedPoint2.Min(solution2.Volume, solution1.AvailableVolume);
                    _solutionContainerSystem.TryAddSolution(plantHolder.SoilSolution.Value, _solutionContainerSystem.SplitSolution(soln2.Value, fillAmount));

                    ForceUpdateByExternalCause(uid, plantHolder);
                }
            }
            var seed = produce.Seed;
            if (seed != null)
            {
                var seedTraits = _botany.GetPlantTraits(seed);
                if (seedTraits != null)
                {
                    var nutrientBonus = seedTraits.Potency / 2.5f;
                    AdjustNutrient(uid, nutrientBonus, plantHolder);
                }
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
        else if (curTime < (component.LastCycle + component.CycleDelay))
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);
            return;
        }

        component.LastCycle = curTime;

        if (component.Seed != null && !component.Dead)
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
        CheckLevelSanity(uid, component);

        // Synchronize harvest status between PlantHolderComponent and HarvestComponent
        if (TryComp<HarvestComponent>(uid, out var harvestComp))
        {
            component.Harvest = harvestComp.ReadyForHarvest;
        }

        if (component.UpdateSpriteAfterUpdate)
            UpdateSprite(uid, component);
    }

    /// <summary>
    /// Ensures all plant holder levels are within valid ranges.
    /// TODO: Move this validation logic to individual growth components
    /// </summary>
    public void CheckLevelSanity(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Seed != null && TryComp<PlantTraitsComponent>(uid, out var traits))
            component.Health = MathHelper.Clamp(component.Health, 0, traits.Endurance);
        else
        {
            component.Health = 0f;
            component.Dead = false;
        }

        component.MutationLevel = MathHelper.Clamp(component.MutationLevel, 0f, 100f);
        component.NutritionLevel = MathHelper.Clamp(component.NutritionLevel, 0f, 100f);
        component.WaterLevel = MathHelper.Clamp(component.WaterLevel, 0f, 100f);
        component.PestLevel = MathHelper.Clamp(component.PestLevel, 0f, 10f);
        component.WeedLevel = MathHelper.Clamp(component.WeedLevel, 0f, 10f);
        component.Toxins = MathHelper.Clamp(component.Toxins, 0f, 100f);
        component.YieldMod = MathHelper.Clamp(component.YieldMod, 0, 2);
        component.MutationMod = MathHelper.Clamp(component.MutationMod, 0f, 3f);
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

    public void Die(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Dead = true;
        component.Harvest = false;
        component.MutationLevel = 0;
        component.YieldMod = 1;
        component.MutationMod = 1;
        component.ImproperPressure = false;
        component.WeedLevel += 1;
        component.PestLevel = 0;
        UpdateSprite(uid, component);
    }

    public void RemovePlant(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.Seed == null)
            return;

        // Remove all growth components before planting new seed
        RemoveAllGrowthComponents(uid);

        component.YieldMod = 1;
        component.MutationMod = 1;
        component.PestLevel = 0;
        component.Seed = null;
        component.Dead = false;
        component.Age = 0;
        component.LastProduce = 0;
        component.Sampled = false;
        component.Harvest = false;
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

        if (!_solutionContainerSystem.ResolveSolution(uid, component.SoilSolutionName, ref component.SoilSolution, out var solution))
            return;

        if (solution.Volume > 0 && component.MutationLevel < 25)
        {
            var amt = FixedPoint2.New(1);
            foreach (var entry in _solutionContainerSystem.RemoveEachReagent(component.SoilSolution.Value, amt))
            {
                var reagentProto = _prototype.Index<ReagentPrototype>(entry.Reagent.Prototype);
                reagentProto.ReactionPlant(uid, entry, solution);
            }
        }

        CheckLevelSanity(uid, component);
    }

    private void Mutate(EntityUid uid, float severity, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Seed != null)
        {
            EnsureUniqueSeed(uid, component);
            _mutation.MutateSeed(uid, ref component.Seed, severity);
        }
    }

    public void UpdateSprite(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.UpdateSpriteAfterUpdate = false;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        TryComp<PlantTraitsComponent>(uid, out var traits);

        if (traits == null)
            return;

        if (component.Seed != null)
        {
            if (component.DrawWarnings)
            {
                _appearance.SetData(uid, PlantHolderVisuals.HealthLight, component.Health <= traits.Endurance / 2f);
            }

            if (component.Dead)
            {
                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, "dead", app);
            }
            else if (component.Harvest)
            {
                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, "harvest", app);
            }
            else
            {
                if (component.Age < traits.Maturation)
                {
                    var growthStage = GetCurrentGrowthStage((uid, component));

                    _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                    _appearance.SetData(uid, PlantHolderVisuals.PlantState, $"stage-{growthStage}", app);
                    component.LastProduce = component.Age;
                }
                else
                {
                    _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                    _appearance.SetData(uid, PlantHolderVisuals.PlantState, $"stage-{traits.GrowthStages}", app);
                }
            }
        }
        else
        {
            _appearance.SetData(uid, PlantHolderVisuals.PlantState, "", app);
            _appearance.SetData(uid, PlantHolderVisuals.HealthLight, false, app);
        }

        if (!component.DrawWarnings)
            return;

        _appearance.SetData(uid, PlantHolderVisuals.WaterLight, component.WaterLevel <= 15, app);
        _appearance.SetData(uid, PlantHolderVisuals.NutritionLight, component.NutritionLevel <= 8, app);
        _appearance.SetData(uid, PlantHolderVisuals.AlertLight,
            component.WeedLevel >= 5 || component.PestLevel >= 5 || component.Toxins >= 40 || component.ImproperHeat ||
            component.ImproperPressure || component.MissingGas > 0, app);
        _appearance.SetData(uid, PlantHolderVisuals.HarvestLight, component.Harvest, app);
    }

    /// <summary>
    /// Check if the currently contained seed is unique. If it is not, clone it so that we have a unique seed.
    /// Necessary to avoid modifying global seeds.
    /// </summary>
    public void EnsureUniqueSeed(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Seed is { Unique: false })
            component.Seed = component.Seed.Clone();
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
    /// </summary>
    private void RemoveAllGrowthComponents(EntityUid uid)
    {
        foreach (var comp in EntityManager.GetComponents<PlantGrowthComponent>(uid))
        {
            RemComp(uid, comp);
        }
    }
}
