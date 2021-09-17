using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Alert;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Behavior;
using Content.Server.Body.Components;
using Content.Server.Body.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.MobState;
using Content.Shared.Notification.Managers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    /// <summary>
    ///     Handles body temperature
    /// </summary>
    [RegisterComponent]
    public class ThermalRegulatorComponent : Component
    {
        [ComponentDependency] private readonly SharedBodyComponent? _body = default!;

        public override string Name => "ThermalRegulator";

        public float AccumulatedFrametime;

        public bool IsShivering;
        public bool IsSweating;

        // TODO MIRROR move to bloodstream
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

        // TODO MIRROR move to bloodstream
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

            // TODO MIRROR remove
            var lungs = EntitySystem.Get<BodySystem>().GetComponentsOnMechanisms<RespiratorComponent>(_body);

            var needs = NeedsAndDeficit(frameTime);
            var used = 0f;
            foreach (var (gas, amountNeeded) in needs)
            {
                var bloodstreamAmount = bloodstream.Air.GetMoles(gas);
                var deficit = 0f;

                if (bloodstreamAmount < amountNeeded)
                {
                    if (!Owner.GetComponent<IMobStateComponent>().IsCritical())
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

        private void TakeSuffocationDamage()
        {
            Suffocating = true;

            if (Owner.TryGetComponent(out ServerAlertsComponent? alertsComponent))
            {
                alertsComponent.ShowAlert(AlertType.LowOxygen);
            }

            EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner.Uid, Damage);
        }

        private void StopSuffocation()
        {
            Suffocating = false;

            if (Owner.TryGetComponent(out ServerAlertsComponent? alertsComponent))
            {
                alertsComponent.ClearAlert(AlertType.LowOxygen);
            }

            EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner.Uid, DamageRecovery);
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
