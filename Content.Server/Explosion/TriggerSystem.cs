using System;
using System.Linq;
using Content.Server.Explosion.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Timing;

namespace Content.Server.Explosion
{
    /// <summary>
    /// This interface gives components behavior when being "triggered" by timer or other conditions
    /// </summary>
    public interface ITrigger
    {
        /// <summary>
        /// Called when one object is triggering some event
        /// </summary>
        bool Trigger(TriggerEventArgs eventArgs);
    }

    public class TriggerEventArgs : HandledEntityEventArgs
    {
        public IEntity Triggered { get; }
        public IEntity? User { get; }

        public TriggerEventArgs(IEntity triggered, IEntity? user = null)
        {
            Triggered = triggered;
            User = user;
        }
    }

    [UsedImplicitly]
    public sealed class TriggerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, TriggerOnCollideComponent component, StartCollideEvent args)
        {
            Trigger(component.Owner);
        }

        public void Trigger(IEntity trigger, IEntity? user = null)
        {
            var timerTriggers = trigger.GetAllComponents<ITrigger>().ToList();
            var triggerEvent = new TriggerEventArgs(trigger, user);

            foreach (var timerTrigger in timerTriggers)
            {
                if (timerTrigger.Trigger(triggerEvent))
                {
                    // If an IOnTimerTrigger returns a status completion we finish our trigger
                    return;
                }
            }

            EntityManager.EventBus.RaiseLocalEvent(trigger.Uid, triggerEvent);
        }

        public void HandleTimerTrigger(TimeSpan delay, IEntity triggered, IEntity? user = null)
        {
            Timer.Spawn(delay, () =>
            {
                Trigger(triggered, user);
            });
        }
    }
}
