using System;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Popups;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.MobState.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems
{
    [UsedImplicitly]
    public class RespiratorSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSys = default!;
        [Dependency] private readonly AdminLogSystem _logSys = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly LungSystem _lungSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSys = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            // We want to process lung reagents before we inhale new reagents.
            UpdatesAfter.Add(typeof(MetabolizerSystem));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (respirator, body) in
                     EntityManager.EntityQuery<RespiratorComponent, SharedBodyComponent>())
            {
                var uid = respirator.Owner;
                if (!EntityManager.TryGetComponent<MobStateComponent>(uid, out var state) ||
                    state.IsDead())
                {
                    continue;
                }

                respirator.AccumulatedFrametime += frameTime;

                if (respirator.AccumulatedFrametime < respirator.CycleDelay)
                    continue;
                respirator.AccumulatedFrametime -= respirator.CycleDelay;
                UpdateSaturation(respirator.Owner, -respirator.CycleDelay, respirator);

                if (!state.IsIncapacitated()) // cannot breathe in crit.
                {
                    switch (respirator.Status)
                    {
                        case RespiratorStatus.Inhaling:
                            Inhale(uid, body);
                            respirator.Status = RespiratorStatus.Exhaling;
                            break;
                        case RespiratorStatus.Exhaling:
                            Exhale(uid, body);
                            respirator.Status = RespiratorStatus.Inhaling;
                            break;
                    }
                }

                if (respirator.Saturation < respirator.SuffocationThreshold)
                {
                    if (_gameTiming.CurTime >= respirator.LastGaspPopupTime + respirator.GaspPopupCooldown)
                    {
                        respirator.LastGaspPopupTime = _gameTiming.CurTime;
                        _popupSystem.PopupEntity(Loc.GetString("lung-behavior-gasp"), uid, Filter.Pvs(uid));
                    }

                    TakeSuffocationDamage(uid, respirator);
                    respirator.SuffocationCycles += 1;
                    continue;
                }

                StopSuffocation(uid, respirator);
                respirator.SuffocationCycles = 0;
            }
        }

        public void Inhale(EntityUid uid, SharedBodyComponent? body=null)
        {
            if (!Resolve(uid, ref body, false))
                return;

            var organs = _bodySystem.GetComponentsOnMechanisms<LungComponent>(uid, body).ToArray();

            // Inhale gas
            var ev = new InhaleLocationEvent();
            RaiseLocalEvent(uid, ev, false);

            if (ev.Gas == null)
            {
                ev.Gas = _atmosSys.GetTileMixture(Transform(uid).Coordinates);
                if (ev.Gas == null) return;
            }

            var ratio = (Atmospherics.BreathVolume / ev.Gas.Volume);
            var actualGas = ev.Gas.RemoveRatio(ratio);

            var lungRatio = 1.0f / organs.Length;
            var gas = organs.Length == 1 ? actualGas : actualGas.RemoveRatio(lungRatio);
            foreach (var (lung, _) in organs)
            {
                // Merge doesn't remove gas from the giver.
                _atmosSys.Merge(lung.Air, gas);
                _lungSystem.GasToReagent(lung.Owner, lung);
            }
        }

        public void Exhale(EntityUid uid, SharedBodyComponent? body=null)
        {
            if (!Resolve(uid, ref body, false))
                return;

            var organs = _bodySystem.GetComponentsOnMechanisms<LungComponent>(uid, body).ToArray();

            // exhale gas

            var ev = new ExhaleLocationEvent();
            RaiseLocalEvent(uid, ev, false);

            if (ev.Gas == null)
            {
                ev.Gas = _atmosSys.GetTileMixture(Transform(uid).Coordinates);

                // Walls and grids without atmos comp return null. I guess it makes sense to not be able to exhale in walls,
                // but this also means you cannot exhale on some grids.
                ev.Gas ??= GasMixture.SpaceGas;
            }

            var outGas = new GasMixture(ev.Gas.Volume);
            foreach (var (lung, _) in organs)
            {
                _atmosSys.Merge(outGas, lung.Air);
                lung.Air.Clear();
                lung.LungSolution.RemoveAllSolution();
            }

            _atmosSys.Merge(ev.Gas, outGas);
        }

        private void TakeSuffocationDamage(EntityUid uid, RespiratorComponent respirator)
        {
            if (respirator.SuffocationCycles == 2)
                _logSys.Add(LogType.Asphyxiation, $"{ToPrettyString(uid):entity} started suffocating");

            if (respirator.SuffocationCycles >= respirator.SuffocationCycleThreshold)
            {
                _alertsSystem.ShowAlert(uid, AlertType.LowOxygen);
            }

            _damageableSys.TryChangeDamage(uid, respirator.Damage, true, false);
        }

        private void StopSuffocation(EntityUid uid, RespiratorComponent respirator)
        {
            if (respirator.SuffocationCycles >= 2)
                _logSys.Add(LogType.Asphyxiation, $"{ToPrettyString(uid):entity} stopped suffocating");

            _alertsSystem.ClearAlert(uid, AlertType.LowOxygen);

            _damageableSys.TryChangeDamage(uid, respirator.DamageRecovery, true);
        }

        public void UpdateSaturation(EntityUid uid, float amount,
            RespiratorComponent? respirator = null)
        {
            if (!Resolve(uid, ref respirator, false))
                return;

            respirator.Saturation += amount;
            respirator.Saturation =
                Math.Clamp(respirator.Saturation, respirator.MinSaturation, respirator.MaxSaturation);
        }
    }
}

public class InhaleLocationEvent : EntityEventArgs
{
    public GasMixture? Gas;
}

public class ExhaleLocationEvent : EntityEventArgs
{
    public GasMixture? Gas;
}
