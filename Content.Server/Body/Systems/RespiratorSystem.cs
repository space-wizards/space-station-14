using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.DoAfter;
using Content.Shared.DoAfter;
using Content.Server.Popups;
using Content.Server.Abilities;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Inventory;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.ActionBlocker;
using Content.Shared.Mobs.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Examine;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Robust.Shared.Physics.Components;
using static Content.Shared.Examine.ExamineSystemShared;

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
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;

        public override void Initialize()
        {
            base.Initialize();

            // We want to process lung reagents before we inhale new reagents.
            UpdatesAfter.Add(typeof(MetabolizerSystem));
            SubscribeLocalEvent<RespiratorComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
            SubscribeLocalEvent<RespiratorComponent, CPRDoAfterEvent>(OnDoAfter);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (respirator, body) in EntityManager.EntityQuery<RespiratorComponent, BodyComponent>())
            {
                var uid = respirator.Owner;

                if (_mobState.IsDead(uid))
                {
                    continue;
                }

                respirator.AccumulatedFrametime += frameTime;

                if (respirator.AccumulatedFrametime < respirator.CycleDelay)
                    continue;
                respirator.AccumulatedFrametime -= respirator.CycleDelay;
                UpdateSaturation(respirator.Owner, -respirator.CycleDelay, respirator);

                if (!_mobState.IsIncapacitated(uid) || respirator.BreatheInCritCounter > 0) // cannot breathe in crit.
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

                    respirator.BreatheInCritCounter = Math.Clamp(respirator.BreatheInCritCounter - 1, 0, 6);
                }

                if (respirator.Saturation < respirator.SuffocationThreshold)
                {
                    if (_gameTiming.CurTime >= respirator.LastGaspPopupTime + respirator.GaspPopupCooldown)
                    {
                        respirator.LastGaspPopupTime = _gameTiming.CurTime;
                        // TODO: remove proper occlusion
                        _popupSystem.PopupEntity(Loc.GetString("lung-behavior-gasp"), uid,
                        Filter.Pvs(uid).RemoveWhereAttachedEntity(entity => !ExamineSystemShared.InRangeUnOccluded(respirator.Owner, entity, ExamineRange, null)),
                        true);
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
            RaiseLocalEvent(uid, ev, false);

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

            _damageableSys.TryChangeDamage(uid, respirator.Damage, true, false);
        }

        private void StopSuffocation(EntityUid uid, RespiratorComponent respirator)
        {
            if (respirator.SuffocationCycles >= 2)
                _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(uid):entity} stopped suffocating");

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

        private void OnDoAfter(EntityUid uid, RespiratorComponent component, CPRDoAfterEvent args)
        {
            component.CPRPlayingStream?.Stop();
            component.IsReceivingCPR = false;

            if (args.Handled || args.Cancelled)
                return;

            component.BreatheInCritCounter = component.BreatheInCritCounter + 3;

            _popupSystem.PopupEntity(Loc.GetString("cpr-end-pvs", ("user", args.Args.User), ("target", uid)), uid, Shared.Popups.PopupType.Medium);
            args.Handled = true;
        }

        /// <summary>
        /// Attempt CPR, which will keep the user breathing even in crit.
        /// As cardiac arrest is currently unsimulated, the damage taken in crit is a function of
        /// respiration alone. This may change in the future.
        /// </summary>
        public void AttemptCPR(EntityUid uid, RespiratorComponent component, EntityUid user)
        {
            if (!_blocker.CanInteract(user, uid))
                return;

            if (component.IsReceivingCPR)
                return;

            if (_inventory.TryGetSlotEntity(uid, "outerClothing", out var outer))
            {
                _popupSystem.PopupEntity(Loc.GetString("cpr-must-remove", ("clothing", outer)), uid, user, Shared.Popups.PopupType.MediumCaution);
                return;
            }

            if (_inventory.TryGetSlotEntity(uid, "belt", out var belt) && _tag.HasTag(belt.Value, "BeltSlotNotBelt"))
            {
                _popupSystem.PopupEntity(Loc.GetString("cpr-must-remove", ("clothing", belt)), uid, user, Shared.Popups.PopupType.MediumCaution);
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("cpr-start-second-person", ("target", Identity.Entity(uid, EntityManager))), uid, user, Shared.Popups.PopupType.Medium);
            _popupSystem.PopupEntity(Loc.GetString("cpr-start-second-person-patient", ("user", Identity.Entity(user, EntityManager))), uid, uid, Shared.Popups.PopupType.Medium);

            component.IsReceivingCPR = true;
            component.CPRPlayingStream = _audio.PlayPvs(component.CPRSound, uid, audioParams: AudioParams.Default.WithVolume(-3f));

            var ev = new CPRDoAfterEvent();
            var args = new DoAfterArgs(user, Math.Min(component.CycleDelay * 2, 6f), ev, uid, target: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

            _doAfter.TryStartDoAfter(args);
        }

        /// <summary>
        /// Used mostly to prevent doafter conflicts on entities with a metric fuckton of doafters.
        /// </summary>
        public bool IsReceivingCPR(EntityUid uid, RespiratorComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return false;

            return component.IsReceivingCPR;
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
