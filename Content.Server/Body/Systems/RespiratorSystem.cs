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
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems
{
    [UsedImplicitly]
    public sealed class RespiratorSystem : EntitySystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSys = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSys = default!;
        [Dependency] private readonly LungSystem _lungSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;

        public override void Initialize()
        {
            base.Initialize();

            // We want to process lung reagents before we inhale new reagents.
            UpdatesAfter.Add(typeof(MetabolizerSystem));
            SubscribeLocalEvent<RespiratorComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<RespiratorComponent, BodyComponent>();
            while (query.MoveNext(out var uid, out var respirator, out var body))
            {
                if (_mobState.IsDead(uid))
                {
                    continue;
                }

                respirator.AccumulatedFrametime += frameTime;

                if (respirator.AccumulatedFrametime < respirator.CycleDelay)
                    continue;
                respirator.AccumulatedFrametime -= respirator.CycleDelay;
                UpdateSaturation(uid, -respirator.CycleDelay, respirator);

                if (!_mobState.IsIncapacitated(uid)) // cannot breathe in crit.
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
                        _popupSystem.PopupEntity(Loc.GetString("lung-behavior-gasp"), uid);
                    }

                    TakeSuffocationDamage(uid, respirator);
                    respirator.SuffocationCycles += 1;
                    continue;
                }

                StopSuffocation(uid, respirator);
                respirator.SuffocationCycles = 0;
            }
        }

        public void Inhale(EntityUid uid, BodyComponent? body = null)
        {
            if (!Resolve(uid, ref body, false))
                return;

            var organs = _bodySystem.GetBodyOrganComponents<LungComponent>(uid, body);

            // Inhale gas
            var ev = new InhaleLocationEvent();
            RaiseLocalEvent(uid, ev);

            ev.Gas ??= _atmosSys.GetContainingMixture(uid, false, true);

            if (ev.Gas == null)
            {
                return;
            }

            var actualGas = ev.Gas.RemoveVolume(Atmospherics.BreathVolume);

            var lungRatio = 1.0f / organs.Count;
            var gas = organs.Count == 1 ? actualGas : actualGas.RemoveRatio(lungRatio);
            foreach (var (lung, _) in organs)
            {
                // Merge doesn't remove gas from the giver.
                _atmosSys.Merge(lung.Air, gas);
                _lungSystem.GasToReagent(lung.Owner, lung);
            }
        }

        public void Exhale(EntityUid uid, BodyComponent? body = null)
        {
            if (!Resolve(uid, ref body, false))
                return;

            var organs = _bodySystem.GetBodyOrganComponents<LungComponent>(uid, body);

            // exhale gas

            var ev = new ExhaleLocationEvent();
            RaiseLocalEvent(uid, ev, false);

            if (ev.Gas == null)
            {
                ev.Gas = _atmosSys.GetContainingMixture(uid, false, true);

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
                _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(uid):entity} started suffocating");

            if (respirator.SuffocationCycles >= respirator.SuffocationCycleThreshold)
            {
                _alertsSystem.ShowAlert(uid, AlertType.LowOxygen);
            }

            _damageableSys.TryChangeDamage(uid, respirator.Damage, false, false);
        }

        private void StopSuffocation(EntityUid uid, RespiratorComponent respirator)
        {
            if (respirator.SuffocationCycles >= 2)
                _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(uid):entity} stopped suffocating");

            _alertsSystem.ClearAlert(uid, AlertType.LowOxygen);

            _damageableSys.TryChangeDamage(uid, respirator.DamageRecovery);
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

        private void OnApplyMetabolicMultiplier(EntityUid uid, RespiratorComponent component,
            ApplyMetabolicMultiplierEvent args)
        {
            if (args.Apply)
            {
                component.CycleDelay *= args.Multiplier;
                component.Saturation *= args.Multiplier;
                component.MaxSaturation *= args.Multiplier;
                component.MinSaturation *= args.Multiplier;
                return;
            }

            // This way we don't have to worry about it breaking if the stasis bed component is destroyed
            component.CycleDelay /= args.Multiplier;
            component.Saturation /= args.Multiplier;
            component.MaxSaturation /= args.Multiplier;
            component.MinSaturation /= args.Multiplier;
            // Reset the accumulator properly
            if (component.AccumulatedFrametime >= component.CycleDelay)
                component.AccumulatedFrametime = component.CycleDelay;
        }
    }
}

public sealed class InhaleLocationEvent : EntityEventArgs
{
    public GasMixture? Gas;
}

public sealed class ExhaleLocationEvent : EntityEventArgs
{
    public GasMixture? Gas;
}
