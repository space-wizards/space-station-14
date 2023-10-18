using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.Database;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.StationEvents.Events;

/// <summary>
///     An abstract entity system inherited by all station events for their behavior.
/// </summary>
public abstract partial class StationEventSystem<T> : GameRuleSystem<T> where T : IComponent
{
    [Dependency] protected readonly IAdminLogManager AdminLogManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom RobustRandom = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] protected readonly ChatSystem ChatSystem = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] protected readonly StationSystem StationSystem = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        Sawmill = Logger.GetSawmill("stationevents");
    }

    /// <inheritdoc/>
    protected override void Added(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        AdminLogManager.Add(LogType.EventAnnounced, $"Event added / announced: {ToPrettyString(uid)}");

        if (stationEvent.StartAnnouncement != null)
        {
            ChatSystem.DispatchGlobalAnnouncement(Loc.GetString(stationEvent.StartAnnouncement), playSound: false, colorOverride: Color.Gold);
        }

        Audio.PlayGlobal(stationEvent.StartAudio, Filter.Broadcast(), true);
        stationEvent.StartTime = _timing.CurTime + stationEvent.StartDelay;
    }

    /// <inheritdoc/>
    protected override void Started(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        AdminLogManager.Add(LogType.EventStarted, LogImpact.High, $"Event started: {ToPrettyString(uid)}");

        if (stationEvent.Duration != null)
        {
            var duration = stationEvent.MaxDuration == null
                ? stationEvent.Duration
                : TimeSpan.FromSeconds(RobustRandom.NextDouble(stationEvent.Duration.Value.TotalSeconds,
                    stationEvent.MaxDuration.Value.TotalSeconds));
            stationEvent.EndTime = _timing.CurTime + duration;
        }
    }

    /// <inheritdoc/>
    protected override void Ended(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        AdminLogManager.Add(LogType.EventStopped, $"Event ended: {ToPrettyString(uid)}");

        if (stationEvent.EndAnnouncement != null)
        {
            ChatSystem.DispatchGlobalAnnouncement(Loc.GetString(stationEvent.EndAnnouncement), playSound: false, colorOverride: Color.Gold);
        }

        Audio.PlayGlobal(stationEvent.EndAudio, Filter.Broadcast(), true);
    }

    /// <summary>
    ///     Called every tick when this event is running.
    ///     Events are responsible for their own lifetime, so this handles starting and ending after time.
    /// </summary>
    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StationEventComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var stationEvent, out var ruleData))
        {
            if (!GameTicker.IsGameRuleAdded(uid, ruleData))
                continue;

            if (!GameTicker.IsGameRuleActive(uid, ruleData) && _timing.CurTime >= stationEvent.StartTime)
            {
                GameTicker.StartGameRule(uid, ruleData);
            }
            else if (stationEvent.EndTime != null && _timing.CurTime >= stationEvent.EndTime && GameTicker.IsGameRuleActive(uid, ruleData))
            {
                GameTicker.EndGameRule(uid, ruleData);
            }
        }
    }

    #region Helper Functions

    protected void ForceEndSelf(EntityUid uid, GameRuleComponent? component = null)
    {
        GameTicker.EndGameRule(uid, component);
    }

    protected bool TryGetRandomStation([NotNullWhen(true)] out EntityUid? station, Func<EntityUid, bool>? filter = null)
    {
        var stations = new ValueList<EntityUid>(Count<StationEventEligibleComponent>());

        filter ??= _ => true;
        var query = AllEntityQuery<StationEventEligibleComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (!filter(uid))
                continue;

            stations.Add(uid);
        }

        if (stations.Count == 0)
        {
            station = null;
            return false;
        }

        // TODO: Engine PR.
        station = stations[RobustRandom.Next(stations.Count)];
        return true;
    }

    protected bool TryFindRandomTile(out Vector2i tile, [NotNullWhen(true)] out EntityUid? targetStation, out EntityUid targetGrid, out EntityCoordinates targetCoords)
    {
        tile = default;

        targetCoords = EntityCoordinates.Invalid;
        if (!TryGetRandomStation(out targetStation))
        {
            targetStation = EntityUid.Invalid;
            targetGrid = EntityUid.Invalid;
            return false;
        }
        var possibleTargets = Comp<StationDataComponent>(targetStation.Value).Grids;
        if (possibleTargets.Count == 0)
        {
            targetGrid = EntityUid.Invalid;
            return false;
        }

        targetGrid = RobustRandom.Pick(possibleTargets);

        if (!TryComp<MapGridComponent>(targetGrid, out var gridComp))
            return false;

        var found = false;
        var (gridPos, _, gridMatrix) = _transform.GetWorldPositionRotationMatrix(targetGrid);
        var gridBounds = gridMatrix.TransformBox(gridComp.LocalAABB);

        for (var i = 0; i < 10; i++)
        {
            var randomX = RobustRandom.Next((int) gridBounds.Left, (int) gridBounds.Right);
            var randomY = RobustRandom.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

            tile = new Vector2i(randomX - (int) gridPos.X, randomY - (int) gridPos.Y);
            if (_atmosphere.IsTileSpace(targetGrid, Transform(targetGrid).MapUid, tile,
                    mapGridComp: gridComp)
                || _atmosphere.IsTileAirBlocked(targetGrid, tile, mapGridComp: gridComp))
            {
                continue;
            }

            found = true;
            targetCoords = gridComp.GridTileToLocal(tile);
            break;
        }

        return found;
    }
    public float GetSeverityModifier()
    {
        var ev = new GetSeverityModifierEvent();
        RaiseLocalEvent(ev);
        return ev.Modifier;
    }

    #endregion
}

/// <summary>
///     Raised broadcast to determine what the severity modifier should be for an event, some positive number that can be multiplied with various things.
///     Handled by usually other game rules (like the ramping scheduler).
///     Most events should try and make use of this if possible.
/// </summary>
public sealed class GetSeverityModifierEvent : EntityEventArgs
{
    /// <summary>
    ///     Should be multiplied/added to rather than set, for commutativity.
    /// </summary>
    public float Modifier = 1.0f;
}
