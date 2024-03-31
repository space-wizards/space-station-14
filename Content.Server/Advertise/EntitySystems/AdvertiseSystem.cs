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

        var query = EntityQueryEnumerator<AdvertiseComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var advert, out var apc))
        {
            if (curTime > advert.NextAdvertisementTime)
            {
                if (apc.Powered)
                {
                    SayAdvertisement(uid, advert);
                }
                // The timer is refreshed when it expires even if it's off, so it doesn't advertise right when the power comes on.
                RefreshTimer(uid, advert);
            }
            _nextCheckTime = MathHelper.Min(advert.NextAdvertisementTime, _nextCheckTime);
        }
    }
}
