using System;
using Content.Server.Administration.Logs;
using Content.Server.Doors;
using Content.Server.Doors.Components;
using Content.Server.Doors.Systems;
using Content.Server.Explosion.Components;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Shared.Audio;
using Content.Shared.Doors;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Threading;
using Content.Server.Construction.Components;
using Content.Shared.Trigger;
using Timer = Robust.Shared.Timing.Timer;
using Content.Shared.Physics;
using System.Collections.Generic;

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

        public override void Initialize()
        {
            base.Initialize();

            InitializeProximity();
            InitializeOnUse();

            SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(OnTriggerCollide);

            SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteTrigger);
            SubscribeLocalEvent<SoundOnTriggerComponent, TriggerEvent>(HandleSoundTrigger);
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

        private void HandleSoundTrigger(EntityUid uid, SoundOnTriggerComponent component, TriggerEvent args)
        {
            if (component.Sound == null) return;
            SoundSystem.Play(Filter.Pvs(component.Owner), component.Sound.GetSound(), uid);
        }

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

        public void HandleTimerTrigger(TimeSpan delay, EntityUid triggered, EntityUid? user = null)
        {
            if (delay.TotalSeconds <= 0)
            {
                Trigger(triggered, user);
                return;
            }

            Timer.Spawn(delay, () =>
            {
                if (Deleted(triggered)) return;
                Trigger(triggered, user);
            });
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateProximity(frameTime);
        }
    }
}
