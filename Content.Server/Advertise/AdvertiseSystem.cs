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

        /// <summary>
        /// The maximum amount of time between checking if advertisements should be displayed
        /// </summary>
        private readonly TimeSpan _maximumNextCheckDuration = TimeSpan.FromSeconds(15);

        /// <summary>
        /// The next time the game will check if advertisements should be displayed
        /// </summary>
        private TimeSpan _nextCheckTime = TimeSpan.MaxValue;

        public override void Initialize()
        {
            SubscribeLocalEvent<AdvertiseComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<AdvertiseComponent, PowerChangedEvent>(OnPowerChanged);

            SubscribeLocalEvent<ApcPowerReceiverComponent, AdvertiseEnableChangeAttemptEvent>(OnPowerReceiverEnableChangeAttempt);
            SubscribeLocalEvent<VendingMachineComponent, AdvertiseEnableChangeAttemptEvent>(OnVendingEnableChangeAttempt);

            // The component inits will lower this.
            _nextCheckTime = TimeSpan.MaxValue;
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

            var minDuration = Math.Max(1, advertise.MinimumWait);
            var maxDuration = Math.Max(minDuration, advertise.MaximumWait);
            var waitDuration = TimeSpan.FromSeconds(_random.Next(minDuration, maxDuration));
            var nextTime = _gameTiming.CurTime + waitDuration;

            advertise.NextAdvertisementTime = nextTime;

            _nextCheckTime = MathHelper.Min(nextTime, _nextCheckTime);
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

            var query = EntityQueryEnumerator<AdvertiseComponent>();
            while (query.MoveNext(out var uid, out var advert))
            {
                if (!advert.Enabled)
                    continue;

                // If this isn't advertising yet
                if (advert.NextAdvertisementTime > curTime)
                {
                    _nextCheckTime = MathHelper.Min(advert.NextAdvertisementTime, _nextCheckTime);
                    continue;
                }

                SayAdvertisement(uid, advert);
                RefreshTimer(uid, advert);
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
