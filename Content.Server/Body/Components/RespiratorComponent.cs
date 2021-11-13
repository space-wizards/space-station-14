using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Alert;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Behavior;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    public class RespiratorComponent : Component
    {
        [ComponentDependency] private readonly SharedBodyComponent? _body = default!;

        public override string Name => "Respirator";

        private float _accumulatedFrameTime;

        private bool _isShivering;
        private bool _isSweating;

        [ViewVariables] [DataField("needsGases")] public Dictionary<Gas, float> NeedsGases { get; set; } = new();

        [ViewVariables] [DataField("producesGases")] public Dictionary<Gas, float> ProducesGases { get; set; } = new();

        [ViewVariables] [DataField("deficitGases")] public Dictionary<Gas, float> DeficitGases { get; set; } = new();

        /// <summary>
        /// Heat generated due to metabolism. It's generated via metabolism
        /// </summary>
        [ViewVariables]
        [DataField("metabolismHeat")]
        public float MetabolismHeat { get; private set; }

        /// <summary>
        /// Heat output via radiation.
        /// </summary>
        [ViewVariables]
        [DataField("radiatedHeat")]
        public float RadiatedHeat { get; private set; }

        /// <summary>
        /// Maximum heat regulated via sweat
        /// </summary>
        [ViewVariables]
        [DataField("sweatHeatRegulation")]
        public float SweatHeatRegulation { get; private set; }

        /// <summary>
        /// Maximum heat regulated via shivering
        /// </summary>
        [ViewVariables]
        [DataField("shiveringHeatRegulation")]
        public float ShiveringHeatRegulation { get; private set; }

        /// <summary>
        /// Amount of heat regulation that represents thermal regulation processes not
        /// explicitly coded.
        /// </summary>
        [DataField("implicitHeatRegulation")]
        public float ImplicitHeatRegulation { get; private set; }

        /// <summary>
        /// Normal body temperature
        /// </summary>
        [ViewVariables]
        [DataField("normalBodyTemperature")]
        public float NormalBodyTemperature { get; private set; }

        /// <summary>
        /// Deviation from normal temperature for body to start thermal regulation
        /// </summary>
        [DataField("thermalRegulationTemperatureThreshold")]
        public float ThermalRegulationTemperatureThreshold { get; private set; }

        [ViewVariables] public bool Suffocating { get; private set; }

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("damageRecovery", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageRecovery = default!;

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
            var total = 0f;

            foreach (var (gas, deficit) in DeficitGases)
            {
                var lack = 1f;
                if (NeedsGases.TryGetValue(gas, out var needed))
                {
                    lack = deficit / needed;
                }

                total += lack / Atmospherics.TotalNumberOfGases;
            }

            return total;
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
                    if (!Owner.GetComponent<MobStateComponent>().IsCritical())
                    {
                        // Panic inhale
                        foreach (var lung in lungs)
                        {
                            lung.Gasp();
                        }
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
            var temperatureSystem = EntitySystem.Get<TemperatureSystem>();
            if (!Owner.TryGetComponent(out TemperatureComponent? temperatureComponent)) return;
            temperatureSystem.ReceiveHeat(Owner.Uid, MetabolismHeat, temperatureComponent);
            temperatureSystem.RemoveHeat(Owner.Uid, RadiatedHeat, temperatureComponent);

            // implicit heat regulation
            var tempDiff = Math.Abs(temperatureComponent.CurrentTemperature - NormalBodyTemperature);
            var targetHeat = tempDiff * temperatureComponent.HeatCapacity;
            if (temperatureComponent.CurrentTemperature > NormalBodyTemperature)
            {
                temperatureSystem.RemoveHeat(Owner.Uid, Math.Min(targetHeat, ImplicitHeatRegulation), temperatureComponent);
            }
            else
            {
                temperatureSystem.ReceiveHeat(Owner.Uid, Math.Min(targetHeat, ImplicitHeatRegulation), temperatureComponent);
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
                    Owner.PopupMessage(Loc.GetString("metabolism-component-is-comfortable"));
                }

                _isShivering = false;
                _isSweating = false;
                return;
            }


            var actionBlocker = EntitySystem.Get<ActionBlockerSystem>();

            if (temperatureComponent.CurrentTemperature > NormalBodyTemperature)
            {
                if (!actionBlocker.CanSweat(OwnerUid)) return;
                if (!_isSweating)
                {
                    Owner.PopupMessage(Loc.GetString("metabolism-component-is-sweating"));
                    _isSweating = true;
                }

                // creadth: sweating does not help in airless environment
                if (EntitySystem.Get<AtmosphereSystem>().GetTileMixture(Owner.Transform.Coordinates) is not {})
                {
                    temperatureSystem.RemoveHeat(OwnerUid, Math.Min(targetHeat, SweatHeatRegulation), temperatureComponent);
                }
            }
            else
            {
                if (!actionBlocker.CanShiver(OwnerUid)) return;
                if (!_isShivering)
                {
                    Owner.PopupMessage(Loc.GetString("metabolism-component-is-shivering"));
                    _isShivering = true;
                }

                temperatureSystem.ReceiveHeat(OwnerUid, Math.Min(targetHeat, ShiveringHeatRegulation), temperatureComponent);
            }
        }

        /// <summary>
        ///     Processes gases in the bloodstream.
        /// </summary>
        /// <param name="frameTime">
        ///     The time since the last metabolism tick in seconds.
        /// </param>
        public void Update(float frameTime)
        {
            if (!Owner.TryGetComponent<MobStateComponent>(out var state) ||
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

            if (Owner.TryGetComponent(out ServerAlertsComponent? alertsComponent))
            {
                alertsComponent.ShowAlert(AlertType.LowOxygen);
            }

            EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner.Uid, Damage, true);
        }

        private void StopSuffocation()
        {
            Suffocating = false;

            if (Owner.TryGetComponent(out ServerAlertsComponent? alertsComponent))
            {
                alertsComponent.ClearAlert(AlertType.LowOxygen);
            }

            EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner.Uid, DamageRecovery, true);
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
