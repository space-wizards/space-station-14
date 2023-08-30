using Content.Server.Advertisements;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Advertise
{
    public sealed class AdvertiseSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ChatSystem _chat = default!;


        // The maximum amount of time between checking advertisements
        private readonly TimeSpan _maximumNextCheckDuration = TimeSpan.FromSeconds(15);
        // The minimum amount of time between checking advertisements
        private readonly TimeSpan _minimumNextCheckDuration = TimeSpan.FromSeconds(1);

        // The next game time the system will check advertisements
        private TimeSpan _nextCheckTime = TimeSpan.MinValue;

        public override void Initialize()
        {
            SubscribeLocalEvent<AdvertiseComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<AdvertiseComponent, PowerChangedEvent>(OnPowerChanged);

            SubscribeLocalEvent<ApcPowerReceiverComponent, AdvertiseEnableChangeAttemptEvent>(OnPowerReceiverEnableChangeAttempt);
            SubscribeLocalEvent<VendingMachineComponent, AdvertiseEnableChangeAttemptEvent>(OnVendingEnableChangeAttempt);
        }

        private void OnComponentInit(EntityUid uid, AdvertiseComponent advertise, ComponentInit args)
        {
            RefreshTimer(uid, advertise);
        }

        private void OnPowerChanged(EntityUid uid, AdvertiseComponent advertise, ref PowerChangedEvent args)
        {
            SetEnabled(uid, args.Powered, advertise);
        }

        public void RefreshTimer(EntityUid uid, AdvertiseComponent? advertise = null)
        {
            if (!Resolve(uid, ref advertise))
                return;
            if (!advertise.Enabled)
                return;
            var minWait = Math.Max(1, advertise.MinimumWait);
            var maxWait = Math.Max(minWait, advertise.MaximumWait);
            var secondsToWait = _random.Next(minWait, maxWait);
            var nextTime = _gameTiming.CurTime +
                           TimeSpan.FromSeconds(secondsToWait);
            advertise.NextAdvertisementTime = nextTime;
            if (nextTime < _nextCheckTime)
            {
                _nextCheckTime = nextTime;
            }
        }

        public void SayAdvertisement(EntityUid uid, AdvertiseComponent? advertise = null)
        {
            if (!Resolve(uid, ref advertise))
                return;

            if (_prototypeManager.TryIndex(advertise.PackPrototypeId, out AdvertisementsPackPrototype? advertisements))
                _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(advertisements.Advertisements)), InGameICChatType.Speak, true);
        }

        public void SetEnabled(EntityUid uid, bool enable, AdvertiseComponent? advertise = null)
        {
            if (!Resolve(uid, ref advertise))
                return;

            if (advertise.Enabled == enable)
                return;

            var attemptEvent = new AdvertiseEnableChangeAttemptEvent(enable);
            RaiseLocalEvent(uid, attemptEvent);

            if (attemptEvent.Cancelled)
                return;

            advertise.Enabled = enable;
            RefreshTimer(uid, advertise);
        }

        private static void OnPowerReceiverEnableChangeAttempt(EntityUid uid, ApcPowerReceiverComponent component, AdvertiseEnableChangeAttemptEvent args)
        {
            if(args.Enabling && !component.Powered)
                args.Cancel();
        }

        private static void OnVendingEnableChangeAttempt(EntityUid uid, VendingMachineComponent component, AdvertiseEnableChangeAttemptEvent args)
        {
            if(args.Enabling && component.Broken)
                args.Cancel();
        }

        public override void Update(float frameTime)
        {
            var curTime = _gameTiming.CurTime;
            if (_nextCheckTime > curTime)
                return;

            _nextCheckTime = curTime + _maximumNextCheckDuration;
            foreach (var (transform, advertise) in EntityManager.EntityQuery<TransformComponent, AdvertiseComponent>())
            {
                if (!advertise.Enabled)
                    continue;

                if (advertise.NextAdvertisementTime > curTime)
                {
                    if (advertise.NextAdvertisementTime < _nextCheckTime)
                    {
                        _nextCheckTime = advertise.NextAdvertisementTime;
                    }
                    continue;
                }

                SayAdvertisement(transform.ParentUid, advertise);
                RefreshTimer(transform.ParentUid, advertise);
            }

            var minimumTime = curTime + _minimumNextCheckDuration;
            if (_nextCheckTime < minimumTime)
            {
                _nextCheckTime = minimumTime;
            }
        }
    }

    public sealed class AdvertiseEnableChangeAttemptEvent : CancellableEntityEventArgs
    {
        public bool Enabling { get; }

        public AdvertiseEnableChangeAttemptEvent(bool enabling)
        {
            Enabling = enabling;
        }
    }
}
