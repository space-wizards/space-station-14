using System.Collections.Generic;
using System.Threading;
using Content.Server.Power.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed class PowerGridCheck : StationEvent
    {
        public override string Name => "PowerGridCheck";
        public override float Weight => WeightNormal;
        public override int? MaxOccurrences => 3;
        public override string StartAnnouncement => Loc.GetString("station-event-power-grid-check-start-announcement");
        protected override string EndAnnouncement => Loc.GetString("station-event-power-grid-check-end-announcement");
        public override string? StartAudio => "/Audio/Announcements/power_off.ogg";

        // If you need EndAudio it's down below. Not set here because we can't play it at the normal time without spamming sounds.

        protected override float StartAfter => 12.0f;

        private CancellationTokenSource? _announceCancelToken;

        private readonly List<IEntity> _powered = new();

        public override void Announce()
        {
            base.Announce();
            EndAfter = IoCManager.Resolve<IRobustRandom>().Next(60, 120);
        }

        public override void Startup()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            foreach (var component in entityManager.EntityQuery<ApcPowerReceiverComponent>(true))
            {
                component.PowerDisabled = true;
                _powered.Add(component.Owner);
            }

            base.Startup();
        }

        public override void Shutdown()
        {
            foreach (var entity in _powered)
            {
                if (entity.Deleted) continue;

                if (entity.TryGetComponent(out ApcPowerReceiverComponent? powerReceiverComponent))
                {
                    powerReceiverComponent.PowerDisabled = false;
                }
            }

            // Can't use the default EndAudio
            _announceCancelToken?.Cancel();
            _announceCancelToken = new CancellationTokenSource();
            Timer.Spawn(3000, () =>
            {
                SoundSystem.Play(Filter.Broadcast(), "/Audio/Announcements/power_on.ogg");
            }, _announceCancelToken.Token);
            _powered.Clear();

            base.Shutdown();
        }
    }
}
