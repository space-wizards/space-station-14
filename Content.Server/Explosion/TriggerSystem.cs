using Content.Server.Construction.Components;
using Content.Server.Explosion.Components;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Shared.Acts;
using Content.Shared.Audio;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System;
using System.Threading;
using Robust.Shared.Log;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Explosion
{
    /// <summary>
    /// Raised whenever something is Triggered on the entity.
    /// </summary>
    public class TriggerEvent : HandledEntityEventArgs
    {
        public IEntity Triggered { get; }
        public IEntity? User { get; }

        public TriggerEvent(IEntity triggered, IEntity? user = null)
        {
            Triggered = triggered;
            User = user;
        }
    }

    [UsedImplicitly]
    public sealed class TriggerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphaseSystem = default!;
        [Dependency] private readonly FlashSystem _flashSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(OnTriggerCollide);
            SubscribeLocalEvent<TriggerOnProximityComponent, StartCollideEvent>(OnProximityStartCollide);
            SubscribeLocalEvent<TriggerOnProximityComponent, EndCollideEvent>(OnProximityEndCollide);

            SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteTrigger);
            SubscribeLocalEvent<SoundOnTriggerComponent, TriggerEvent>(HandleSoundTrigger);
            SubscribeLocalEvent<ExplodeOnTriggerComponent, TriggerEvent>(HandleExplodeTrigger);
            SubscribeLocalEvent<FlashOnTriggerComponent, TriggerEvent>(HandleFlashTrigger);

            SubscribeLocalEvent<ExplosiveComponent, DestructionEventArgs>(HandleDestruction);
            SubscribeLocalEvent<TriggerOnProximityComponent, ComponentStartup>(OnStartup);
            /*
            SubscribeLocalEvent<TriggerOnProximityComponent, UnanchoredEvent>(HandleAnchor);
            SubscribeLocalEvent<TriggerOnProximityComponent, AnchoredEvent>(HandleAnchor);
            */

        }

        private void OnStartup(EntityUid uid, TriggerOnProximityComponent component, ComponentStartup args)
        {
            SetProximityFixture(uid, component, component.Enabled, true);
        }

        #region Explosions
        private void HandleDestruction(EntityUid uid, ExplosiveComponent component, DestructionEventArgs args)
        {
            Explode(uid, component);
        }

        private void HandleExplodeTrigger(EntityUid uid, ExplodeOnTriggerComponent component, TriggerEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out ExplosiveComponent? explosiveComponent)) return;

            Explode(uid, explosiveComponent);
        }

        // You really shouldn't call this directly (TODO Change that when ExplosionHelper gets changed).
        public void Explode(EntityUid uid, ExplosiveComponent component)
        {
            if (component.Exploding)
            {
                return;
            }

            component.Exploding = true;
            component.Owner.SpawnExplosion(component.DevastationRange, component.HeavyImpactRange, component.LightImpactRange, component.FlashRange);
            EntityManager.QueueDeleteEntity(uid);
        }
        #endregion

        #region Flash
        private void HandleFlashTrigger(EntityUid uid, FlashOnTriggerComponent component, TriggerEvent args)
        {
            // TODO Make flash durations sane ffs.
            _flashSystem.FlashArea(uid, args.User?.Uid, component.Range, component.Duration * 1000f);
        }
        #endregion

        private void HandleSoundTrigger(EntityUid uid, SoundOnTriggerComponent component, TriggerEvent args)
        {
            if (component.Sound == null) return;
            SoundSystem.Play(Filter.Pvs(component.Owner), component.Sound.GetSound(), AudioHelpers.WithVariation(0.01f));
        }

        private void HandleDeleteTrigger(EntityUid uid, DeleteOnTriggerComponent component, TriggerEvent args)
        {
            EntityManager.QueueDeleteEntity(uid);
        }

        private void OnTriggerCollide(EntityUid uid, TriggerOnCollideComponent component, StartCollideEvent args)
        {
            Trigger(component.Owner);
        }

        #region Proximity

        private void OnProximityStartCollide(EntityUid uid, TriggerOnProximityComponent component, StartCollideEvent args)
        {
            if (args.OurFixture.ID != TriggerOnProximityComponent.FixtureID) return;

            var curTime = _gameTiming.CurTime;

            if (component.NextTrigger > curTime ||
                !component.Colliding.Add(uid)) return;

            Trigger(component.Owner);
            component.NextTrigger = TimeSpan.FromSeconds(curTime.TotalSeconds + component.Cooldown);

            SetRepeating(uid, component);
        }

        private void OnProximityEndCollide(EntityUid uid, TriggerOnProximityComponent component, EndCollideEvent args)
        {
            component.Colliding.Remove(uid);

            if (component.Colliding.Count == 0)
            {
                component.RepeatCancelTokenSource?.Cancel();
            }
        }

        private void SetRepeating(EntityUid uid, TriggerOnProximityComponent component)
        {
            // Setup the proximity timer to re-trigger in cooldown seconds. Also pass in a token in case we need to cancel it.
            component.RepeatCancelTokenSource?.Cancel();

            if (!component.Repeating || !(component.Cooldown > 0f)) return;

            component.RepeatCancelTokenSource = new CancellationTokenSource();

            Timer.Spawn((int) (component.Cooldown * 1000), () =>
            {
                if (component.Colliding.Count == 0 ||
                    !EntityManager.TryGetEntity(uid, out var entity) ||
                    component.Deleted) return;

                Trigger(entity);
                SetRepeating(uid, component);
            }, component.RepeatCancelTokenSource.Token);
        }

        public void SetProximityFixture(EntityUid uid, TriggerOnProximityComponent component, bool value, bool force = false)
        {
            if (component.Enabled == value && !force ||
                !ComponentManager.TryGetComponent(uid, out PhysicsComponent? body)) return;

            component.Enabled = true;

            if (value)
            {
                // Already has it so don't worry about it.
                if (body.GetFixture(TriggerOnProximityComponent.FixtureID) != null) return;

                _broadphaseSystem.CreateFixture(body, new Fixture(body, component.Shape)
                {
                    // TODO: Should probably have these settable via datafield but I'm lazy and it's a pain
                    CollisionLayer = (int) (CollisionGroup.MobImpassable | CollisionGroup.SmallImpassable | CollisionGroup.SmallImpassable), Hard = false, ID = TriggerOnProximityComponent.FixtureID
                });
            }
            else
            {
                var fixture = body.GetFixture(TriggerOnProximityComponent.FixtureID);

                if (fixture == null) return;

                _broadphaseSystem.DestroyFixture(fixture);
            }
        }

        #endregion

        public void Trigger(IEntity trigger, IEntity? user = null)
        {
            var triggerEvent = new TriggerEvent(trigger, user);
            EntityManager.EventBus.RaiseLocalEvent(trigger.Uid, triggerEvent);
        }

        public void HandleTimerTrigger(TimeSpan delay, IEntity triggered, IEntity? user = null)
        {
            if (delay.TotalSeconds <= 0)
            {
                Trigger(triggered, user);
                return;
            }

            Timer.Spawn(delay, () =>
            {
                if (triggered.Deleted) return;
                Trigger(triggered, user);
            });
        }
    }
}
