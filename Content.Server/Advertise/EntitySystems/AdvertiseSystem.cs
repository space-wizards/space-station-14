using Content.Server.Advertise.Components;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Advertise.EntitySystems;

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
    private TimeSpan _nextCheckTime = TimeSpan.MinValue;

    public override void Initialize()
    {
        SubscribeLocalEvent<AdvertiseComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ApcPowerReceiverComponent, AttemptAdvertiseEvent>(OnPowerReceiverAttemptAdvertiseEvent);
        SubscribeLocalEvent<VendingMachineComponent, AttemptAdvertiseEvent>(OnVendingAttemptAdvertiseEvent);

        _nextCheckTime = TimeSpan.MinValue;
    }

    private void OnMapInit(EntityUid uid, AdvertiseComponent advertise, MapInitEvent args)
    {
        RefreshTimer(uid, advertise);
        _nextCheckTime = MathHelper.Min(advertise.NextAdvertisementTime, _nextCheckTime);
    }

    private void RefreshTimer(EntityUid uid, AdvertiseComponent? advertise = null)
    {
        if (!Resolve(uid, ref advertise))
            return;

        var minDuration = Math.Max(1, advertise.MinimumWait);
        var maxDuration = Math.Max(minDuration, advertise.MaximumWait);
        var waitDuration = TimeSpan.FromSeconds(_random.Next(minDuration, maxDuration));

        advertise.NextAdvertisementTime = _gameTiming.CurTime + waitDuration;
    }

    public void SayAdvertisement(EntityUid uid, AdvertiseComponent? advertise = null)
    {
        if (!Resolve(uid, ref advertise))
            return;

        if (_prototypeManager.TryIndex(advertise.Pack, out var advertisements))
            _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(advertisements.Messages)), InGameICChatType.Speak, hideChat: true);
    }

    public override void Update(float frameTime)
    {
        var curTime = _gameTiming.CurTime;
        if (_nextCheckTime > curTime)
            return;

        // _nextCheckTime starts at TimeSpan.MinValue, so this has to SET the value, not just increment it.
        _nextCheckTime = curTime + _maximumNextCheckDuration;

        var query = EntityQueryEnumerator<AdvertiseComponent>();
        while (query.MoveNext(out var uid, out var advert))
        {
            if (curTime > advert.NextAdvertisementTime)
            {
                var attemptEvent = new AttemptAdvertiseEvent(uid);
                RaiseLocalEvent(uid, ref attemptEvent);
                if (!attemptEvent.Cancelled)
                {
                    SayAdvertisement(uid, advert);
                }
                // The timer is always refreshed when it expires, to prevent mass advertising (ex: all the vending machines have no power, and get it back at the same time).
                RefreshTimer(uid, advert);
            }
            _nextCheckTime = MathHelper.Min(advert.NextAdvertisementTime, _nextCheckTime);
        }
    }


    private static void OnPowerReceiverAttemptAdvertiseEvent(EntityUid uid, ApcPowerReceiverComponent component, ref AttemptAdvertiseEvent args)
    {
        args.Cancelled |= !component.Powered;
    }

    private static void OnVendingAttemptAdvertiseEvent(EntityUid uid, VendingMachineComponent component, ref AttemptAdvertiseEvent args)
    {
        args.Cancelled |= component.Broken;
    }
}

[ByRefEvent]
public record struct AttemptAdvertiseEvent(EntityUid? Advertiser)
{
    public bool Cancelled = false;
}
