using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Hands.Components;
using Content.Server.Kitchen.Components;
using Content.Shared.Interaction;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Content.Shared.FixedPoint;
using Content.Shared.Botany;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems
{
    public sealed class PlantHolderSystem : EntitySystem
    {
        [Dependency] private readonly BotanySystem _botanySystem = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly MutationSystem _mutation = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

        public const float HydroponicsSpeedMultiplier = 1f;
        public const float HydroponicsConsumptionMultiplier = 4f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlantHolderComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<PlantHolderComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PlantHolderComponent, InteractHandEvent>(OnInteractHand);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var plantHolder in EntityQuery<PlantHolderComponent>())
            {
                if (plantHolder.NextUpdate > _gameTiming.CurTime)
                    continue;
                plantHolder.NextUpdate = _gameTiming.CurTime + plantHolder.UpdateDelay;

                Update(plantHolder.Owner, plantHolder);
            }
        }

        private void OnExamine(EntityUid uid, PlantHolderComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

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

                if (component.Health <= component.Seed.Endurance / 2)
                {
                    args.PushMarkup(Loc.GetString(
                        "plant-holder-component-something-already-growing-low-health-message",
                        ("healthState",
                            Loc.GetString(component.Age > component.Seed.Lifespan
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
                ("waterLevel", (int) component.WaterLevel)));
            args.PushMarkup(Loc.GetString($"plant-holder-component-nutrient-level-message",
                ("nutritionLevel", (int) component.NutritionLevel)));

            if (component.DrawWarnings)
            {
                if (component.Toxins > 40f)
                    args.PushMarkup(Loc.GetString("plant-holder-component-toxins-high-warning"));

                if (component.ImproperLight)
                    args.PushMarkup(Loc.GetString("plant-holder-component-light-improper-warning"));

                if (component.ImproperHeat)
                    args.PushMarkup(Loc.GetString("plant-holder-component-heat-improper-warning"));

                if (component.ImproperPressure)
                    args.PushMarkup(Loc.GetString("plant-holder-component-pressure-improper-warning"));

                if (component.MissingGas > 0)
                    args.PushMarkup(Loc.GetString("plant-holder-component-gas-missing-warning"));
            }
        }

        private void OnInteractUsing(EntityUid uid, PlantHolderComponent component, InteractUsingEvent args)
        {
            if (TryComp(args.Used, out SeedComponent? seeds))
            {
                if (component.Seed == null)
                {
                    if (!_botanySystem.TryGetSeed(seeds, out var seed))
                        return ;

                    var name = Loc.GetString(seed.Name);
                    var noun = Loc.GetString(seed.Noun);
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-plant-success-message",
                        ("seedName", name),
                        ("seedNoun", noun)), args.User, PopupType.Medium);

                    component.Seed = seed;
                    component.Dead = false;
                    component.Age = 1;
                    component.Health = component.Seed.Endurance;
                    component.LastCycle = _gameTiming.CurTime;

                    EntityManager.QueueDeleteEntity(args.Used);

                    CheckLevelSanity(uid, component);
                    UpdateSprite(uid, component);

                    return;
                }

                _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-already-seeded-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
                return;
            }

            if (_tagSystem.HasTag(args.Used, "Hoe"))
            {
                if (component.WeedLevel > 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-remove-weeds-message",
                        ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
                    _popupSystem.PopupEntity(Loc.GetString("plant-holder-component-remove-weeds-others-message",
                        ("otherName", Comp<MetaDataComponent>(args.User).EntityName)), uid, Filter.PvsExcept(args.User), true);
                    component.WeedLevel = 0;
                    UpdateSprite(uid, component);
                }
                else
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-no-weeds-message"), args.User);
                }

                return;
            }

            if (_tagSystem.HasTag(args.Used, "Shovel"))
            {
                if (component.Seed != null)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-remove-plant-message",
                        ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
                    _popupSystem.PopupEntity(Loc.GetString("plant-holder-component-remove-plant-others-message",
                        ("name", Comp<MetaDataComponent>(args.User).EntityName)), uid, Filter.PvsExcept(args.User), true);
                    RemovePlant(uid, component);
                }
                else
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-no-plant-message",
                        ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User);
                }

                return;
            }

            if (_solutionSystem.TryGetDrainableSolution(args.Used, out var solution)
                && _solutionSystem.TryGetSolution(uid, component.SoilSolutionName, out var targetSolution)
                && TryComp(args.Used, out SprayComponent? spray))
            {
                var amount = FixedPoint2.New(1);

                var targetEntity = uid;
                var solutionEntity = args.Used;

                _audio.PlayPvs(spray.SpraySound, args.Used, AudioParams.Default.WithVariation(0.125f));

                var split =_solutionSystem.Drain(solutionEntity, solution, amount);

                if (split.Volume == 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-no-plant-message",
                        ("owner", args.Used)), args.User);
                    return;
                }

                _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-spray-message",
                    ("owner", uid),
                    ("amount", split.Volume)), args.User, PopupType.Medium);

               _solutionSystem.TryAddSolution(targetEntity, targetSolution, split);

                ForceUpdateByExternalCause(uid, component);

                return;
            }

            if (_tagSystem.HasTag(args.Used, "PlantSampleTaker"))
            {
                if (component.Seed == null)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-nothing-to-sample-message"), args.User);
                    return;
                }

                if (component.Sampled)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-already-sampled-message"), args.User);
                    return;
                }

                if (component.Dead)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-dead-plant-message"), args.User);
                    return;
                }

                component.Seed.Unique = false;
                var seed = _botanySystem.SpawnSeedPacket(component.Seed, Transform(args.User).Coordinates);
                seed.RandomOffset(0.25f);
                var displayName = Loc.GetString(component.Seed.DisplayName);
                _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-take-sample-message",
                    ("seedName", displayName)), args.User);
                component.Health -= (_random.Next(3, 5) * 10);

                if (_random.Prob(0.3f))
                    component.Sampled = true;

                // Just in case.
                CheckLevelSanity(uid, component);
                ForceUpdateByExternalCause(uid, component);

                return;
            }

            if (HasComp<SharpComponent>(args.Used))
                DoHarvest(uid, args.User, component);

            if (TryComp<ProduceComponent?>(args.Used, out var produce))
            {
                _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-compost-message",
                    ("owner", uid),
                    ("usingItem", args.Used)), args.User, PopupType.Medium);
                _popupSystem.PopupEntity(Loc.GetString("plant-holder-component-compost-others-message",
                    ("user", Identity.Entity(args.User, EntityManager)),
                    ("usingItem", args.Used),
                    ("owner", uid)), uid, Filter.PvsExcept(args.User), true);

                if (_solutionSystem.TryGetSolution(args.Used, produce.SolutionName, out var solution2))
                {
                    // This deliberately discards overfill.
                   _solutionSystem.TryAddSolution(args.Used, solution2,
                       _solutionSystem.SplitSolution(args.Used, solution2, solution2.Volume));

                    ForceUpdateByExternalCause(uid, component);
                }

                EntityManager.QueueDeleteEntity(args.Used);
            }
        }

        private void OnInteractHand(EntityUid uid, PlantHolderComponent component, InteractHandEvent args)
        {
            DoHarvest(uid, args.User, component);
        }

        public void WeedInvasion()
        {
            // TODO
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

            // Process mutations
            if (component.MutationLevel > 0)
            {
                Mutate(uid, Math.Min(component.MutationLevel, 25), component);
                component.MutationLevel = 0;
            }

            // Weeds like water and nutrients! They may appear even if there's not a seed planted.
            if (component.WaterLevel > 10 && component.NutritionLevel > 2 && _random.Prob(component.Seed == null ? 0.05f : 0.01f))
            {
                component.WeedLevel += 1 * HydroponicsSpeedMultiplier * component.WeedCoefficient;

                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }

            // There's a chance for a weed explosion to happen if weeds take over.
            // Plants that are themselves weeds (WeedTolerance > 8) are unaffected.
            if (component.WeedLevel >= 10 && _random.Prob(0.1f))
            {
                if (component.Seed == null || component.WeedLevel >= component.Seed.WeedTolerance + 2)
                    WeedInvasion();
            }

            // If we have no seed planted, or the plant is dead, stop processing here.
            if (component.Seed == null || component.Dead)
            {
                if (component.UpdateSpriteAfterUpdate)
                    UpdateSprite(uid, component);

                return;
            }

            // There's a small chance the pest population increases.
            // Can only happen when there's a live seed planted.
            if (_random.Prob(0.01f))
            {
                component.PestLevel += 0.5f * HydroponicsSpeedMultiplier;
                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }

            // Advance plant age here.
            if (component.SkipAging > 0)
                component.SkipAging--;
            else
            {
                if (_random.Prob(0.8f))
                    component.Age += (int) (1 * HydroponicsSpeedMultiplier);

                component.UpdateSpriteAfterUpdate = true;
            }

            // Nutrient consumption.
            if (component.Seed.NutrientConsumption > 0 && component.NutritionLevel > 0 && _random.Prob(0.75f))
            {
                component.NutritionLevel -= MathF.Max(0f, component.Seed.NutrientConsumption * HydroponicsSpeedMultiplier);
                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }

            // Water consumption.
            if (component.Seed.WaterConsumption > 0 && component.WaterLevel > 0 && _random.Prob(0.75f))
            {
                component.WaterLevel -= MathF.Max(0f,
                    component.Seed.NutrientConsumption * HydroponicsConsumptionMultiplier * HydroponicsSpeedMultiplier);
                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }

            var healthMod = _random.Next(1, 3) * HydroponicsSpeedMultiplier;

            // Make sure genetics are viable.
            if (!component.Seed.Viable)
            {
                AffectGrowth(uid, -1, component);
                component.Health -= 6*healthMod;
            }

            // Make sure the plant is not starving.
            if (_random.Prob(0.35f))
            {
                if (component.NutritionLevel > 2)
                {
                    component.Health += healthMod;
                }
                else
                {
                    AffectGrowth(uid, -1, component);
                    component.Health -= healthMod;
                }

                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }

            // Make sure the plant is not thirsty.
            if (_random.Prob(0.35f))
            {
                if (component.WaterLevel > 10)
                {
                    component.Health += healthMod;
                }
                else
                {
                    AffectGrowth(uid, -1, component);
                    component.Health -= healthMod;
                }

                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }
            var environment = _atmosphere.GetContainingMixture(uid, true, true) ?? GasMixture.SpaceGas;

            if (component.Seed.ConsumeGasses.Count > 0)
            {
                component.MissingGas = 0;

                foreach (var (gas, amount) in component.Seed.ConsumeGasses)
                {
                    if (environment.GetMoles(gas) < amount)
                    {
                        component.MissingGas++;
                        continue;
                    }

                    environment.AdjustMoles(gas, -amount);
                }

                if (component.MissingGas > 0)
                {
                    component.Health -= component.MissingGas * HydroponicsSpeedMultiplier;
                    if (component.DrawWarnings)
                        component.UpdateSpriteAfterUpdate = true;
                }
            }

            // SeedPrototype pressure resistance.
            var pressure = environment.Pressure;
            if (pressure < component.Seed.LowPressureTolerance || pressure > component.Seed.HighPressureTolerance)
            {
                component.Health -= healthMod;
                component.ImproperPressure = true;
                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }
            else
            {
                component.ImproperPressure = false;
            }

            // SeedPrototype ideal temperature.
            if (MathF.Abs(environment.Temperature - component.Seed.IdealHeat) > component.Seed.HeatTolerance)
            {
                component.Health -= healthMod;
                component.ImproperHeat = true;
                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }
            else
            {
                component.ImproperHeat = false;
            }

            // Gas production.
            var exudeCount = component.Seed.ExudeGasses.Count;
            if (exudeCount > 0)
            {
                foreach (var (gas, amount) in component.Seed.ExudeGasses)
                {
                    environment.AdjustMoles(gas,
                        MathF.Max(1f, MathF.Round(amount * MathF.Round(component.Seed.Potency) / exudeCount)));
                }
            }

            // Toxin levels beyond the plant's tolerance cause damage.
            // They are, however, slowly reduced over time.
            if (component.Toxins > 0)
            {
                var toxinUptake = MathF.Max(1, MathF.Round(component.Toxins / 10f));
                if (component.Toxins > component.Seed.ToxinsTolerance)
                {
                    component.Health -= toxinUptake;
                }

                component.Toxins -= toxinUptake;
                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }

            // Weed levels.
            if (component.PestLevel > 0)
            {
                // TODO: Carnivorous plants?
                if (component.PestLevel > component.Seed.PestTolerance)
                {
                    component.Health -= HydroponicsSpeedMultiplier;
                }

                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }

            // Weed levels.
            if (component.WeedLevel > 0)
            {
                // TODO: Parasitic plants.
                if (component.WeedLevel >= component.Seed.WeedTolerance)
                {
                    component.Health -= HydroponicsSpeedMultiplier;
                }

                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }

            if (component.Age > component.Seed.Lifespan)
            {
                component.Health -= _random.Next(3, 5) * HydroponicsSpeedMultiplier;
                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }
            else if (component.Age < 0) // Revert back to seed packet!
            {
                _botanySystem.SpawnSeedPacket(component.Seed, Transform(uid).Coordinates);
                RemovePlant(uid, component);
                component.ForceUpdate = true;
                Update(uid, component);
            }

            CheckHealth(uid, component);

            if (component.Harvest && component.Seed.HarvestRepeat == HarvestType.SelfHarvest)
                AutoHarvest(uid, component);

            // If enough time has passed since the plant was harvested, we're ready to harvest again!
            if (!component.Dead && component.Seed.ProductPrototypes.Count > 0)
            {
                if (component.Age > component.Seed.Production)
                {
                    if (component.Age - component.LastProduce > component.Seed.Production && !component.Harvest)
                    {
                        component.Harvest = true;
                        component.LastProduce = component.Age;
                    }
                }
                else
                {
                    if (component.Harvest)
                    {
                        component.Harvest = false;
                        component.LastProduce = component.Age;
                    }
                }
            }

            CheckLevelSanity(uid, component);

            if (component.Seed.Sentient)
            {
                var comp = EnsureComp<GhostTakeoverAvailableComponent>(uid);
                comp.RoleName = MetaData(uid).EntityName;
                comp.RoleDescription = Loc.GetString("station-event-random-sentience-role-description", ("name", comp.RoleName));
            }

            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);
        }

        //TODO: kill this bullshit
        public void CheckLevelSanity(EntityUid uid, PlantHolderComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.Seed != null)
                component.Health = MathHelper.Clamp(component.Health, 0, component.Seed.Endurance);
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

        public bool DoHarvest(EntityUid plantholder, EntityUid user, PlantHolderComponent? component = null)
        {
            if (!Resolve(plantholder, ref component))
                return false;

            if (component.Seed == null || Deleted(user))
                return false;


            if (component.Harvest && !component.Dead)
            {
                if (TryComp<HandsComponent>(user, out var hands))
                {
                    if (!_botanySystem.CanHarvest(component.Seed, hands.ActiveHandEntity))
                        return false;
                }
                else if (!_botanySystem.CanHarvest(component.Seed))
                {
                    return false;
                }

                _botanySystem.Harvest(component.Seed, user, component.YieldMod);
                AfterHarvest(plantholder, component);
                return true;
            }

            if (!component.Dead)
                return false;

            RemovePlant(plantholder, component);
            AfterHarvest(plantholder, component);
            return true;
        }

        public void AutoHarvest(EntityUid uid, PlantHolderComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.Seed == null || !component.Harvest)
                return;

            _botanySystem.AutoHarvest(component.Seed, Transform(uid).Coordinates);
            AfterHarvest(uid, component);
        }

        private void AfterHarvest(EntityUid uid, PlantHolderComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Harvest = false;
            component.LastProduce = component.Age;

            if (component.Seed?.HarvestRepeat == HarvestType.NoRepeat)
                RemovePlant(uid, component);

            CheckLevelSanity(uid, component);
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

        public void Die(EntityUid uid, PlantHolderComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Dead = true;
            component.Harvest = false;
            component.MutationLevel = 0;
            component.YieldMod = 1;
            component.MutationMod = 1;
            component.ImproperLight = false;
            component.ImproperHeat = false;
            component.ImproperPressure = false;
            component.WeedLevel += 1 * HydroponicsSpeedMultiplier;
            component.PestLevel = 0;
            UpdateSprite(uid, component);
        }

        public void RemovePlant(EntityUid uid, PlantHolderComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.YieldMod = 1;
            component.MutationMod = 1;
            component.PestLevel = 0;
            component.Seed = null;
            component.Dead = false;
            component.Age = 0;
            component.Sampled = false;
            component.Harvest = false;
            component.ImproperLight = false;
            component.ImproperPressure = false;
            component.ImproperHeat = false;

            UpdateSprite(uid, component);
        }

        public void AffectGrowth(EntityUid uid, int amount, PlantHolderComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.Seed == null)
                return;

            if (amount > 0)
            {
                if (component.Age < component.Seed.Maturation)
                    component.Age += amount;
                else if (!component.Harvest && component.Seed.Yield <= 0f)
                    component.LastProduce -= amount;
            }
            else
            {
                if (component.Age < component.Seed.Maturation)
                    component.SkipAging++;
                else if (!component.Harvest && component.Seed.Yield <= 0f)
                    component.LastProduce += amount;
            }
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

            if (!_solutionSystem.TryGetSolution(uid, component.SoilSolutionName, out var solution))
                return;

            if (solution.Volume > 0 && component.MutationLevel < 25)
            {
                var amt = FixedPoint2.New(1);
                foreach (var (reagentId, quantity) in _solutionSystem.RemoveEachReagent(uid, solution, amt))
                {
                    var reagentProto = _prototype.Index<ReagentPrototype>(reagentId);
                    reagentProto.ReactionPlant(uid, new Solution.ReagentQuantity(reagentId, quantity), solution);
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
                _mutation.MutateSeed(component.Seed, severity);
            }
        }

        public void UpdateSprite(EntityUid uid, PlantHolderComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.UpdateSpriteAfterUpdate = false;

            if (component.Seed != null && component.Seed.Bioluminescent)
            {
                var light = EnsureComp<PointLightComponent>(uid);
                light.Radius = component.Seed.BioluminescentRadius;
                light.Color = component.Seed.BioluminescentColor;
                light.CastShadows = false; // this is expensive, and botanists make lots of plants
                Dirty(light);
            }
            else
            {
                RemComp<PointLightComponent>(uid);
            }

            if (!TryComp<AppearanceComponent>(uid, out var app))
                return;

            if (component.Seed != null)
            {
                if (component.DrawWarnings)
                {
                    _appearance.SetData(uid, PlantHolderVisuals.HealthLight,
                        component.Health <= component.Seed.Endurance / 2f);
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
                else if (component.Age < component.Seed.Maturation)
                {
                    var growthStage = Math.Max(1, (int) (component.Age * component.Seed.GrowthStages / component.Seed.Maturation));

                    _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                    _appearance.SetData(uid, PlantHolderVisuals.PlantState, $"stage-{growthStage}", app);
                    component.LastProduce = component.Age;
                }
                else
                {
                    _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                    _appearance.SetData(uid, PlantHolderVisuals.PlantState, $"stage-{component.Seed.GrowthStages}", app);
                }
            }
            else
            {
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, "", app);
                _appearance.SetData(uid, PlantHolderVisuals.HealthLight, false, app);
            }

            if (!component.DrawWarnings)
                return;

            _appearance.SetData(uid, PlantHolderVisuals.WaterLight, component.WaterLevel <= 10, app);
            _appearance.SetData(uid, PlantHolderVisuals.NutritionLight, component.NutritionLevel <= 2, app);
            _appearance.SetData(uid, PlantHolderVisuals.AlertLight,
                component.WeedLevel >= 5 || component.PestLevel >= 5 || component.Toxins >= 40 || component.ImproperHeat ||
                component.ImproperLight || component.ImproperPressure || component.MissingGas > 0, app);
            _appearance.SetData(uid, PlantHolderVisuals.HarvestLight, component.Harvest, app);
        }

        /// <summary>
        ///     Check if the currently contained seed is unique. If it is not, clone it so that we have a unique seed.
        ///     Necessary to avoid modifying global seeds.
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
    }
}
