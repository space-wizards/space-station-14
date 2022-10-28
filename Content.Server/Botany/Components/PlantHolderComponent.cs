using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Botany;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Components
{
    [RegisterComponent]
    public sealed class PlantHolderComponent : Component
    {
        public const float HydroponicsSpeedMultiplier = 1f;
        public const float HydroponicsConsumptionMultiplier = 4f;

        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables] private int _lastProduce;

        [ViewVariables(VVAccess.ReadWrite)] public int MissingGas;

        private readonly TimeSpan _cycleDelay = TimeSpan.FromSeconds(15f);

        [ViewVariables] public TimeSpan LastCycle = TimeSpan.Zero;

        [ViewVariables(VVAccess.ReadWrite)] private bool _updateSpriteAfterUpdate;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("drawWarnings")]
        public bool DrawWarnings { get; private set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public float WaterLevel { get; private set; } = 100f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float NutritionLevel { get; private set; } = 100f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float PestLevel { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float WeedLevel { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Toxins { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int Age { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int SkipAging { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Dead { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Harvest { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Sampled { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int YieldMod { get; set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        public float MutationMod { get; set; } = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float MutationLevel { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Health { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float WeedCoefficient { get; set; } = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public SeedData? Seed { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperHeat { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperPressure { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperLight { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ForceUpdate { get; set; }

        [DataField("solution")]
        public string SoilSolutionName { get; set; } = "soil";

        public void WeedInvasion()
        {
            // TODO
        }

        public void Update()
        {
            UpdateReagents();

            var curTime = _gameTiming.CurTime;

            if (ForceUpdate)
                ForceUpdate = false;
            else if (curTime < (LastCycle + _cycleDelay))
            {
                if (_updateSpriteAfterUpdate)
                    UpdateSprite();
                return;
            }

            LastCycle = curTime;

            // todo ecs.
            var botanySystem = EntitySystem.Get<BotanySystem>();

            // Process mutations
            if (MutationLevel > 0)
            {
                Mutate(Math.Min(MutationLevel, 25));
                MutationLevel = 0;
            }

            // Weeds like water and nutrients! They may appear even if there's not a seed planted.
            if (WaterLevel > 10 && NutritionLevel > 2 && _random.Prob(Seed == null ? 0.05f : 0.01f))
            {
                WeedLevel += 1 * HydroponicsSpeedMultiplier * WeedCoefficient;

                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // There's a chance for a weed explosion to happen if weeds take over.
            // Plants that are themselves weeds (WeedTolerance > 8) are unaffected.
            if (WeedLevel >= 10 && _random.Prob(0.1f))
            {
                if (Seed == null || WeedLevel >= Seed.WeedTolerance + 2)
                    WeedInvasion();
            }

            // If we have no seed planted, or the plant is dead, stop processing here.
            if (Seed == null || Dead)
            {
                if (_updateSpriteAfterUpdate)
                    UpdateSprite();

                return;
            }

            // There's a small chance the pest population increases.
            // Can only happen when there's a live seed planted.
            if (_random.Prob(0.01f))
            {
                PestLevel += 0.5f * HydroponicsSpeedMultiplier;
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Advance plant age here.
            if (SkipAging > 0)
                SkipAging--;
            else
            {
                if (_random.Prob(0.8f))
                    Age += (int) (1 * HydroponicsSpeedMultiplier);

                _updateSpriteAfterUpdate = true;
            }

            // Nutrient consumption.
            if (Seed.NutrientConsumption > 0 && NutritionLevel > 0 && _random.Prob(0.75f))
            {
                NutritionLevel -= MathF.Max(0f, Seed.NutrientConsumption * HydroponicsSpeedMultiplier);
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Water consumption.
            if (Seed.WaterConsumption > 0 && WaterLevel > 0 && _random.Prob(0.75f))
            {
                WaterLevel -= MathF.Max(0f,
                    Seed.NutrientConsumption * HydroponicsConsumptionMultiplier * HydroponicsSpeedMultiplier);
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            var healthMod = _random.Next(1, 3) * HydroponicsSpeedMultiplier;

            // Make sure genetics are viable.
            if (!Seed.Viable)
            {
                AffectGrowth(-1);
                Health -= 6*healthMod;
            }

            // Make sure the plant is not starving.
            if (_random.Prob(0.35f))
            {
                if (NutritionLevel > 2)
                {
                    Health += healthMod;
                }
                else
                {
                    AffectGrowth(-1);
                    Health -= healthMod;
                }

                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Make sure the plant is not thirsty.
            if (_random.Prob(0.35f))
            {
                if (WaterLevel > 10)
                {
                    Health += healthMod;
                }
                else
                {
                    AffectGrowth(-1);
                    Health -= healthMod;
                }

                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            var atmosphereSystem = _entMan.EntitySysManager.GetEntitySystem<AtmosphereSystem>();
            var environment = atmosphereSystem.GetContainingMixture(Owner, true, true) ??
                              GasMixture.SpaceGas;

            if (Seed.ConsumeGasses.Count > 0)
            {
                MissingGas = 0;

                foreach (var (gas, amount) in Seed.ConsumeGasses)
                {
                    if (environment.GetMoles(gas) < amount)
                    {
                        MissingGas++;
                        continue;
                    }

                    environment.AdjustMoles(gas, -amount);
                }

                if (MissingGas > 0)
                {
                    Health -= MissingGas * HydroponicsSpeedMultiplier;
                    if (DrawWarnings)
                        _updateSpriteAfterUpdate = true;
                }
            }

            // SeedPrototype pressure resistance.
            var pressure = environment.Pressure;
            if (pressure < Seed.LowPressureTolerance || pressure > Seed.HighPressureTolerance)
            {
                Health -= healthMod;
                ImproperPressure = true;
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }
            else
            {
                ImproperPressure = false;
            }

            // SeedPrototype ideal temperature.
            if (MathF.Abs(environment.Temperature - Seed.IdealHeat) > Seed.HeatTolerance)
            {
                Health -= healthMod;
                ImproperHeat = true;
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }
            else
            {
                ImproperHeat = false;
            }

            // Gas production.
            var exudeCount = Seed.ExudeGasses.Count;
            if (exudeCount > 0)
            {
                foreach (var (gas, amount) in Seed.ExudeGasses)
                {
                    environment.AdjustMoles(gas,
                        MathF.Max(1f, MathF.Round((amount * MathF.Round(Seed.Potency)) / exudeCount)));
                }
            }

            // Toxin levels beyond the plant's tolerance cause damage.
            // They are, however, slowly reduced over time.
            if (Toxins > 0)
            {
                var toxinUptake = MathF.Max(1, MathF.Round(Toxins / 10f));
                if (Toxins > Seed.ToxinsTolerance)
                {
                    Health -= toxinUptake;
                }

                Toxins -= toxinUptake;
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Weed levels.
            if (PestLevel > 0)
            {
                // TODO: Carnivorous plants?
                if (PestLevel > Seed.PestTolerance)
                {
                    Health -= HydroponicsSpeedMultiplier;
                }

                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Weed levels.
            if (WeedLevel > 0)
            {
                // TODO: Parasitic plants.
                if (WeedLevel >= Seed.WeedTolerance)
                {
                    Health -= HydroponicsSpeedMultiplier;
                }

                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            if (Age > Seed.Lifespan)
            {
                Health -= _random.Next(3, 5) * HydroponicsSpeedMultiplier;
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }
            else if (Age < 0) // Revert back to seed packet!
            {
                botanySystem.SpawnSeedPacket(Seed, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
                RemovePlant();
                ForceUpdate = true;
                Update();
            }

            CheckHealth();

            if (Harvest && Seed.HarvestRepeat == HarvestType.SelfHarvest)
                AutoHarvest();

            // If enough time has passed since the plant was harvested, we're ready to harvest again!
            if (!Dead && Seed.ProductPrototypes.Count > 0)
            {
                if (Age > Seed.Production)
                {
                    if ((Age - _lastProduce) > Seed.Production && !Harvest)
                    {
                        Harvest = true;
                        _lastProduce = Age;
                    }
                }
                else
                {
                    if (Harvest)
                    {
                        Harvest = false;
                        _lastProduce = Age;
                    }
                }
            }

            CheckLevelSanity();

            if (Seed.Sentient)
            {
                var comp = _entMan.EnsureComponent<GhostTakeoverAvailableComponent>(Owner);
                comp.RoleName = _entMan.GetComponent<MetaDataComponent>(Owner).EntityName;
                comp.RoleDescription = Loc.GetString("station-event-random-sentience-role-description", ("name", comp.RoleName));
            }

            if (_updateSpriteAfterUpdate)
                UpdateSprite();
        }

        public void CheckLevelSanity()
        {
            if (Seed != null)
                Health = MathHelper.Clamp(Health, 0, Seed.Endurance);
            else
            {
                Health = 0f;
                Dead = false;
            }

            MutationLevel = MathHelper.Clamp(MutationLevel, 0f, 100f);
            NutritionLevel = MathHelper.Clamp(NutritionLevel, 0f, 100f);
            WaterLevel = MathHelper.Clamp(WaterLevel, 0f, 100f);
            PestLevel = MathHelper.Clamp(PestLevel, 0f, 10f);
            WeedLevel = MathHelper.Clamp(WeedLevel, 0f, 10f);
            Toxins = MathHelper.Clamp(Toxins, 0f, 100f);
            YieldMod = MathHelper.Clamp(YieldMod, 0, 2);
            MutationMod = MathHelper.Clamp(MutationMod, 0f, 3f);
        }

        public bool DoHarvest(EntityUid user)
        {
            if (Seed == null || _entMan.Deleted(user))
                return false;

            var botanySystem = EntitySystem.Get<BotanySystem>();

            if (Harvest && !Dead)
            {
                if (_entMan.TryGetComponent(user, out HandsComponent? hands))
                {
                    if (!botanySystem.CanHarvest(Seed, hands.ActiveHandEntity))
                        return false;
                }
                else if (!botanySystem.CanHarvest(Seed))
                {
                    return false;
                }

                botanySystem.Harvest(Seed, user, YieldMod);
                AfterHarvest();
                return true;
            }

            if (!Dead) return false;

            RemovePlant();
            AfterHarvest();
            return true;
        }

        public void AutoHarvest()
        {
            if (Seed == null || !Harvest)
                return;

            var botanySystem = EntitySystem.Get<BotanySystem>();

            botanySystem.AutoHarvest(Seed, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
            AfterHarvest();
        }

        private void AfterHarvest()
        {
            Harvest = false;
            _lastProduce = Age;

            if (Seed?.HarvestRepeat == HarvestType.NoRepeat)
                RemovePlant();

            CheckLevelSanity();
            UpdateSprite();
        }

        public void CheckHealth()
        {
            if (Health <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
            Dead = true;
            Harvest = false;
            MutationLevel = 0;
            YieldMod = 1;
            MutationMod = 1;
            ImproperLight = false;
            ImproperHeat = false;
            ImproperPressure = false;
            WeedLevel += 1 * HydroponicsSpeedMultiplier;
            PestLevel = 0;
            UpdateSprite();
        }

        public void RemovePlant()
        {
            YieldMod = 1;
            MutationMod = 1;
            PestLevel = 0;
            Seed = null;
            Dead = false;
            Age = 0;
            Sampled = false;
            Harvest = false;
            ImproperLight = false;
            ImproperPressure = false;
            ImproperHeat = false;

            UpdateSprite();
        }

        public void AffectGrowth(int amount)
        {
            if (Seed == null)
                return;

            if (amount > 0)
            {
                if (Age < Seed.Maturation)
                    Age += amount;
                else if (!Harvest && Seed.Yield <= 0f)
                    _lastProduce -= amount;
            }
            else
            {
                if (Age < Seed.Maturation)
                    SkipAging++;
                else if (!Harvest && Seed.Yield <= 0f)
                    _lastProduce += amount;
            }
        }

        public void AdjustNutrient(float amount)
        {
            NutritionLevel += amount;
        }

        public void AdjustWater(float amount)
        {
            WaterLevel += amount;

            // Water dilutes toxins.
            if (amount > 0)
            {
                Toxins -= amount * 4f;
            }
        }

        public void UpdateReagents()
        {
            var solutionSystem = EntitySystem.Get<SolutionContainerSystem>();
            if (!solutionSystem.TryGetSolution(Owner, SoilSolutionName, out var solution))
                return;

            if (solution.TotalVolume > 0 && MutationLevel < 25)
            {
                var amt = FixedPoint2.New(1);
                foreach (var (reagentId, quantity) in solutionSystem.RemoveEachReagent(Owner, solution, amt))
                {
                    var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagentId);
                    reagentProto.ReactionPlant(Owner, new Solution.ReagentQuantity(reagentId, quantity), solution);
                }
            }

            CheckLevelSanity();
        }

        private void Mutate(float severity)
        {
            if (Seed != null)
            {
                EnsureUniqueSeed();
                _entMan.System<MutationSystem>().MutateSeed(Seed, severity);
            }
        }

        public void UpdateSprite()
        {
            _updateSpriteAfterUpdate = false;

            if (Seed != null && Seed.Bioluminescent)
            {
                var light = _entMan.EnsureComponent<PointLightComponent>(Owner);
                light.Radius = Seed.BioluminescentRadius;
                light.Color = Seed.BioluminescentColor;
                light.CastShadows = false; // this is expensive, and botanists make lots of plants
                light.Dirty();
            }
            else
            {
                _entMan.RemoveComponent<PointLightComponent>(Owner);
            }

            if (!_entMan.TryGetComponent<AppearanceComponent>(Owner, out var appearanceComponent))
                return;

            if (Seed != null)
            {
                if (DrawWarnings)
                    appearanceComponent.SetData(PlantHolderVisuals.HealthLight, Health <= (Seed.Endurance / 2f));

                if (Dead)
                {
                    appearanceComponent.SetData(PlantHolderVisuals.PlantRsi, Seed.PlantRsi.ToString());
                    appearanceComponent.SetData(PlantHolderVisuals.PlantState, "dead");
                }
                else if (Harvest)
                {
                    appearanceComponent.SetData(PlantHolderVisuals.PlantRsi, Seed.PlantRsi.ToString());
                    appearanceComponent.SetData(PlantHolderVisuals.PlantState, "harvest");
                }
                else if (Age < Seed.Maturation)
                {
                    var growthStage = Math.Max(1, (int) ((Age * Seed.GrowthStages) / Seed.Maturation));

                    appearanceComponent.SetData(PlantHolderVisuals.PlantRsi, Seed.PlantRsi.ToString());
                    appearanceComponent.SetData(PlantHolderVisuals.PlantState, $"stage-{growthStage}");
                    _lastProduce = Age;
                }
                else
                {
                    appearanceComponent.SetData(PlantHolderVisuals.PlantRsi, Seed.PlantRsi.ToString());
                    appearanceComponent.SetData(PlantHolderVisuals.PlantState, $"stage-{Seed.GrowthStages}");
                }
            }
            else
            {
                appearanceComponent.SetData(PlantHolderVisuals.PlantState, "");
                appearanceComponent.SetData(PlantHolderVisuals.HealthLight, false);
            }

            if (!DrawWarnings) return;
            appearanceComponent.SetData(PlantHolderVisuals.WaterLight, WaterLevel <= 10);
            appearanceComponent.SetData(PlantHolderVisuals.NutritionLight, NutritionLevel <= 2);
            appearanceComponent.SetData(PlantHolderVisuals.AlertLight,
                WeedLevel >= 5 || PestLevel >= 5 || Toxins >= 40 || ImproperHeat || ImproperLight || ImproperPressure ||
                MissingGas > 0);
            appearanceComponent.SetData(PlantHolderVisuals.HarvestLight, Harvest);
        }

        /// <summary>
        ///     Check if the currently contained seed is unique. If it is not, clone it so that we have a unique seed.
        ///     Necessary to avoid modifying global seeds.
        /// </summary>
        public void EnsureUniqueSeed()
        {
            if (Seed != null && !Seed.Unique)
                Seed = Seed.Clone();
        }

        public void ForceUpdateByExternalCause()
        {
            SkipAging++; // We're forcing an update cycle, so one age hasn't passed.
            ForceUpdate = true;
            Update();
        }
    }
}
