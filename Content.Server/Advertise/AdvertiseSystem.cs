using Content.Server.Advertisements;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Server.VendingMachines;
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

        private const float UpdateTimer = 5f;

        private float _timer = 0f;

        public override void Initialize()
        {
            SubscribeLocalEvent<AdvertiseComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<AdvertiseComponent, PowerChangedEvent>(OnPowerChanged);

            SubscribeLocalEvent<ApcPowerReceiverComponent, AdvertiseEnableChangeAttemptEvent>(OnPowerReceiverEnableChangeAttempt);
            SubscribeLocalEvent<VendingMachineComponent, AdvertiseEnableChangeAttemptEvent>(OnVendingEnableChangeAttempt);
        }

        private void OnComponentInit(EntityUid uid, AdvertiseComponent advertise, ComponentInit args)
        {
            RefreshTimer(uid, true, advertise);
        }

        private void OnPowerChanged(EntityUid uid, AdvertiseComponent advertise, ref PowerChangedEvent args)
        {
            SetEnabled(uid, args.Powered, advertise);
        }

        public void RefreshTimer(EntityUid uid, bool minimumBound = true, AdvertiseComponent? advertise = null)
        {
            if (!Resolve(uid, ref advertise))
                return;

            var minWait = Math.Max(1, advertise.MinimumWait);
            var maxWait = Math.Max(minWait, advertise.MaximumWait);

            var waitSeconds = minimumBound ? _random.Next(minWait, maxWait) : _random.Next(maxWait);
            advertise.NextAdvertisementTime = _gameTiming.CurTime.Add(TimeSpan.FromSeconds(waitSeconds));
        }

        public void SayAdvertisement(EntityUid uid, bool refresh = true, AdvertiseComponent? advertise = null)
        {
            if (!Resolve(uid, ref advertise))
                return;

            if (_prototypeManager.TryIndex(advertise.PackPrototypeId, out AdvertisementsPackPrototype? advertisements))
                _chat.TrySendInGameICMessage(advertise.Owner, Loc.GetString(_random.Pick(advertisements.Advertisements)), InGameICChatType.Speak, true);

            if(refresh)
                RefreshTimer(uid, true, advertise);
        }

        public void SetEnabled(EntityUid uid, bool enabled, AdvertiseComponent? advertise = null)
        {
            if (!Resolve(uid, ref advertise))
                return;

            var attemptEvent = new AdvertiseEnableChangeAttemptEvent(enabled, advertise.Enabled);
            RaiseLocalEvent(uid, attemptEvent, false);

            if (attemptEvent.Cancelled)
                return;

            if(enabled)
                RefreshTimer(uid, !advertise.Enabled, advertise);

            advertise.Enabled = enabled;
        }

        private void OnPowerReceiverEnableChangeAttempt(EntityUid uid, ApcPowerReceiverComponent component, AdvertiseEnableChangeAttemptEvent args)
        {
            if(args.NewState && !component.Powered)
                args.Cancel();
        }

        private void OnVendingEnableChangeAttempt(EntityUid uid, VendingMachineComponent component, AdvertiseEnableChangeAttemptEvent args)
        {
            // TODO: Improve this...
            if(args.NewState && component.Broken)
                args.Cancel();
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < UpdateTimer)
                return;

            _timer -= UpdateTimer;

            var curTime = _gameTiming.CurTime;

            foreach (var advertise in EntityManager.EntityQuery<AdvertiseComponent>())
            {
                if (!advertise.Enabled)
                    continue;

                // If it's still not time for the advertisement, do nothing.
                if (advertise.NextAdvertisementTime > curTime)
                    continue;

                SayAdvertisement(advertise.Owner, true, advertise);
            }
        }
    }

    public sealed class AdvertiseEnableChangeAttemptEvent : CancellableEntityEventArgs
    {
        public bool NewState { get; }
        public bool OldState { get; }

        public AdvertiseEnableChangeAttemptEvent(bool newState, bool oldEnabledState)
        {
            NewState = newState;
            OldState = oldEnabledState;
        }
    }
}
