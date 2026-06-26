using Content.Shared.TextScreen;
using Content.Server.Screens.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.RoundEnd;
using Content.Shared.Screens;
using Robust.Shared.Timing;

namespace Content.Server.Screens.Systems;

/// <summary>
/// Controls the wallmounted screens on stations and shuttles displaying e.g. FTL duration, ETA
/// </summary>
public sealed partial class ScreenSystem : DevicePayloadSystem<ScreenComponent>
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<ScreenShuttlePayload>(OnShuttleTimer);
        SubscribePayload<ScreenTextPayload>(OnScreenText);
    }

    /// <summary>
    ///     Send a text update to every screen on the same MapUid as the originating comms console.
    /// </summary>
    private void OnScreenText(Entity<ScreenComponent> ent, ref ScreenTextPayload payload, ref DeviceNetworkPacketData args)
    {
        // don't allow text updates if there's an active timer
        // (and just check here so the server doesn't have to track them)
        if (_appearanceSystem.TryGetData(ent, TextScreenVisuals.TargetTime, out TimeSpan target)
            && target > _gameTiming.CurTime)
            return;

        var screenMap = Transform(ent).MapUid;
        var argsMap = Transform(args.Sender).MapUid;

        if (screenMap == null
            || argsMap == null
            || screenMap != argsMap
            || payload.Text == null)
            return;

        _appearanceSystem.SetData(ent, TextScreenVisuals.DefaultText, payload.Text);
        _appearanceSystem.SetData(ent, TextScreenVisuals.ScreenText, payload.Text);
    }

    /// <summary>
    /// Determines if/how a timer packet affects this screen.
    /// Currently there are 2 broadcast domains: Arrivals, and every other screen.
    /// Domain is determined by the <see cref="Shared.DeviceNetwork.Components.DeviceNetworkComponent.TransmitFrequencyId"/> on each timer.
    /// Each broadcast domain is divided into subnets. Screen MapUid determines subnet.
    /// Subnets are the shuttle, source, and dest. Source/dest change each jump.
    /// This is required to send different timers to the shuttle/terminal/station.
    /// </summary>
    private void OnShuttleTimer(Entity<ScreenComponent> ent, ref ScreenShuttlePayload payload, ref DeviceNetworkPacketData args)
    {
        var timerXform = Transform(ent);

        // no false positives.
        if (timerXform.MapUid == null)
            return;

        string? text = null;
        TimeSpan time;

        switch (timerXform.MapUid)
        {
            // sometimes the timer transforms on FTL shuttles have a hyperspace mapent, so matching by grid works as a fallback.
            case var local when local == GetEntity(payload.Shuttle) || timerXform.GridUid == GetEntity(payload.Shuttle):
                time = payload.ShuttleTime;
                break;
            case var origin when origin == GetEntity(payload.SourceMap):
                time = payload.SourceTime;
                break;
            case var remote when remote == GetEntity(payload.DestinationMap):
                time = payload.DestinationTime;
                text = ShuttleTimerMasks.ETA;
                break;
            default:
                return;
        }

        if (payload.OverrideText != null)
            text = payload.OverrideText;

        _appearanceSystem.SetData(ent, TextScreenVisuals.TargetTime, _gameTiming.CurTime + time);

        if (text != null)
            _appearanceSystem.SetData(ent, TextScreenVisuals.ScreenText, text);

        if (payload.OverrideColor != null)
            _appearanceSystem.SetData(ent, TextScreenVisuals.Color, payload.OverrideColor);
    }
}
