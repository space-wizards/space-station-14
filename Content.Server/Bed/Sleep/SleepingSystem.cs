using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.Sound.Components;
using Content.Shared.Audio;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Slippery;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Bed.Sleep
{
    public sealed class SleepingSystem : SharedSleepingSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

        [ValidatePrototypeId<EntityPrototype>] public const string SleepActionId = "ActionSleep";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MobStateComponent, SleepStateChangedEvent>(OnSleepStateChanged);
            SubscribeLocalEvent<SleepingComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<MobStateComponent, SleepActionEvent>(OnSleepAction);
            SubscribeLocalEvent<MobStateComponent, WakeActionEvent>(OnWakeAction);
            SubscribeLocalEvent<SleepingComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<SleepingComponent, GetVerbsEvent<AlternativeVerb>>(AddWakeVerb);
            SubscribeLocalEvent<SleepingComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<SleepingComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<SleepingComponent, SlipAttemptEvent>(OnSlip);
            SubscribeLocalEvent<ForcedSleepingComponent, ComponentInit>(OnInit);
        }

        /// <summary>
        /// when sleeping component is added or removed, we do some stuff with other components.
        /// </summary>
        private void OnSleepStateChanged(EntityUid uid, MobStateComponent component, SleepStateChangedEvent args)
        {
            if (args.FellAsleep)
            {
                // Expiring status effects would remove the components needed for sleeping
                _statusEffectsSystem.TryRemoveStatusEffect(uid, "Stun");
                _statusEffectsSystem.TryRemoveStatusEffect(uid, "KnockedDown");

                EnsureComp<StunnedComponent>(uid);
                EnsureComp<KnockedDownComponent>(uid);

                if (TryComp<SleepEmitSoundComponent>(uid, out var sleepSound))
                {
                    var emitSound = EnsureComp<SpamEmitSoundComponent>(uid);
                    emitSound.Sound = sleepSound.Snore;
                    emitSound.PlayChance = sleepSound.Chance;
                    emitSound.RollInterval = sleepSound.Interval;
                    emitSound.PopUp = sleepSound.PopUp;
                }

                return;
            }

            RemComp<StunnedComponent>(uid);
            RemComp<KnockedDownComponent>(uid);
            RemComp<SpamEmitSoundComponent>(uid);
        }

        /// <summary>
        /// Wake up if we take an instance of more than 2 damage.
        /// </summary>
        private void OnDamageChanged(EntityUid uid, SleepingComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased || args.DamageDelta == null)
                return;

            if (args.DamageDelta.Total >= component.WakeThreshold)
                TryWaking(uid, component);
        }

        private void OnSleepAction(EntityUid uid, MobStateComponent component, SleepActionEvent args)
        {
            TrySleeping(uid);
        }

        private void OnWakeAction(EntityUid uid, MobStateComponent component, WakeActionEvent args)
        {
            if (!TryWakeCooldown(uid))
                return;

            if (TryWaking(uid))
                args.Handled = true;
        }

        /// <summary>
        /// In crit, we wake up if we are not being forced to sleep.
        /// And, you can't sleep when dead...
        /// </summary>
        private void OnMobStateChanged(EntityUid uid, SleepingComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Dead)
            {
                RemComp<SpamEmitSoundComponent>(uid);
                RemComp<SleepingComponent>(uid);
                return;
            }
            if (TryComp<SpamEmitSoundComponent>(uid, out var spam))
                spam.Enabled = args.NewMobState == MobState.Alive;
        }

        private void AddWakeVerb(EntityUid uid, SleepingComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    if (!TryWakeCooldown(uid))
                        return;

                    TryWaking(args.Target, user: args.User);
                },
                Text = Loc.GetString("action-name-wake"),
                Priority = 2
            };

            args.Verbs.Add(verb);
        }

        /// <summary>
        /// When you click on a sleeping person with an empty hand, try to wake them.
        /// </summary>
        private void OnInteractHand(EntityUid uid, SleepingComponent component, InteractHandEvent args)
        {
            args.Handled = true;

            if (!TryWakeCooldown(uid))
                return;

            TryWaking(args.Target, user: args.User);
        }

        private void OnExamined(EntityUid uid, SleepingComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                args.PushMarkup(Loc.GetString("sleep-examined", ("target", Identity.Entity(uid, EntityManager))));
            }
        }

        private void OnSlip(EntityUid uid, SleepingComponent component, SlipAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnInit(EntityUid uid, ForcedSleepingComponent component, ComponentInit args)
        {
            TrySleeping(uid);
        }

        /// <summary>
        /// Try sleeping. Only mobs can sleep.
        /// </summary>
        public bool TrySleeping(EntityUid uid)
        {
            if (!HasComp<MobStateComponent>(uid))
                return false;

            var tryingToSleepEvent = new TryingToSleepEvent(uid);
            RaiseLocalEvent(uid, ref tryingToSleepEvent);
            if (tryingToSleepEvent.Cancelled)
                return false;

            EnsureComp<SleepingComponent>(uid);
            return true;
        }

        private bool TryWakeCooldown(EntityUid uid, SleepingComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return false;

            var curTime = _gameTiming.CurTime;

            if (curTime < component.CoolDownEnd)
            {
                return false;
            }

            component.CoolDownEnd = curTime + component.Cooldown;
            return true;
        }

        /// <summary>
        /// Try to wake up.
        /// </summary>
        public bool TryWaking(EntityUid uid, SleepingComponent? component = null, bool force = false, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component, false))
                return false;

            if (!force && HasComp<ForcedSleepingComponent>(uid))
            {
                if (user != null)
                {
                    _audio.PlayPvs("/Audio/Effects/thudswoosh.ogg", uid, AudioHelpers.WithVariation(0.05f, _robustRandom));
                    _popupSystem.PopupEntity(Loc.GetString("wake-other-failure", ("target", Identity.Entity(uid, EntityManager))), uid, Filter.Entities(user.Value), true, Shared.Popups.PopupType.SmallCaution);
                }
                return false;
            }

            if (user != null)
            {
                _audio.PlayPvs("/Audio/Effects/thudswoosh.ogg", uid, AudioHelpers.WithVariation(0.05f, _robustRandom));
                _popupSystem.PopupEntity(Loc.GetString("wake-other-success", ("target", Identity.Entity(uid, EntityManager))), uid, Filter.Entities(user.Value), true);
            }
            RemComp<SleepingComponent>(uid);
            return true;
        }
    }
}
