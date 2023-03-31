using Content.Server.Power.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Threading;
using Content.Server.Power.EntitySystems;
using Timer = Robust.Shared.Timing.Timer;
using System.Linq;
using Robust.Shared.Random;
using Content.Server.Station.Components;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed class PowerGridCheck : StationEventSystem
    {
        [Dependency] private readonly ApcSystem _apcSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        public override string Prototype => "PowerGridCheck";

        private CancellationTokenSource? _announceCancelToken;

        private readonly List<EntityUid> _powered = new();
        private readonly List<EntityUid> _unpowered = new();

        private const float SecondsUntilOff = 30.0f;

        private int _numberPerSecond = 0;
        private float UpdateRate => 1.0f / _numberPerSecond;
        private float _frameTimeAccumulator = 0.0f;
        private float _endAfter = 0.0f;

        public override void Added()
        {
            base.Added();
            _endAfter = RobustRandom.Next(60, 120);
        }

        public override void Started()
        {
            if (StationSystem.Stations.Count == 0)
                return;
            var chosenStation = RobustRandom.Pick(StationSystem.Stations.ToList());

            foreach (var (apc, transform) in EntityQuery<ApcComponent, TransformComponent>(true))
            {
                if (apc.MainBreakerEnabled && CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == chosenStation)
                    _powered.Add(apc.Owner);
            }

            RobustRandom.Shuffle(_powered);

            _numberPerSecond = Math.Max(1, (int)(_powered.Count / SecondsUntilOff)); // Number of APCs to turn off every second. At least one.

            base.Started();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!RuleStarted)
                return;

            if (Elapsed > _endAfter)
            {
                ForceEndSelf();
                return;
            }

            var updates = 0;
            _frameTimeAccumulator += frameTime;
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
                if (EntityManager.Deleted(selected)) continue;
                if (EntityManager.TryGetComponent<ApcComponent>(selected, out var apcComponent))
                {
                    if (apcComponent.MainBreakerEnabled)
                        _apcSystem.ApcToggleBreaker(selected, apcComponent);
                }
                _unpowered.Add(selected);
            }
        }

        public override void Ended()
        {
            foreach (var entity in _unpowered)
            {
                if (EntityManager.Deleted(entity)) continue;

                if (EntityManager.TryGetComponent(entity, out ApcComponent? apcComponent))
                {
                    if(!apcComponent.MainBreakerEnabled)
                        _apcSystem.ApcToggleBreaker(entity, apcComponent);
                }
            }

            // Can't use the default EndAudio
            _announceCancelToken?.Cancel();
            _announceCancelToken = new CancellationTokenSource();
            Timer.Spawn(3000, () =>
            {
                _audioSystem.PlayGlobal("/Audio/Announcements/power_on.ogg", Filter.Broadcast(), true, AudioParams.Default.WithVolume(-4f));
            }, _announceCancelToken.Token);
            _unpowered.Clear();

            base.Ended();
        }
    }
}
