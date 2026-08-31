using Content.Server.Screens.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.RoundEnd;
using Content.Shared.Screens;
using Content.Shared.TextScreen;
using Robust.Shared.Timing;

namespace Content.Server.Screens.Systems;

/// <summary>
/// Controls the wallmounted screens on stations and shuttles displaying e.g. FTL duration, ETA
/// </summary>
public sealed partial class ScreenSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery;

    /// <summary>
    /// Send a text update to every screen on the same MapUid as the originating comms console.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnScreenText(Entity<ScreenComponent> ent, ref DeviceNetworkPacketEvent<ScreenTextPayload> args)
    {
        var text = args.Data.Text;
        if (text == null)
            return;

        // don't allow text updates if there's an active timer
        // (and just check here so the server doesn't have to track them)
        if (!_appearanceQuery.TryComp(ent, out var appearance))
            return;

        if (_appearance.TryGetData(ent, TextScreenVisuals.TargetTime, out TimeSpan target, appearance)
            && target > _gameTiming.CurTime)
            return;

        var screenMap = Transform(ent).MapUid;
        if (screenMap == null)
            return;

        var argsMap = Transform(args.Sender).MapUid;
        if (screenMap != argsMap)
            return;

        _appearance.SetData(ent, TextScreenVisuals.DefaultText, text, appearance);
        _appearance.SetData(ent, TextScreenVisuals.ScreenText, text, appearance);
        _appearance.SetData(ent, TextScreenVisuals.ScreenTextTime, _gameTiming.CurTime, appearance);
    }

    /// <summary>
    /// Determines if/how a timer packet affects this screen.
    /// Currently there are 2 broadcast domains: Arrivals, and every other screen.
    /// Domain is determined by the <see cref="DeviceNetworkComponent.TransmitFrequencyId"/> on each timer.
    /// Each broadcast domain is divided into subnets. Screen MapUid determines subnet.
    /// Subnets are the shuttle, source, and dest. Source/dest change each jump.
    /// This is required to send different timers to the shuttle/terminal/station.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnShuttleTimer(Entity<ScreenComponent> ent, ref DeviceNetworkPacketEvent<ScreenShuttlePayload> args)
    {
        var payload = args.Data;
        var timerXform = Transform(ent);

        // no false positives.
        if (timerXform.MapUid == null)
            return;

        // Need to have AppearanceData to do anything.
        if (!_appearanceQuery.TryComp(ent, out var appearance))
            return;

        string? text = null;
        TimeSpan time;

        switch (timerXform.MapUid)
        {
            // sometimes the timer transforms on FTL shuttles have a hyperspace mapent, so matching by grid works as a fallback.
            case var local when local == payload.Shuttle || timerXform.GridUid == payload.Shuttle:
                time = payload.ShuttleTime;
                break;
            case var origin when origin == payload.SourceMap:
                time = payload.SourceTime;
                break;
            case var remote when remote == payload.DestinationMap:
                time = payload.DestinationTime;
                text = ShuttleTimerMasks.ETA;
                break;
            default:
                return;
        }

        if (payload.OverrideText != null)
            text = payload.OverrideText;

        _appearance.SetData(ent, TextScreenVisuals.TargetTime, _gameTiming.CurTime + time, appearance);
        _appearance.SetData(ent, TextScreenVisuals.ScreenTextTime, _gameTiming.CurTime, appearance);

        if (text != null)
            _appearance.SetData(ent, TextScreenVisuals.ScreenText, text, appearance);

        if (payload.OverrideColor != null)
            _appearance.SetData(ent, TextScreenVisuals.Color, payload.OverrideColor, appearance);
    }
}
