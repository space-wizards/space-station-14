using Content.Server.Construction.Components;
using Content.Server.Explosion.Components;
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(HandleCollide);
            SubscribeLocalEvent<TriggerOnProximityComponent, StartCollideEvent>(HandleCollide);

            SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteTrigger);
            SubscribeLocalEvent<SoundOnTriggerComponent, TriggerEvent>(HandleSoundTrigger);
            SubscribeLocalEvent<ExplodeOnTriggerComponent, TriggerEvent>(HandleExplodeTrigger);
            SubscribeLocalEvent<FlashOnTriggerComponent, TriggerEvent>(HandleFlashTrigger);

            SubscribeLocalEvent<ExplosiveComponent, DestructionEventArgs>(HandleDestruction);
            SubscribeLocalEvent<TriggerOnProximityComponent, ComponentInit>(CheckEnable);
            SubscribeLocalEvent<TriggerOnProximityComponent, UnanchoredEvent>(HandleAnchor);
            SubscribeLocalEvent<TriggerOnProximityComponent, AnchoredEvent>(HandleAnchor);

        }

        private void HandleAnchor(EntityUid uid, TriggerOnProximityComponent component, UnanchoredEvent args)
        {
            SetProximityFixture(uid, component, component.enabled && component.Owner.Transform.Anchored);
        }

        private void HandleAnchor(EntityUid uid, TriggerOnProximityComponent component, AnchoredEvent args)
        {
            SetProximityFixture(uid, component, component.enabled && component.Owner.Transform.Anchored);
        }

        private void CheckEnable(EntityUid uid, TriggerOnProximityComponent component, ComponentInit args)
        {
            component.Enabled = component.enabled;
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
            bool repeatable = component.Repeating && component.LastFlash + TimeSpan.FromSeconds(component.Cooldown) < _gameTiming.CurTime;

            if (!component.Flashed || repeatable)
            {
                FlashableComponent.FlashAreaHelper(component.Owner, component.Range, component.Duration);
                component.Flashed = true;
                component.LastFlash = _gameTiming.CurTime;
            }
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

        private void HandleCollide(EntityUid uid, TriggerOnCollideComponent component, StartCollideEvent args)
        {
            Trigger(component.Owner);
        }
        private void HandleCollide(EntityUid uid, TriggerOnProximityComponent component, StartCollideEvent args)
        {
            var curTime = _gameTiming.CurTime;
            if (args.OurFixture.ID == component.ProximityFixture)
            {
                if (component.LastTrigger + TimeSpan.FromSeconds(component.Cooldown) < curTime)
                {
                    Trigger(component.Owner);
                    component.LastTrigger = curTime;
                }
            }
        }
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
        public void SetProximityFixture(EntityUid uid, TriggerOnProximityComponent component, bool remove)
        {
            var entity = EntityManager.GetEntity(uid);
            var broadphase = Get<SharedBroadphaseSystem>();

            if (entity.TryGetComponent(out PhysicsComponent? physics))
            {
                var fixture = physics.GetFixture(component.ProximityFixture);
                if (!remove && fixture != null)
                {
                    broadphase.DestroyFixture(physics, fixture);
                }
                else
                {
                    broadphase.CreateFixture(physics, new Fixture(physics, component.Shape) { CollisionLayer = (int) (CollisionGroup.MobImpassable | CollisionGroup.SmallImpassable | CollisionGroup.SmallImpassable), Hard = false, ID = component.ProximityFixture });
                }
            }
        }
    }
}
