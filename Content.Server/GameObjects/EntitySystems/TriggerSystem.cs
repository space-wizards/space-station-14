using System;
using System.Linq;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Timers;

namespace Content.Server.GameObjects.EntitySystems
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
        public IEntity User { get; set; }
        public IEntity Source { get; set; }
    }

    [UsedImplicitly]
    public sealed class TriggerSystem : EntitySystem
    {
        public void HandleTimerTrigger(int delay, IEntity user, IEntity trigger)
        {

            Timer.Spawn(delay, () =>
            {
                var timerTriggerEventArgs = new TimerTriggerEventArgs
                {
                    User = user,
                    Source = trigger
                };
                var attackBys = trigger.GetAllComponents<ITimerTrigger>().ToList();

                foreach (var attackBy in attackBys)
                {
                    if (attackBy.Trigger(timerTriggerEventArgs))
                    {
                        // If an IOnTimerTrigger returns a status completion we finish our trigger
                        return;
                    }
                }
            });
        }
    }
}