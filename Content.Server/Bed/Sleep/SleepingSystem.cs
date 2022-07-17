using Content.Shared.Stunnable;
using Content.Shared.MobState.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Server.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Prototypes;
using Content.Shared.Sound;
using Robust.Shared.Timing;
using Content.Shared.MobState;
using Content.Server.MobState;
using Content.Server.Sound.Components;

namespace Content.Server.Bed.Sleep
{
    public sealed class SleepingSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ActionsSystem _actionsSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var forced in EntityQuery<ForcedSleepingComponent>())
            {
                forced.Accumulator += frameTime;
                if (forced.Accumulator < forced.TargetDuration.TotalSeconds)
                {
                    continue;
                }
                RemCompDeferred<ForcedSleepingComponent>(forced.Owner);
            }
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MobStateComponent, SleepStateChangedEvent>(OnSleepStateChanged);
            SubscribeLocalEvent<SleepingComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<MobStateComponent, SleepActionEvent>(OnSleepAction);
            SubscribeLocalEvent<MobStateComponent, WakeActionEvent>(OnWakeAction);
            SubscribeLocalEvent<SleepingComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        private void OnSleepStateChanged(EntityUid uid, MobStateComponent component, SleepStateChangedEvent args)
        {
            _prototypeManager.TryIndex<InstantActionPrototype>("Wake", out var wakeAction);
            if (args.FellAsleep)
            {
                EnsureComp<StunnedComponent>(uid);
                EnsureComp<KnockedDownComponent>(uid);
                if (wakeAction != null)
                {
                    var emitSound = EnsureComp<SpamEmitSoundComponent>(uid);
                    emitSound.Sound = new SoundCollectionSpecifier("Snores");
                    emitSound.PlayChance = 0.33f;
                    emitSound.RollInterval = 5f;
                    emitSound.PopUp = "sleep-onomatopoeia";
                    emitSound.PitchVariation = 0.2f;

                    var wakeInstance = new InstantAction(wakeAction);
                    wakeInstance.Cooldown = (_gameTiming.CurTime, _gameTiming.CurTime + TimeSpan.FromSeconds(30));
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

        private void OnDamageChanged(EntityUid uid, SleepingComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased || args.DamageDelta == null)
                return;

            if (args.DamageDelta.Total >= 2.5)
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

        private void OnMobStateChanged(EntityUid uid, SleepingComponent component, MobStateChangedEvent args)
        {
            if (_mobStateSystem.IsCritical(uid) && !HasComp<ForcedSleepingComponent>(uid))
            {
                RemComp<SleepingComponent>(uid);
                return;
            }
            if (_mobStateSystem.IsDead(uid))
                RemComp<SleepingComponent>(uid);
        }

        public bool TrySleeping(EntityUid uid)
        {
            if (!HasComp<MobStateComponent>(uid))
                return false;

            if (_prototypeManager.TryIndex<InstantActionPrototype>("Sleep", out var sleepAction))
                _actionsSystem.RemoveAction(uid, sleepAction);

            EnsureComp<SleepingComponent>(uid);
            return true;
        }

        public bool AddForcedSleepingTime(EntityUid uid, float secondsToAdd)
        {
            if (!HasComp<MobStateComponent>(uid))
                return false;

            EnsureComp<SleepingComponent>(uid);
            var forced = EnsureComp<ForcedSleepingComponent>(uid);
            forced.Accumulator -= secondsToAdd;
            return true;
        }

        public bool TryWaking(EntityUid uid)
        {
            if (HasComp<ForcedSleepingComponent>(uid))
                return false;

            RemComp<SleepingComponent>(uid);
            return true;
        }
    }
}
