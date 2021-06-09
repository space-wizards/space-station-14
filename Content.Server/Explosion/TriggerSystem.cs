using System;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Explosion
{
    /// <summary>
    /// This interface gives components behavior when being "triggered" by timer or other conditions
    /// </summary>
    public interface ITimerTrigger
    {
        /// <summary>
        /// Called when one object is triggering some event
        /// </summary>
        bool Trigger(TimerTriggerEventArgs eventArgs);
    }

    public class TimerTriggerEventArgs : EventArgs
    {
        public TimerTriggerEventArgs(IEntity user, IEntity source)
        {
            User = user;
            Source = source;
        }

        public IEntity User { get; set; }
        public IEntity Source { get; set; }
    }

    [UsedImplicitly]
    public sealed class TriggerSystem : EntitySystem
    {
        public void HandleTimerTrigger(TimeSpan delay, IEntity user, IEntity trigger)
        {

            Timer.Spawn(delay, () =>
            {
                var timerTriggerEventArgs = new TimerTriggerEventArgs(user, trigger);
                var timerTriggers = trigger.GetAllComponents<ITimerTrigger>().ToList();

                foreach (var timerTrigger in timerTriggers)
                {
                    if (timerTrigger.Trigger(timerTriggerEventArgs))
                    {
                        // If an IOnTimerTrigger returns a status completion we finish our trigger
                        return;
                    }
                }
            });
        }
    }
}
