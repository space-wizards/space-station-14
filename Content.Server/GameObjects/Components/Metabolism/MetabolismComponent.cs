#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Body.Behavior;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Metabolism
{
    [RegisterComponent]
    public class MetabolismComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ComponentDependency] private readonly IBody? _body = default!;

        public override string Name => "Metabolism";

        private float _accumulatedFrameTime;

        private bool _isShivering;
        private bool _isSweating;

        [ViewVariables(VVAccess.ReadWrite)] private int _suffocationDamage;

        [ViewVariables] public Dictionary<Gas, float> NeedsGases { get; set; } = new();

        [ViewVariables] public Dictionary<Gas, float> ProducesGases { get; set; } = new();

        [ViewVariables] public Dictionary<Gas, float> DeficitGases { get; set; } = new();

        /// <summary>
        /// Heat generated due to metabolism. It's generated via metabolism
        /// </summary>
        [ViewVariables]
        public float MetabolismHeat { get; private set; }

        /// <summary>
        /// Heat output via radiation.
        /// </summary>
        [ViewVariables]
        public float RadiatedHeat { get; private set; }

        /// <summary>
        /// Maximum heat regulated via sweat
        /// </summary>
        [ViewVariables]
        public float SweatHeatRegulation { get; private set; }

        /// <summary>
        /// Maximum heat regulated via shivering
        /// </summary>
        [ViewVariables]
        public float ShiveringHeatRegulation { get; private set; }

        /// <summary>
        /// Amount of heat regulation that represents thermal regulation processes not
        /// explicitly coded.
        /// </summary>
        public float ImplicitHeatRegulation { get; private set; }

        /// <summary>
        /// Normal body temperature
        /// </summary>
        [ViewVariables]
        public float NormalBodyTemperature { get; private set; }

        /// <summary>
        /// Deviation from normal temperature for body to start thermal regulation
        /// </summary>
        public float ThermalRegulationTemperatureThreshold { get; private set; }

        [ViewVariables] public bool Suffocating { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, b => b.NeedsGases, "needsGases", new Dictionary<Gas, float>());
            serializer.DataField(this, b => b.ProducesGases, "producesGases", new Dictionary<Gas, float>());
            serializer.DataField(this, b => b.DeficitGases, "deficitGases", new Dictionary<Gas, float>());
            serializer.DataField(this, b => b.MetabolismHeat, "metabolismHeat", 0);
            serializer.DataField(this, b => b.RadiatedHeat, "radiatedHeat", 0);
            serializer.DataField(this, b => b.SweatHeatRegulation, "sweatHeatRegulation", 0);
            serializer.DataField(this, b => b.ShiveringHeatRegulation, "shiveringHeatRegulation", 0);
            serializer.DataField(this, b => b.ImplicitHeatRegulation, "implicitHeatRegulation", 0);
            serializer.DataField(this, b => b.NormalBodyTemperature, "normalBodyTemperature", 0);
            serializer.DataField(this, b => b.ThermalRegulationTemperatureThreshold,
                "thermalRegulationTemperatureThreshold", 0);
            serializer.DataField(ref _suffocationDamage, "suffocationDamage", 1);
        }

        private Dictionary<Gas, float> NeedsAndDeficit(float frameTime)
        {
            var needs = new Dictionary<Gas, float>(NeedsGases);
            foreach (var (gas, amount) in DeficitGases)
            {
                var newAmount = (needs.GetValueOrDefault(gas) + amount) * frameTime;
                needs[gas] = newAmount;
            }

            return needs;
        }

        private void ClampDeficit()
        {
            var deficitGases = new Dictionary<Gas, float>(DeficitGases);

            foreach (var (gas, deficit) in deficitGases)
            {
                if (!NeedsGases.TryGetValue(gas, out var need))
                {
                    DeficitGases.Remove(gas);
                    continue;
                }

                if (deficit > need)
                {
                    DeficitGases[gas] = need;
                }
            }
        }

        private float SuffocatingPercentage()
        {
            var percentages = new float[Atmospherics.TotalNumberOfGases];

            foreach (var (gas, deficit) in DeficitGases)
            {
                if (!NeedsGases.TryGetValue(gas, out var needed))
                {
                    percentages[(int) gas] = 1;
                    continue;
                }

                percentages[(int) gas] = deficit / needed;
            }

            return percentages.Average();
        }

        private float GasProducedMultiplier(Gas gas, float usedAverage)
        {
            if (!ProducesGases.TryGetValue(gas, out var produces))
            {
                return 0;
            }

            if (!NeedsGases.TryGetValue(gas, out var needs))
            {
                needs = 1;
            }

            return needs * produces * usedAverage;
        }

        private Dictionary<Gas, float> GasProduced(float usedAverage)
        {
            return ProducesGases.ToDictionary(pair => pair.Key, pair => GasProducedMultiplier(pair.Key, usedAverage));
        }

        private void ProcessGases(float frameTime)
        {
            if (!Owner.TryGetComponent(out BloodstreamComponent? bloodstream))
            {
                return;
            }

            if (_body == null)
            {
                return;
            }

            var lungs = _body.GetMechanismBehaviors<LungBehavior>().ToArray();

            var needs = NeedsAndDeficit(frameTime);
            var used = 0f;
            foreach (var (gas, amountNeeded) in needs)
            {
                var bloodstreamAmount = bloodstream.Air.GetMoles(gas);
                var deficit = 0f;

                if (bloodstreamAmount < amountNeeded)
                {
                    // Panic inhale
                    foreach (var lung in lungs)
                    {
                        lung.Gasp();
                    }

                    bloodstreamAmount = bloodstream.Air.GetMoles(gas);

                    deficit = Math.Max(0, amountNeeded - bloodstreamAmount);

                    if (deficit > 0)
                    {
                        bloodstream.Air.SetMoles(gas, 0);
                    }
                    else
                    {
                        bloodstream.Air.AdjustMoles(gas, -amountNeeded);
                    }
                }
                else
                {
                    bloodstream.Air.AdjustMoles(gas, -amountNeeded);
                }

                DeficitGases[gas] = deficit;


                used += (amountNeeded - deficit) / amountNeeded;
            }

            var produced = GasProduced(used / needs.Count);

            foreach (var (gas, amountProduced) in produced)
            {
                bloodstream.Air.AdjustMoles(gas, amountProduced);
            }

            ClampDeficit();
        }

        /// <summary>
        /// Process thermal regulation
        /// </summary>
        /// <param name="frameTime"></param>
        private void ProcessThermalRegulation(float frameTime)
        {
            if (!Owner.TryGetComponent(out TemperatureComponent? temperatureComponent)) return;
            temperatureComponent.ReceiveHeat(MetabolismHeat);
            temperatureComponent.RemoveHeat(RadiatedHeat);

            // implicit heat regulation
            var tempDiff = Math.Abs(temperatureComponent.CurrentTemperature - NormalBodyTemperature);
            var targetHeat = tempDiff * temperatureComponent.HeatCapacity;
            if (temperatureComponent.CurrentTemperature > NormalBodyTemperature)
            {
                temperatureComponent.RemoveHeat(Math.Min(targetHeat, ImplicitHeatRegulation));
            }
            else
            {
                temperatureComponent.ReceiveHeat(Math.Min(targetHeat, ImplicitHeatRegulation));
            }

            // recalc difference and target heat
            tempDiff = Math.Abs(temperatureComponent.CurrentTemperature - NormalBodyTemperature);
            targetHeat = tempDiff * temperatureComponent.HeatCapacity;

            // if body temperature is not within comfortable, thermal regulation
            // processes starts
            if (tempDiff < ThermalRegulationTemperatureThreshold)
            {
                if (_isShivering || _isSweating)
                {
                    Owner.PopupMessage(Loc.GetString("You feel comfortable"));
                }

                _isShivering = false;
                _isSweating = false;
                return;
            }


            if (temperatureComponent.CurrentTemperature > NormalBodyTemperature)
            {
                if (!ActionBlockerSystem.CanSweat(Owner)) return;
                if (!_isSweating)
                {
                    Owner.PopupMessage(Loc.GetString("You are sweating"));
                    _isSweating = true;
                }

                // creadth: sweating does not help in airless environment
                if (Owner.Transform.Coordinates.TryGetTileAir(out _, Owner.EntityManager))
                {
                    temperatureComponent.RemoveHeat(Math.Min(targetHeat, SweatHeatRegulation));
                }
            }
            else
            {
                if (!ActionBlockerSystem.CanShiver(Owner)) return;
                if (!_isShivering)
                {
                    Owner.PopupMessage(Loc.GetString("You are shivering"));
                    _isShivering = true;
                }

                temperatureComponent.ReceiveHeat(Math.Min(targetHeat, ShiveringHeatRegulation));
            }
        }

        /// <summary>
        ///     Loops through each reagent in _internalSolution,
        ///     and calls <see cref="IMetabolizable.Metabolize"/> for each of them.
        /// </summary>
        /// <param name="frameTime">The time since the last metabolism tick in seconds.</param>
        private void ProcessNutrients(float frameTime)
        {
            if (!Owner.TryGetComponent(out BloodstreamComponent? bloodstream))
            {
                return;
            }

            if (bloodstream.Solution.CurrentVolume == 0)
            {
                return;
            }

            // Run metabolism for each reagent, remove metabolized reagents
            // Using ToList here lets us edit reagents while iterating
            foreach (var reagent in bloodstream.Solution.ReagentList.ToList())
            {
                if (!_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype prototype))
                {
                    continue;
                }

                // Run metabolism code for each reagent
                foreach (var metabolizable in prototype.Metabolism)
                {
                    var reagentDelta = metabolizable.Metabolize(Owner, reagent.ReagentId, frameTime);
                    bloodstream.Solution.TryRemoveReagent(reagent.ReagentId, reagentDelta);
                }
            }
        }

        /// <summary>
        ///     Processes gases in the bloodstream and triggers metabolism of the
        ///     reagents inside of it.
        /// </summary>
        /// <param name="frameTime">
        ///     The time since the last metabolism tick in seconds.
        /// </param>
        public void Update(float frameTime)
        {
            if (!Owner.TryGetComponent<IMobStateComponent>(out var state) ||
                state.IsDead())
            {
                return;
            }

            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime < 1)
            {
                return;
            }

            ProcessGases(_accumulatedFrameTime);
            ProcessNutrients(_accumulatedFrameTime);
            ProcessThermalRegulation(_accumulatedFrameTime);

            _accumulatedFrameTime -= 1;

            if (SuffocatingPercentage() > 0)
            {
                TakeSuffocationDamage();
                return;
            }

            StopSuffocation();
        }

        private void TakeSuffocationDamage()
        {
            Suffocating = true;

            if (!Owner.TryGetComponent(out IDamageableComponent? damageable))
            {
                return;
            }

            damageable.ChangeDamage(DamageClass.Airloss, _suffocationDamage, false);
        }

        private void StopSuffocation()
        {
            Suffocating = false;
        }

        public GasMixture Clean(BloodstreamComponent bloodstream)
        {
            var gasMixture = new GasMixture(bloodstream.Air.Volume)
            {
                Temperature = bloodstream.Air.Temperature
            };

            for (Gas gas = 0; gas < (Gas) Atmospherics.TotalNumberOfGases; gas++)
            {
                float amount;
                var molesInBlood = bloodstream.Air.GetMoles(gas);

                if (!NeedsGases.TryGetValue(gas, out var needed))
                {
                    amount = molesInBlood;
                }
                else
                {
                    var overflowThreshold = needed * 5f;

                    amount = molesInBlood > overflowThreshold
                        ? molesInBlood - overflowThreshold
                        : 0;
                }

                gasMixture.AdjustMoles(gas, amount);
                bloodstream.Air.AdjustMoles(gas, -amount);
            }

            return gasMixture;
        }
    }
}
