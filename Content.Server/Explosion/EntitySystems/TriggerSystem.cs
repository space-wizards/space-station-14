using Content.Server.Administration.Logs;
using Content.Server.Doors.Components;
using Content.Server.Doors.Systems;
using Content.Server.Explosion.Components;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Content.Shared.Sound;
using Content.Shared.Trigger;
using Content.Shared.Database;

namespace Content.Server.Explosion.EntitySystems
{
    /// <summary>
    /// Raised whenever something is Triggered on the entity.
    /// </summary>
    public sealed class TriggerEvent : HandledEntityEventArgs
    {
        public EntityUid Triggered { get; }
        public EntityUid? User { get; }

        public TriggerEvent(EntityUid triggered, EntityUid? user = null)
        {
            Triggered = triggered;
            User = user;
        }
    }

    [UsedImplicitly]
    public sealed partial class TriggerSystem : EntitySystem
    {
        [Dependency] private readonly ExplosionSystem _explosions = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly FlashSystem _flashSystem = default!;
        [Dependency] private readonly DoorSystem _sharedDoorSystem = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeProximity();
            InitializeOnUse();

            SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(OnTriggerCollide);

            SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteTrigger);
            SubscribeLocalEvent<ExplodeOnTriggerComponent, TriggerEvent>(HandleExplodeTrigger);
            SubscribeLocalEvent<FlashOnTriggerComponent, TriggerEvent>(HandleFlashTrigger);
            SubscribeLocalEvent<ToggleDoorOnTriggerComponent, TriggerEvent>(HandleDoorTrigger);
        }

        #region Explosions
        private void HandleExplodeTrigger(EntityUid uid, ExplodeOnTriggerComponent component, TriggerEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out ExplosiveComponent? explosiveComponent)) return;

            Explode(uid, explosiveComponent, args.User);
        }

        // You really shouldn't call this directly (TODO Change that when ExplosionHelper gets changed).
        public void Explode(EntityUid uid, ExplosiveComponent component, EntityUid? user = null)
        {
            if (component.Exploding)
            {
                return;
            }

            component.Exploding = true;
            _explosions.SpawnExplosion(uid,
                component.DevastationRange,
                component.HeavyImpactRange,
                component.LightImpactRange,
                component.FlashRange,
                user);
            EntityManager.QueueDeleteEntity(uid);
        }
        #endregion

        #region Flash
        private void HandleFlashTrigger(EntityUid uid, FlashOnTriggerComponent component, TriggerEvent args)
        {
            // TODO Make flash durations sane ffs.
            _flashSystem.FlashArea(uid, args.User, component.Range, component.Duration * 1000f);
        }
        #endregion

        private void HandleDeleteTrigger(EntityUid uid, DeleteOnTriggerComponent component, TriggerEvent args)
        {
            EntityManager.QueueDeleteEntity(uid);
        }

        private void HandleDoorTrigger(EntityUid uid, ToggleDoorOnTriggerComponent component, TriggerEvent args)
        {
            _sharedDoorSystem.TryToggleDoor(uid);
        }

        private void OnTriggerCollide(EntityUid uid, TriggerOnCollideComponent component, StartCollideEvent args)
        {
            Trigger(component.Owner);
        }


        public void Trigger(EntityUid trigger, EntityUid? user = null)
        {
            var triggerEvent = new TriggerEvent(trigger, user);
            EntityManager.EventBus.RaiseLocalEvent(trigger, triggerEvent);
        }

        public void HandleTimerTrigger(EntityUid uid, EntityUid? user, float delay , float beepInterval, float? initialBeepDelay, SoundSpecifier? beepSound, AudioParams beepParams)
        {
            if (delay <= 0)
            {
                RemComp<ActiveTimerTriggerComponent>(uid);
                Trigger(uid, user);
                return;
            }

            if (HasComp<ActiveTimerTriggerComponent>(uid))
                return;

            if (user != null)
            {
                _logSystem.Add(LogType.Trigger,
                    $"{ToPrettyString(user.Value):user} started a {delay} second timer trigger on entity {ToPrettyString(uid):timer}");
            }
            else
            {
                _logSystem.Add(LogType.Trigger,
                    $"{delay} second timer trigger started on entity {ToPrettyString(uid):timer}");
            }

            var active = AddComp<ActiveTimerTriggerComponent>(uid);
            active.TimeRemaining = delay;
            active.User = user;
            active.BeepParams = beepParams;
            active.BeepSound = beepSound;
            active.BeepInterval = beepInterval;
            active.TimeUntilBeep = initialBeepDelay == null ? active.BeepInterval : initialBeepDelay.Value;

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                appearance.SetData(TriggerVisuals.VisualState, TriggerVisualState.Primed);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateProximity(frameTime);
            UpdateTimer(frameTime);
        }

        private void UpdateTimer(float frameTime)
        {
            HashSet<EntityUid> toRemove = new();
            foreach (var timer in EntityQuery<ActiveTimerTriggerComponent>())
            {
                timer.TimeRemaining -= frameTime;
                timer.TimeUntilBeep -= frameTime;

                if (timer.TimeRemaining <= 0)
                {
                    Trigger(timer.Owner, timer.User);
                    toRemove.Add(timer.Owner);
                    continue;
                }

                if (timer.BeepSound == null || timer.TimeUntilBeep > 0)
                    continue;

                timer.TimeUntilBeep += timer.BeepInterval;
                var filter = Filter.Pvs(timer.Owner, entityManager: EntityManager);
                SoundSystem.Play(filter, timer.BeepSound.GetSound(), timer.Owner, timer.BeepParams);
            }

            foreach (var uid in toRemove)
            {
                RemComp<ActiveTimerTriggerComponent>(uid);

                // In case this is a re-usable grenade, un-prime it.
                if (TryComp<AppearanceComponent>(uid, out var appearance))
                    appearance.SetData(TriggerVisuals.VisualState, TriggerVisualState.Unprimed);
            }
        }
    }
}
