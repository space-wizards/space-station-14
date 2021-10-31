using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Alert;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.EntitySystems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.MobState;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBloodstreamComponent))]
    public class BloodstreamComponent : SharedBloodstreamComponent, IGasMixtureHolder
    {
        public override string Name => "Bloodstream";

        [ComponentDependency] private readonly SharedBodyComponent? _body = default!;

        /// <summary>
        ///     Max volume of internal solution storage
        /// </summary>
        [DataField("maxVolume")]
        public ReagentUnit MaxVolume = ReagentUnit.New(250);

        public float AccumulatedFrametime = 0f;

        /// <summary>
        ///     Internal solution for reagent storage
        /// </summary>
        public Solution InternalSolution = default!;

        [ViewVariables] [DataField("needsGases")] public Dictionary<Gas, float> NeedsGases { get; set; } = new();

        [ViewVariables] [DataField("producesGases")] public Dictionary<Gas, float> ProducesGases { get; set; } = new();

        [ViewVariables] [DataField("deficitGases")] public Dictionary<Gas, float> DeficitGases { get; set; } = new();

        [ViewVariables] public bool Suffocating { get; private set; }

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("damageRecovery", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageRecovery = default!;

        [ViewVariables]
        public GasMixture Air { get; set; } = new(6)
            {Temperature = Atmospherics.NormalBodyTemperature};

        protected override void Initialize()
        {
            base.Initialize();

            InternalSolution = EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(Owner, DefaultSolutionName);
            InternalSolution.MaxVolume = MaxVolume;
        }

        /// <summary>
        ///     Attempt to transfer provided solution to internal solution.
        ///     Only supports complete transfers
        /// </summary>
        /// <param name="solution">Solution to be transferred</param>
        /// <returns>Whether or not transfer was a success</returns>
        public override bool TryTransferSolution(Solution solution)
        {
            // For now doesn't support partial transfers
            var current = InternalSolution.CurrentVolume;
            var max = InternalSolution.MaxVolume;
            if (solution.TotalVolume + current > max)
            {
                return false;
            }

            EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(Owner.Uid, InternalSolution, solution);
            return true;
        }

        public void PumpToxins(GasMixture to)
        {
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            var toxins = Clean(this);
            var toOld = new float[to.Moles.Length];
            Array.Copy(to.Moles, toOld, toOld.Length);

            atmosphereSystem.Merge(to, toxins);

            for (var i = 0; i < toOld.Length; i++)
            {
                var newAmount = to.GetMoles(i);
                var oldAmount = toOld[i];
                var delta = newAmount - oldAmount;

                toxins.AdjustMoles(i, -delta);
            }

            atmosphereSystem.Merge(Air, toxins);
        }

        public Dictionary<Gas, float> NeedsAndDeficit(float frameTime)
        {
            var needs = new Dictionary<Gas, float>(NeedsGases);
            foreach (var (gas, amount) in DeficitGases)
            {
                var newAmount = (needs.GetValueOrDefault(gas) + amount) * frameTime;
                needs[gas] = newAmount;
            }

            return needs;
        }

        public void ClampDeficit()
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

        public float SuffocatingPercentage()
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

        public float GasProducedMultiplier(Gas gas, float usedAverage)
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

        public Dictionary<Gas, float> GasProduced(float usedAverage)
        {
            return ProducesGases.ToDictionary(pair => pair.Key, pair => GasProducedMultiplier(pair.Key, usedAverage));
        }

        public void ProcessGases(float frameTime)
        {
            if (!Owner.TryGetComponent(out BloodstreamComponent? bloodstream))
            {
                return;
            }

            if (_body == null)
            {
                return;
            }

            // TODO MIRROR remove and events it instead
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

        public void TakeSuffocationDamage()
        {
            Suffocating = true;

            if (Owner.TryGetComponent(out ServerAlertsComponent? alertsComponent))
            {
                alertsComponent.ShowAlert(AlertType.LowOxygen);
            }

            EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner.Uid, Damage, true);
        }

        public void StopSuffocation()
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
