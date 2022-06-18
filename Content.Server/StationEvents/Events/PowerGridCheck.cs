using System.Threading;
using Content.Server.Power.Components;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed class PowerGridCheck : StationEvent
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "PowerGridCheck";
        public override float Weight => WeightNormal;
        public override int? MaxOccurrences => 3;
        public override string StartAnnouncement => Loc.GetString("station-event-power-grid-check-start-announcement");
        protected override string EndAnnouncement => Loc.GetString("station-event-power-grid-check-end-announcement");
        public override SoundSpecifier? StartAudio => new SoundPathSpecifier("/Audio/Announcements/power_off.ogg");

        // If you need EndAudio it's down below. Not set here because we can't play it at the normal time without spamming sounds.

        protected override float StartAfter => 12.0f;

        private CancellationTokenSource? _announceCancelToken;

        private readonly List<EntityUid> _powered = new();
        private readonly List<EntityUid> _unpowered = new();

        private const float SecondsUntilOff = 30.0f;

        private int _numberPerSecond = 0;
        private float UpdateRate => 1.0f / _numberPerSecond;
        private float _frameTimeAccumulator = 0.0f;

        public override void Announce()
        {
            base.Announce();
            EndAfter = IoCManager.Resolve<IRobustRandom>().Next(60, 120);
        }

        public override void Startup()
        {
            foreach (var component in _entityManager.EntityQuery<ApcPowerReceiverComponent>(true))
            {
                if (!component.PowerDisabled)
                    _powered.Add(component.Owner);
            }

            _random.Shuffle(_powered);

            _numberPerSecond = Math.Max(1, (int)(_powered.Count / SecondsUntilOff)); // Number of APCs to turn off every second. At least one.

            base.Startup();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _frameTimeAccumulator += frameTime;

            var updates = 0;
            if (_frameTimeAccumulator > UpdateRate)
            {
                updates = (int) (_frameTimeAccumulator / UpdateRate);
                _frameTimeAccumulator -= UpdateRate * updates;
            }

            for (var i = 0; i < updates; i++)
            {
                if (_powered.Count == 0)
                    break;

                var selected = _powered.Pop();
                if (_entityManager.Deleted(selected)) continue;
                if (_entityManager.TryGetComponent<ApcPowerReceiverComponent>(selected, out var powerReceiverComponent))
                {
                    powerReceiverComponent.PowerDisabled = true;
                }
                _unpowered.Add(selected);
            }
        }

        public override void Shutdown()
        {
            foreach (var entity in _unpowered)
            {
                if (_entityManager.Deleted(entity)) continue;

                if (_entityManager.TryGetComponent(entity, out ApcPowerReceiverComponent? powerReceiverComponent))
                {
                    powerReceiverComponent.PowerDisabled = false;
                }
            }

            // Can't use the default EndAudio
            _announceCancelToken?.Cancel();
            _announceCancelToken = new CancellationTokenSource();
            Timer.Spawn(3000, () =>
            {
                SoundSystem.Play("/Audio/Announcements/power_on.ogg", Filter.Broadcast(), AudioParams);
            }, _announceCancelToken.Token);
            _unpowered.Clear();

            base.Shutdown();
        }
    }
}
