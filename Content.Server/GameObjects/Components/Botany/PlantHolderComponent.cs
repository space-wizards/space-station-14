#nullable enable
using System;
using Content.Server.Atmos;
using Content.Server.Botany;
using Content.Server.GameObjects.Components.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Botany
{
    [RegisterComponent]
    public class PlantHolderComponent : Component
    {
        public const float HydroponicsSpeedMultiplier = 1f;

        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "PlantHolder";

        private int _lastProduce;
        private bool _updateSpriteAfterUpdate = false;

        protected virtual bool DrawWarnings { get; } = false;

        public float WaterLevel { get; set; } = 100f;
        public float NutritionLevel { get; set; } = 100f;
        public float PestLevel { get; set; } = 0f;
        public float WeedLevel { get; set; } = 0f;
        public float Toxins { get; set; } = 0f;

        public int Age { get; set; } = 0;
        public int SkipAging { get; set; } = 0;
        public bool Dead { get; set; } = false;
        public bool Harvest { get; set; } = false;
        public bool Sampled { get; set; } = false;

        public float YieldMod { get; set; } = 1f;
        public float MutationMod { get; set; } = 1f;
        public float MutationLevel { get; set; } = 0f;

        public float Health { get; set; } = 0f;

        public float WeedCoefficient { get; set; } = 1f;

        public Seed? Seed { get; set; } = null;

        [ComponentDependency] private readonly SolutionContainerComponent? _solutionContainer = default!;

        public override void Initialize()
        {
            base.Initialize();

            if(!Owner.EnsureComponent<SolutionContainerComponent>(out var _))
                Logger.Warning($"Entity {Owner} with a PlantHolderComponent did not have a SolutionContainerComponent.");
        }

        public void WeedInvasion()
        {
            // TODO
        }

        public void Update(float frameTime)
        {
            UpdateReagents(frameTime);

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
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Seed ideal temperature.
            if (MathF.Abs(environment.Temperature - Seed.IdealHeat) > Seed.HeatTolerance)
            {
                Health -= healthMod;
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
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
                Update(frameTime);
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
            // TODO
        }

        public void AutoHarvest()
        {
            // TODO
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
            // TODO
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

        public void UpdateReagents(float frameTime)
        {
            // TODO
        }

        public void UpdateSprite()
        {
            _updateSpriteAfterUpdate = false;
        }
    }
}
