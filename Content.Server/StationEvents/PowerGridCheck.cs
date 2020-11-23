using System.Collections.Generic;
using System.Threading;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.StationEvents
{
    [UsedImplicitly]
    public sealed class PowerGridCheck : StationEvent
    {
        public override string Name => "PowerGridCheck";

        public override StationEventWeight Weight => StationEventWeight.Normal;

        public override int? MaxOccurrences => 2;

        protected override string StartAnnouncement => Loc.GetString(
            "Abnormal activity detected in the station's powernet. As a precautionary measure, the station's power will be shut off for an indeterminate duration.");

        protected override string EndAnnouncement => Loc.GetString(
            "Power has been restored to the station. We apologize for the inconvenience.");

        private float _elapsedTime;
        private int _failDuration;

        /// <summary>
        ///     So we don't overlap the announcement with power-down sounds we'll delay it a few seconds.
        /// </summary>
        private bool _announced;

        private CancellationTokenSource _announceCancelToken;

        private readonly List<IEntity> _powered = new List<IEntity>();

        public override void Startup()
        {
            base.Startup();

            _announced = false;
            _elapsedTime = 0.0f;
            _failDuration = IoCManager.Resolve<IRobustRandom>().Next(60, 120);
            var componentManager = IoCManager.Resolve<IComponentManager>();

            foreach (PowerReceiverComponent component in componentManager.EntityQuery<PowerReceiverComponent>())
            {
                component.PowerDisabled = true;
                _powered.Add(component.Owner);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            foreach (var entity in _powered)
            {
                if (entity.Deleted) continue;

                if (entity.TryGetComponent(out PowerReceiverComponent powerReceiverComponent))
                {
                    powerReceiverComponent.PowerDisabled = false;
                }
            }

            _announceCancelToken?.Cancel();
            _announceCancelToken = new CancellationTokenSource();
            Timer.Spawn(3000, () =>
            {
                EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Announcements/power_on.ogg");
            }, _announceCancelToken.Token);
            _powered.Clear();
        }

        public override void Update(float frameTime)
        {
            if (!Running)
            {
                return;
            }

            if (!_announced && _elapsedTime > 3.0f)
            {
                EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Announcements/power_off.ogg");
                _announced = true;
            }

            _elapsedTime += frameTime;

            if (_elapsedTime < _failDuration)
            {
                return;
            }

            Running = false;
        }
    }
}
