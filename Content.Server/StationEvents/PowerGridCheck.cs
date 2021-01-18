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
        public override float Weight => WeightNormal;
        public override int? MaxOccurrences => 3;
        public override string StartAnnouncement => Loc.GetString(
            "Abnormal activity detected in the station's powernet. As a precautionary measure, the station's power will be shut off for an indeterminate duration.");
        protected override string EndAnnouncement => Loc.GetString(
            "Power has been restored to the station. We apologize for the inconvenience.");
        public override string StartAudio => "/Audio/Announcements/power_off.ogg";

        protected override float StartAfter => 3.0f;


        private CancellationTokenSource _announceCancelToken;

        private readonly List<IEntity> _powered = new();

        public override void Startup()
        {
            var componentManager = IoCManager.Resolve<IComponentManager>();

            foreach (var component in componentManager.EntityQuery<PowerReceiverComponent>())
            {
                component.PowerDisabled = true;
                _powered.Add(component.Owner);
            }

            EndAfter = IoCManager.Resolve<IRobustRandom>().Next(60, 120);
            base.Startup();
        }

        public override void Shutdown()
        {
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
            base.Shutdown();
        }
    }
}
