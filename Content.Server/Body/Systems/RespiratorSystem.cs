using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Alert;
using Content.Server.Atmos;
using Content.Server.Body.Components;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.MobState.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Body.Systems
{
    [UsedImplicitly]
    public class RespiratorSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSys = default!;
        [Dependency] private readonly AdminLogSystem _logSys = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly LungSystem _lungSystem = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (respirator, blood, body) in
                     EntityManager.EntityQuery<RespiratorComponent, BloodstreamComponent, SharedBodyComponent>())
            {
                var uid = respirator.Owner;
                if (!EntityManager.TryGetComponent<MobStateComponent>(uid, out var state) ||
                    state.IsDead())
                {
                    continue;
                }

                respirator.AccumulatedFrametime += frameTime;

                if (respirator.AccumulatedFrametime < 1)
                {
                    continue;
                }

                ProcessGases(uid, respirator, blood, body);

                respirator.AccumulatedFrametime -= 1;

                if (SuffocatingPercentage(respirator) > 0)
                {
                    TakeSuffocationDamage(uid, respirator);
                    continue;
                }

                StopSuffocation(uid, respirator);
            }
        }

        private Dictionary<Gas, float> NeedsAndDeficit(RespiratorComponent respirator)
        {
            var needs = new Dictionary<Gas, float>(respirator.NeedsGases);
            foreach (var (gas, amount) in respirator.DeficitGases)
            {
                var newAmount = (needs.GetValueOrDefault(gas) + amount);
                needs[gas] = newAmount;
            }

            return needs;
        }

        private void ClampDeficit(RespiratorComponent respirator)
        {
            var deficitGases = new Dictionary<Gas, float>(respirator.DeficitGases);

            foreach (var (gas, deficit) in deficitGases)
            {
                if (!respirator.NeedsGases.TryGetValue(gas, out var need))
                {
                    respirator.DeficitGases.Remove(gas);
                    continue;
                }

                if (deficit > need)
                {
                    respirator.DeficitGases[gas] = need;
                }
            }
        }

        private float SuffocatingPercentage(RespiratorComponent respirator)
        {
            var total = 0f;

            foreach (var (gas, deficit) in respirator.DeficitGases)
            {
                var lack = 1f;
                if (respirator.NeedsGases.TryGetValue(gas, out var needed))
                {
                    lack = deficit / needed;
                }

                total += lack / Atmospherics.TotalNumberOfGases;
            }

            return total;
        }

        private float GasProducedMultiplier(RespiratorComponent respirator, Gas gas, float usedAverage)
        {
            if (!respirator.ProducesGases.TryGetValue(gas, out var produces))
            {
                return 0;
            }

            if (!respirator.NeedsGases.TryGetValue(gas, out var needs))
            {
                needs = 1;
            }

            return needs * produces * usedAverage;
        }

        private Dictionary<Gas, float> GasProduced(RespiratorComponent respirator, float usedAverage)
        {
            return respirator.ProducesGases.ToDictionary(pair => pair.Key, pair => GasProducedMultiplier(respirator, pair.Key, usedAverage));
        }

        private void ProcessGases(EntityUid uid, RespiratorComponent respirator,
            BloodstreamComponent? bloodstream,
            SharedBodyComponent? body)
        {
            if (!Resolve(uid, ref bloodstream, ref body, false))
                return;

            var lungs = _bodySystem.GetComponentsOnMechanisms<LungComponent>(uid, body).ToArray();

            var needs = NeedsAndDeficit(respirator);
            var used = 0f;

            foreach (var (lung, mech) in lungs)
            {
                _lungSystem.UpdateLung(lung.Owner, lung, mech);
            }

            foreach (var (gas, amountNeeded) in needs)
            {
                var bloodstreamAmount = bloodstream.Air.GetMoles(gas);
                var deficit = 0f;

                if (bloodstreamAmount < amountNeeded)
                {
                    // Panic inhale
                    foreach (var (lung, mech) in lungs)
                    {
                        _lungSystem.Gasp((lung).Owner, lung, mech);
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

                respirator.DeficitGases[gas] = deficit;

                used += (amountNeeded - deficit) / amountNeeded;
            }

            var produced = GasProduced(respirator, used / needs.Count);

            foreach (var (gas, amountProduced) in produced)
            {
                bloodstream.Air.AdjustMoles(gas, amountProduced);
            }

            ClampDeficit(respirator);
        }

        private void TakeSuffocationDamage(EntityUid uid, RespiratorComponent respirator)
        {
            if (!respirator.Suffocating)
                _logSys.Add(LogType.Asphyxiation, $"{ToPrettyString(uid):entity} started suffocating");

            respirator.Suffocating = true;

            if (EntityManager.TryGetComponent(uid, out ServerAlertsComponent? alertsComponent))
            {
                alertsComponent.ShowAlert(AlertType.LowOxygen);
            }

            _damageableSys.TryChangeDamage(uid, respirator.Damage, true, false);
        }

        private void StopSuffocation(EntityUid uid, RespiratorComponent respirator)
        {
            if (respirator.Suffocating)
                _logSys.Add(LogType.Asphyxiation, $"{ToPrettyString(uid):entity} stopped suffocating");

            respirator.Suffocating = false;

            if (EntityManager.TryGetComponent(uid, out ServerAlertsComponent? alertsComponent))
            {
                alertsComponent.ClearAlert(AlertType.LowOxygen);
            }

            _damageableSys.TryChangeDamage(uid, respirator.DamageRecovery, true);
        }

        public GasMixture Clean(EntityUid uid, RespiratorComponent respirator, BloodstreamComponent bloodstream)
        {
            var gasMixture = new GasMixture(bloodstream.Air.Volume)
            {
                Temperature = bloodstream.Air.Temperature
            };

            for (Gas gas = 0; gas < (Gas) Atmospherics.TotalNumberOfGases; gas++)
            {
                float amount;
                var molesInBlood = bloodstream.Air.GetMoles(gas);

                if (!respirator.NeedsGases.TryGetValue(gas, out var needed))
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
