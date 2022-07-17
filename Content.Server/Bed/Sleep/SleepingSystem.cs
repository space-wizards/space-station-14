using Content.Shared.Stunnable;
using Content.Shared.MobState.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Server.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Bed.Sleep
{
    public sealed class SleepingSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ActionsSystem _actionsSystem = default!;
        [Dependency] private readonly IGameTiming GameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MobStateComponent, SleepStateChangedEvent>(OnSleepStateChanged);
            SubscribeLocalEvent<SleepingComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<MobStateComponent, SleepActionEvent>(OnSleepAction);
            SubscribeLocalEvent<MobStateComponent, WakeActionEvent>(OnWakeAction);
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
                    var wakeInstance = new InstantAction(wakeAction);
                    wakeInstance.Cooldown = (GameTiming.CurTime, GameTiming.CurTime + TimeSpan.FromMinutes(1));
                    _actionsSystem.AddAction(uid, wakeInstance, null);
                }
                return;
            }
            if (wakeAction != null)
                _actionsSystem.RemoveAction(uid, wakeAction);

            RemComp<StunnedComponent>(uid);
            RemComp<KnockedDownComponent>(uid);
        }

        private void OnDamageChanged(EntityUid uid, SleepingComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased || args.DamageDelta == null)
                return;

            if (args.DamageDelta.Total >= 5)
                TryWaking(uid);
        }

        private void OnSleepAction(EntityUid uid, MobStateComponent component, SleepActionEvent args)
        {
            Logger.Error("Received sleep action event");
            TrySleeping(uid);
        }

        private void OnWakeAction(EntityUid uid, MobStateComponent component, WakeActionEvent args)
        {
            Logger.Error("Received wake action event");
            TryWaking(uid);
        }

        public bool TrySleeping(EntityUid uid)
        {
            if (!HasComp<MobStateComponent>(uid))
                return false;

            if (_prototypeManager.TryIndex<InstantActionPrototype>("Sleep", out var sleepAction))
                _actionsSystem.RemoveAction(uid, sleepAction);

            AddComp<SleepingComponent>(uid);
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
