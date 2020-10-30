using System;
using System.Threading;
using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Timing
{
    /// <summary>
    /// Timer that creates a cooldown each time an object is activated/used
    /// </summary>
    [RegisterComponent]
    public class UseDelayComponent : Component
    {
        public override string Name => "UseDelay";

        private TimeSpan _lastUseTime;

        private float _delay;
        /// <summary>
        /// The time, in seconds, between an object's use and when it can be used again
        /// </summary>
        public float Delay { get => _delay; set => _delay = value; }

        public bool ActiveDelay{ get; private set; }

        private CancellationTokenSource cancellationTokenSource;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _delay, "delay", 1);
        }

        public void BeginDelay()
        {
            if (ActiveDelay)
            {
                return;
            }

            ActiveDelay = true;

            cancellationTokenSource = new CancellationTokenSource();

            Owner.SpawnTimer(TimeSpan.FromSeconds(Delay), () => ActiveDelay = false, cancellationTokenSource.Token);

            _lastUseTime = IoCManager.Resolve<IGameTiming>().CurTime;

            if (Owner.TryGetComponent(out ItemCooldownComponent cooldown))
            {
                cooldown.CooldownStart = _lastUseTime;
                cooldown.CooldownEnd = _lastUseTime + TimeSpan.FromSeconds(Delay);
            }

        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
            ActiveDelay = false;

            if (Owner.TryGetComponent(out ItemCooldownComponent cooldown))
            {
                cooldown.CooldownEnd = IoCManager.Resolve<IGameTiming>().CurTime;
            }
        }

        public void Restart()
        {
            cancellationTokenSource.Cancel();
            ActiveDelay = false;
            BeginDelay();
        }
    }
}
