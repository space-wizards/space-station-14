using Content.Server.Screens.Components;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.TextScreen.Components;
using Content.Shared.TextScreen.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Screens.Systems;

/// <summary>
/// Controls the wallmounted screens on stations and shuttles displaying e.g. FTL duration, ETA
/// </summary>
public sealed partial class ScreenSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private TextScreenSystem _textScreen = default!;

    [Dependency] private EntityQuery<TextScreenTimerComponent> _timerScreenQuery = default!;
    [Dependency] private EntityQuery<TextScreenComponent> _textScreenQuery = default!;

    /// <summary>
    /// Calls either a normal screen text update or shuttle timer update based on the presence of
    /// <see cref="ShuttleTimerMasks.ShuttleMap"/> in <see cref="args.Data"/>
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPacketReceived(Entity<ScreenComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (args.Data.TryGetValue(ShuttleTimerMasks.ShuttleMap, out _))
            ShuttleTimer(ent, args);
        else
            ScreenText(ent, args);
    }

    /// <summary>
    /// Send a text update to every screen on the same MapUid as the originating comms console.
    /// </summary>
    private void ScreenText(Entity<ScreenComponent> ent, DeviceNetworkPacketEvent args)
    {
        // don't allow text updates if there's an active timer
        // (and just check here so the server doesn't have to track them)
        if (!_timerScreenQuery.TryComp(ent, out var timerScreen)
            || timerScreen.TargetTime > _gameTiming.CurTime)
            return;

        var screenMap = Transform(ent).MapUid;
        if (screenMap == null)
            return;

        var argsMap = Transform(args.Sender).MapUid;
        if (screenMap != argsMap)
            return;

        if (!args.Data.TryGetValue(ScreenMasks.Text, out string? text)
            || text == null)
            return;

        _textScreen.SetTimerStrings((ent.Owner, timerScreen), text);
    }

    /// <summary>
    /// Determines if/how a timer packet affects this screen.
    /// Currently there are 2 broadcast domains: Arrivals, and every other screen.
    /// Domain is determined by the <see cref="DeviceNetworkComponent.TransmitFrequencyId"/> on each timer.
    /// Each broadcast domain is divided into subnets. Screen MapUid determines subnet.
    /// Subnets are the shuttle, source, and dest. Source/dest change each jump.
    /// This is required to send different timers to the shuttle/terminal/station.
    /// </summary>
    private void ShuttleTimer(Entity<ScreenComponent> ent, DeviceNetworkPacketEvent args)
    {
        var timerXform = Transform(ent);

        // no false positives.
        if (timerXform.MapUid == null)
            return;

        // Need to have a screen to do anything.
        if (!_timerScreenQuery.TryComp(ent, out var timer))
            return;

        string key;
        args.Data.TryGetValue(ShuttleTimerMasks.ShuttleMap, out EntityUid? shuttleMap);
        args.Data.TryGetValue(ShuttleTimerMasks.SourceMap, out EntityUid? source);
        args.Data.TryGetValue(ShuttleTimerMasks.DestMap, out EntityUid? dest);
        args.Data.TryGetValue(ShuttleTimerMasks.Docked, out bool docked);
        string text = docked ? ShuttleTimerMasks.ETD : ShuttleTimerMasks.ETA;

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
                text = ShuttleTimerMasks.ETA;
                break;
            default:
                return;
        }

        if (!args.Data.TryGetValue(key, out TimeSpan duration))
            return;

        if (args.Data.TryGetValue(ScreenMasks.Text, out string? label) && label != null)
            text = label;

        _textScreen.SetTimerStrings((ent.Owner, timer), text, text);
        _textScreen.SetTimerTarget((ent.Owner, timer), _gameTiming.CurTime + duration);

        if (args.Data.TryGetValue(ScreenMasks.Color, out Color color))
            _textScreen.SetColor(ent.Owner, color);
    }
}
