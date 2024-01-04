using Content.Shared.TextScreen;
using Content.Server.Shuttles.Components;
using Content.Server.DeviceNetwork.Systems;
using Robust.Shared.Timing;


namespace Content.Server.Shuttles.Systems;

/// <summary>
/// Controls the wallmounted screens on stations and shuttles displaying e.g. FTL duration, ETA
/// </summary>
public sealed class ShuttleTimerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShuttleTimerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    /// <summary>
    /// Determines if/how a broadcast packet affects this timer.
    /// All shuttle timer packets are broadcast in their network, and subnetting is implemented by filtering timer MapUid.
    /// </summary>
    private void OnPacketReceived(EntityUid uid, ShuttleTimerComponent component, DeviceNetworkPacketEvent args)
    {
        var timerXform = Transform(uid);

        // no false positives.
        if (timerXform.MapUid == null)
            return;

        string key;
        args.Data.TryGetValue(ShuttleTimerMasks.ShuttleMap, out EntityUid? shuttleMap);
        args.Data.TryGetValue(ShuttleTimerMasks.SourceMap, out EntityUid? source);
        args.Data.TryGetValue(ShuttleTimerMasks.DestMap, out EntityUid? dest);
        args.Data.TryGetValue(ShuttleTimerMasks.Docked, out bool docked);
        string?[] text = new string?[] { docked ? "ETD" : "ETA" };

        switch (timerXform.MapUid)
        {
            // sometimes the timer transforms on FTL shuttles have a hyperspace mapuid, so matching by grid works as a fallback.
            case var local when local == shuttleMap || timerXform.GridUid == shuttleMap:
                key = ShuttleTimerMasks.ShuttleTime;
                break;
            case var origin when origin == source:
                key = ShuttleTimerMasks.SourceTime;
                break;
            case var remote when remote == dest:
                key = ShuttleTimerMasks.DestTime;
                text = new string?[] { "ETA" };
                break;
            default:
                return;
        }

        if (!args.Data.TryGetValue(key, out TimeSpan duration))
            return;

        if (args.Data.TryGetValue(ShuttleTimerMasks.Text, out string?[]? label))
            text = label;

        _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, _gameTiming.CurTime + duration);
        _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, text);
    }
}
