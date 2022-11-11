using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.Sound.Components;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Audio;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Slippery;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Bed.Sleep
{
    public sealed class SleepingSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ActionsSystem _actionsSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        [Dependency] private readonly IRobustRandom _robustRandom = default!;
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
            _prototypeManager.TryIndex<InstantActionPrototype>("Wake", out var wakeAction);
            if (args.FellAsleep)
            {
                EnsureComp<StunnedComponent>(uid);
                EnsureComp<KnockedDownComponent>(uid);

                var emitSound = EnsureComp<SpamEmitSoundComponent>(uid);

                // TODO WTF is this, these should a data fields and not hard-coded.
                emitSound.Sound = new SoundCollectionSpecifier("Snores", AudioParams.Default.WithVariation(0.2f));
                emitSound.PlayChance = 0.33f;
                emitSound.RollInterval = 5f;
                emitSound.PopUp = "sleep-onomatopoeia";

                if (wakeAction != null)
                {
                    var wakeInstance = new InstantAction(wakeAction);
                    wakeInstance.Cooldown = (_gameTiming.CurTime, _gameTiming.CurTime + TimeSpan.FromSeconds(15));
                    _actionsSystem.AddAction(uid, wakeInstance, null);
                }
                return;
            }
            if (wakeAction != null)
                _actionsSystem.RemoveAction(uid, wakeAction);

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
                TryWaking(uid);
        }

        private void OnSleepAction(EntityUid uid, MobStateComponent component, SleepActionEvent args)
        {
            TrySleeping(uid);
        }

        private void OnWakeAction(EntityUid uid, MobStateComponent component, WakeActionEvent args)
        {
            TryWaking(uid);
        }

        /// <summary>
        /// In crit, we wake up if we are not being forced to sleep.
        /// And, you can't sleep when dead...
        /// </summary>
        private void OnMobStateChanged(EntityUid uid, SleepingComponent component, MobStateChangedEvent args)
        {
            if (args.CurrentMobState == DamageState.Dead)
            {
                RemComp<SpamEmitSoundComponent>(uid);
                RemComp<SleepingComponent>(uid);
                return;
            }
            if (TryComp<SpamEmitSoundComponent>(uid, out var spam))
                spam.Enabled = (args.CurrentMobState == DamageState.Alive) ? true : false;
        }

        private void AddWakeVerb(EntityUid uid, SleepingComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
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

            var curTime = _gameTiming.CurTime;
            if (curTime < component.CoolDownEnd)
            {
                return;
            }

            TryWaking(args.Target, user: args.User);
            component.CoolDownEnd = curTime + component.Cooldown;
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

            if (_prototypeManager.TryIndex<InstantActionPrototype>("Sleep", out var sleepAction))
                _actionsSystem.RemoveAction(uid, sleepAction);

            EnsureComp<SleepingComponent>(uid);
            return true;
        }

        /// <summary>
        /// Try to wake up.
        /// </summary>
        public bool TryWaking(EntityUid uid, bool force = false, EntityUid? user = null)
        {
            if (!force && HasComp<ForcedSleepingComponent>(uid))
            {
                if (user != null)
                {
                    SoundSystem.Play("/Audio/Effects/thudswoosh.ogg", Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.05f, _robustRandom));
                    _popupSystem.PopupEntity(Loc.GetString("wake-other-failure", ("target", Identity.Entity(uid, EntityManager))), uid, Filter.Entities(user.Value), Shared.Popups.PopupType.SmallCaution);
                }
                return false;
            }

            if (user != null)
            {
                SoundSystem.Play("/Audio/Effects/thudswoosh.ogg", Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.05f, _robustRandom));
                _popupSystem.PopupEntity(Loc.GetString("wake-other-success", ("target", Identity.Entity(uid, EntityManager))), uid, Filter.Entities(user.Value));
            }
            RemComp<SleepingComponent>(uid);
            return true;
        }
    }
}
