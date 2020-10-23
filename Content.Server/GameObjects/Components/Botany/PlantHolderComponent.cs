#nullable enable
using System;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.Botany;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Botany
{
    [RegisterComponent]
    public class PlantHolderComponent : Component
    {
        public const float HydroponicsSpeedMultiplier = 1f;

        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "PlantHolder";

        [ViewVariables] private int _lastProduce;
        private readonly TimeSpan _cycleDelay = TimeSpan.FromSeconds(15f);
        [ViewVariables] private TimeSpan _lastCycle = TimeSpan.Zero;
        [ViewVariables] private bool _updateSpriteAfterUpdate = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool DrawWarnings { get; private set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public float WaterLevel { get; set; } = 100f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float NutritionLevel { get; set; } = 100f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float PestLevel { get; set; } = 0f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float WeedLevel { get; set; } = 0f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float Toxins { get; set; } = 0f;

        [ViewVariables(VVAccess.ReadWrite)]
        public int Age { get; set; } = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public int SkipAging { get; set; } = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Dead { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Harvest { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Sampled { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public int YieldMod { get; set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        public float MutationMod { get; set; } = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float MutationLevel { get; set; } = 0f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float Health { get; set; } = 0f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float WeedCoefficient { get; set; } = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public Seed? Seed { get; set; } = null;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperHeat { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperPressure { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperLight { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ForceUpdate { get; set; }

        [ComponentDependency] private readonly SolutionContainerComponent? _solutionContainer = default!;

        public override void Initialize()
        {
            base.Initialize();

            if(!Owner.EnsureComponent<SolutionContainerComponent>(out var solution))
                Logger.Warning($"Entity {Owner} with a PlantHolderComponent did not have a SolutionContainerComponent.");
        }

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
            else if (curTime < (_lastCycle + _cycleDelay))
            {
                if(_updateSpriteAfterUpdate)
                    UpdateSprite();
                return;
            }

            _lastCycle = curTime;


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
                if(_random.Prob(0.8f))
                    Age += (int)(1 * HydroponicsSpeedMultiplier);

                _updateSpriteAfterUpdate = true;
            }

            // Nutrient consumption.
            if (Seed.NutrientConsumption > 0 && NutritionLevel > 0 && _random.Prob(0.25f))
            {
                NutritionLevel -= MathF.Max(0f, Seed.NutrientConsumption * HydroponicsSpeedMultiplier);
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Water consumption.
            if (Seed.WaterConsumption > 0 && WaterLevel > 0 && _random.Prob(0.25f))
            {
                WaterLevel -= MathF.Max(0f, Seed.NutrientConsumption * HydroponicsSpeedMultiplier);
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            var healthMod = _random.Next(1, 3) * HydroponicsSpeedMultiplier;

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

            var tileAtmos = Owner.Transform.Coordinates.GetTileAtmosphere();
            var environment = tileAtmos?.Air ?? GasMixture.SpaceGas;

            if (Seed.ConsumeGasses.Count > 0)
            {
                var missingGas = 0;

                foreach (var (gas, amount) in Seed.ConsumeGasses)
                {
                    if (environment.GetMoles(gas) < amount)
                    {
                        missingGas++;
                        continue;
                    }

                    environment.AdjustMoles(gas, -amount);
                }

                if (missingGas > 0)
                {
                    Health -= missingGas * HydroponicsSpeedMultiplier;
                    if (DrawWarnings)
                        _updateSpriteAfterUpdate = true;
                }
            }

            // Seed pressure resistance.
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

            // Seed ideal temperature.
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
                    environment.AdjustMoles(gas, MathF.Max(1f, MathF.Round((amount * MathF.Round(Seed.Potency)) / exudeCount)));
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
            else if(Age < 0) // Revert back to seed packet!
            {
                Seed.SpawnSeedPacket(Owner.Transform.Coordinates);
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

            if(_updateSpriteAfterUpdate)
                UpdateSprite();
        }

        private void CheckLevelSanity()
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

        public void AutoHarvest()
        {
            if (Seed == null || !Harvest)
                return;

            Seed.AutoHarvest(Owner.Transform.Coordinates);
            AfterHarvest();
        }

        private void AfterHarvest()
        {
            Harvest = false;
            _lastProduce = Age;

            if(Seed?.HarvestRepeat == HarvestType.NoRepeat)
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
             // TODO
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
            if (_solutionContainer == null)
                return;

            if (_solutionContainer.Solution.TotalVolume <= 0 || MutationLevel >= 25)
            {
                if (MutationLevel >= 0)
                {
                    Mutate(Math.Min(MutationLevel, 25));
                    MutationLevel = 0;
                }
            }
            else
            {
                var one = ReagentUnit.New(1);

                foreach (var (reagent, amount) in _solutionContainer.ReagentList.ToArray())
                {
                    var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent);
                    reagentProto.ReactionPlant(Owner);
                    _solutionContainer.Solution.RemoveReagent(reagent, amount < one ? amount : one);
                }
            }

            CheckLevelSanity();
        }

        private void Mutate(float mutation)
        {
            // TODO
        }

        public void UpdateSprite()
        {
            _updateSpriteAfterUpdate = false;

            UpdateName();
        }

        private void UpdateName()
        {
            // TODO
        }

        public void CheckForDivergence(bool modified)
        {
            // Make sure we're not modifying a "global" seed.
            // If this seed is not in the global seed list, then no products of this line have been harvested yet.
            // It is then safe to assume it's restricted to this tray.
            if (Seed == null) return;
            var plantSystem = EntitySystem.Get<PlantSystem>();
            if (plantSystem.Seeds.ContainsKey(Seed.Uid))
                Seed = Seed.Diverge(modified);
        }
    }
}
