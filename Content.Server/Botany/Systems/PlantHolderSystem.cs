using Content.Server.Botany.Components;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Botany;
using Content.Shared.Burial.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Systems;

public sealed class PlantHolderSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    public const float HydroponicsSpeedMultiplier = 1f;
    public const float HydroponicsConsumptionMultiplier = 2f;

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

    private void OnExamine(Entity<PlantHolderComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(PlantHolderComponent)))
        {

            var (_, component) = entity;
            SeedData? seed = null;
            var hasPlant = GetPlant(component.PlantUid, out var plant);
            if (hasPlant)
                seed = plant.Comp.Seed;

            if (!hasPlant)
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));
            }
            else if (!plant.Comp.Dead && seed != null)
            {
                var displayName = Loc.GetString(seed.DisplayName);
                args.PushMarkup(Loc.GetString("plant-holder-component-something-already-growing-message",
                    ("seedName", displayName),
                    ("toBeForm", displayName.EndsWith('s') ? "are" : "is")));

                if (plant.Comp.Health <= seed.Endurance / 2)
                {
                    args.PushMarkup(Loc.GetString(
                        "plant-holder-component-something-already-growing-low-health-message",
                        ("healthState",
                            Loc.GetString(plant.Comp.Age > seed.Lifespan
                                ? "plant-holder-component-plant-old-adjective"
                                : "plant-holder-component-plant-unhealthy-adjective"))));
                }
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

    /// <summary>
    /// Gets the PlantComponent on an Plant entity.
    /// </summary>
    private bool GetPlant(EntityUid? uid, out Entity<PlantComponent> plant)
    {
        if (uid != null)
        {
            if (TryComp<PlantComponent>(uid, out var plantComp) && plantComp != null)
            {
                plant = (uid.Value, plantComp);
                return true;
            }
        }
        plant = (EntityUid.Invalid, new PlantComponent());
        return false;
    }

    private void OnInteractUsing(Entity<PlantHolderComponent> entity, ref InteractUsingEvent args)
    {
        var (uid, component) = entity;
        var hasPlant = GetPlant(component.PlantUid, out _);

        if (TryComp(args.Used, out SeedComponent? seeds))
        {
            if (seeds == null)
                return;
            if (!hasPlant)
            {
                if (!_botany.TryGetSeed(seeds, out var seed))
                    return;


                if (entity.Comp.PlantUid == null)
                {
                    seed = seed.Clone();
                    var name = Loc.GetString(seed.Name);
                    var noun = Loc.GetString(seed.Noun);
                    _popup.PopupCursor(Loc.GetString("plant-holder-component-plant-success-message",
                        ("seedName", name),
                        ("seedNoun", noun)), args.User, PopupType.Medium);

                    var newPlant = Spawn("BasePlant", Transform(uid).Coordinates);
                    _meta.SetEntityName(newPlant, Loc.GetString(seed.DisplayName));
                    var plantcomp = Comp<PlantComponent>(newPlant);
                    plantcomp.PlantHolderUid = uid;
                    plantcomp.Seed = seed;
                    entity.Comp.PlantUid = newPlant;

                    var xform = Transform(newPlant);
                    _transform.SetParent(newPlant, xform, uid);
                    _plant.UpdateSprite(newPlant);

                    foreach (var mutation in seed.Mutations)
                    {
                        if (mutation.AppliesToPlant)
                        {
                            var effectArgs = new EntityEffectBaseArgs(newPlant, EntityManager);
                            mutation.Effect.Effect(effectArgs);
                        }
                    }

                    if (seeds.HealthOverride != null)
                    {
                        plantcomp.Health = seeds.HealthOverride.Value;
                    }
                    else
                    {
                        plantcomp.Health = seed.Endurance;
                    }

                    component.LastCycle = _gameTiming.CurTime;

                    QueueDel(args.Used);
                    UpdateSprite(uid, component);

                }
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-already-seeded-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
                return;
            }

            return;
        }

        if (_tagSystem.HasTag(args.Used, "Hoe"))
        {
            args.Handled = true;
            if (component.WeedLevel > 0)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-remove-weeds-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
                _popup.PopupEntity(Loc.GetString("plant-holder-component-remove-weeds-others-message",
                    ("otherName", Comp<MetaDataComponent>(args.User).EntityName)), uid, Filter.PvsExcept(args.User), true);
                component.WeedLevel = 0;
                UpdateSprite(uid, component);
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-no-weeds-message"), args.User);
            }

            return;
        }

        if (HasComp<ShovelComponent>(args.Used))
        {
            if (hasPlant == true)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-remove-plant-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
                _popup.PopupEntity(Loc.GetString("plant-holder-component-remove-plant-others-message",
                    ("name", Comp<MetaDataComponent>(args.User).EntityName)), uid, Filter.PvsExcept(args.User), true);
                RemovePlant(uid, component);
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-no-plant-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User);
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
                if (_solutionContainerSystem.ResolveSolution(uid, component.SoilSolutionName, ref component.SoilSolution, out var solution1))
                {
                    // We try to fit as much of the composted plant's contained solution into the hydroponics tray as we can,
                    // since the plant will be consumed anyway.

                    var fillAmount = FixedPoint2.Min(solution2.Volume, solution1.AvailableVolume);
                    _solutionContainerSystem.TryAddSolution(component.SoilSolution.Value, _solutionContainerSystem.SplitSolution(soln2.Value, fillAmount));

                    ForceUpdateByExternalCause(uid, component);
                }
            }
            var seed = produce.Seed;
            if (seed != null)
            {
                var nutrientBonus = seed.Potency / 2.5f;
                AdjustNutrient(uid, nutrientBonus, component);
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

        if (component.ForceUpdate)
            component.ForceUpdate = false;
        else if (curTime < (component.LastCycle + component.CycleDelay))
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);
            return;
        }

        component.LastCycle = curTime;

        SeedData? seed = null;
        var hasPlant = TryComp<PlantComponent>(component.PlantUid, out var plant);
        if (hasPlant)
        {
            TryComp<SeedComponent>(component.PlantUid, out var seedComp);
            if (seedComp != null && seedComp.Seed != null)
                seed = seedComp.Seed;
        }
        else // Plant was removed in a non-standard way, like admin direct delete.
        {
            component.PlantUid = null;
        }

        // Weeds like water and nutrients! They may appear even if there's not a seed planted.
        if (component.WaterLevel > 10 && component.NutritionLevel > 5)
        {
            var chance = 0f;
            if (seed == null)
                chance = 0.05f;
            else if (seed.TurnIntoKudzu)
                chance = 1f;
            else
                chance = 0.01f;

            if (_random.Prob(chance))
                component.WeedLevel = Math.Clamp(component.WeedLevel + (1 + HydroponicsSpeedMultiplier * component.WeedCoefficient), 0, 10);

            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        if (plant != null && seed != null && seed.TurnIntoKudzu
            && component.WeedLevel >= seed.WeedHighLevelThreshold)
        {
            Spawn(seed.KudzuPrototype, Transform(uid).Coordinates.SnapToGrid(EntityManager));
            seed.TurnIntoKudzu = false;
            plant.Health = 0;
        }

        // If we have no seed planted, or the plant is dead, stop processing here.
        if (seed == null || plant == null || plant.Dead)
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);

            return;
        }

        // There's a small chance the pest population increases.
        // Can only happen when there's a live seed planted.
        if (_random.Prob(0.01f))
        {
            component.PestLevel = Math.Clamp(component.PestLevel + 0.5f * HydroponicsSpeedMultiplier, 0, 10);
            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        if (component.UpdateSpriteAfterUpdate)
            UpdateSprite(uid, component);
    }

    public void RemovePlant(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.PestLevel = 0;
        component.ImproperPressure = false;
        component.ImproperHeat = false;
        if (component.PlantUid != null)
        {
            GetPlant(component.PlantUid.Value, out var plant);
            _plant.RemovePlant(plant);
            component.PlantUid = null;
        }

        UpdateSprite(uid, component);
    }

    public void AdjustNutrient(EntityUid uid, float amount, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.NutritionLevel = Math.Clamp(component.NutritionLevel + amount, 0, 100);
    }

    public void AdjustWater(EntityUid uid, float amount, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.WaterLevel = Math.Clamp(component.WaterLevel + amount, 0, 100);

        // Water dilutes toxins.
        if (amount > 0)
        {
            component.Toxins = Math.Clamp(component.Toxins - amount * 4f, 0, 100);
        }
    }

    public void UpdateReagents(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_solutionContainerSystem.ResolveSolution(uid, component.SoilSolutionName, ref component.SoilSolution, out var solution))
            return;

        if (solution.Volume > 0)
        {
            var amt = FixedPoint2.New(1);
            foreach (var entry in _solutionContainerSystem.RemoveEachReagent(component.SoilSolution.Value, amt))
            {
                var reagentProto = _prototype.Index<ReagentPrototype>(entry.Reagent.Prototype);
                reagentProto.ReactionPlant(uid, entry, solution);
            }
        }
    }

    public void UpdateSprite(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.DrawWarnings)
            return;

        component.UpdateSpriteAfterUpdate = false;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        SeedData? seed = null;

        if (component.PlantUid != null)
        {
            var hasPlant = GetPlant(component.PlantUid.Value, out var plantEnt);

            if (hasPlant && plantEnt.Comp != null)
            {
                seed = plantEnt.Comp.Seed;
                _appearance.SetData(uid, PlantHolderVisuals.HarvestLight, plantEnt.Comp.Harvest, app);
            }
            else
            {
                _appearance.SetData(uid, PlantHolderVisuals.HarvestLight, false, app);
            }

            if (seed == null || plantEnt.Comp == null)
            {
                _appearance.SetData(uid, PlantHolderVisuals.HealthLight, false, app);
            }
            else
            {
                _appearance.SetData(uid, PlantHolderVisuals.HealthLight, plantEnt.Comp.Health <= seed.Endurance / 2f);
            }
        }
        else
        {
            _appearance.SetData(uid, PlantHolderVisuals.HarvestLight, false, app);
            _appearance.SetData(uid, PlantHolderVisuals.HealthLight, false, app);
        }

        _appearance.SetData(uid, PlantHolderVisuals.WaterLight, component.WaterLevel <= 15, app);
        _appearance.SetData(uid, PlantHolderVisuals.NutritionLight, component.NutritionLevel <= 8, app);
        _appearance.SetData(uid, PlantHolderVisuals.AlertLight,
            component.WeedLevel >= 5 || component.PestLevel >= 5 || component.Toxins >= 40 || component.ImproperHeat ||
            component.ImproperPressure || component.MissingGas > 0, app);
    }


    public void ForceUpdateByExternalCause(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.PlantUid != null && GetPlant(component.PlantUid.Value, out var plant))
        {
            plant.Comp.SkipAging++; // We're forcing an update cycle, so one age hasn't passed.
            _plant.Update(component.PlantUid.Value, plant.Comp);
        }
        else
        {
            component.PlantUid = null;
        }
        component.ForceUpdate = true;
        Update(uid, component);
    }
}
