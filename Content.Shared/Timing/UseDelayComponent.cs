using System;
using System.Threading;
using Content.Shared.Cooldown;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Timing
{
    /// <summary>
    /// Timer that creates a cooldown each time an object is activated/used
    /// </summary>
    [RegisterComponent]
    public class UseDelayComponent : Component
    {
        public override string Name => "UseDelay";

        private TimeSpan _lastUseTime;

        [DataField("delay")]
        private float _delay = 1;
        /// <summary>
        /// The time, in seconds, between an object's use and when it can be used again
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Delay { get => _delay; set => _delay = value; }

        public bool ActiveDelay{ get; private set; }

        private CancellationTokenSource? cancellationTokenSource;

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

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out ItemCooldownComponent? cooldown))
            {
                cooldown.CooldownStart = _lastUseTime;
                cooldown.CooldownEnd = _lastUseTime + TimeSpan.FromSeconds(Delay);
            }

        }

        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
            ActiveDelay = false;

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out ItemCooldownComponent? cooldown))
            {
                cooldown.CooldownEnd = IoCManager.Resolve<IGameTiming>().CurTime;
            }
        }

        public void Restart()
        {
            cancellationTokenSource?.Cancel();
            ActiveDelay = false;
            BeginDelay();
        }
    }
}
