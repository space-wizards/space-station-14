using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chat;
using Content.Shared.Station;
using Content.Shared.CCVar;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.AlertLevel;

public sealed class AlertLevelSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    public override void Update(float time)
    {
        var query = EntityQueryEnumerator<AlertLevelComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var station, out var alertComp))
        {
            if (alertComp.DelayedUntil <= curTime)
            {
                // Raise an event so that UIs can refresh.
                var ev = new AlertLevelDelayFinishedEvent();
                RaiseLocalEvent(ref ev);
                alertComp.DelayedUntil = null;
                Dirty(station, alertComp);
            }
        }
    }

    /// <summary>
    /// Can the alert level currently be changed from the communications console?
    /// This considers both the cooldown and if the current mode prevents changing it, for example when the nuke is armed.
    /// </summary>
    public bool CanChangeAlertLevel(Entity<AlertLevelComponent?> station)
    {
        if (!Resolve(station, ref station.Comp))
            return false;

        return station.Comp.DelayedUntil == null && !station.Comp.IsLevelLocked;
    }

    /// <summary>
    /// Gets a list of alert levels that are available on this station and can be selected from the communications console.
    /// </summary>
    public List<ProtoId<AlertLevelPrototype>> GetSelectableAlertLevels(Entity<AlertLevelComponent?> station)
    {
        List<ProtoId<AlertLevelPrototype>> list = new();
        if (!Resolve(station, ref station.Comp))
            return list;

        foreach (var levelId in station.Comp.AvailableAlertLevels)
        {
            if (!_prototype.Resolve(levelId, out var level))
                continue;

            if (level.Selectable)
                list.Add(levelId);
        }

        return list;
    }

    /// <summary>
    /// Gets the current alert level for this station entity.
    /// </summary>
    public bool TryGetLevel(Entity<AlertLevelComponent?> station, [NotNullWhen(true)] out ProtoId<AlertLevelPrototype>? level)
    {
        level = null;
        if (!Resolve(station, ref station.Comp, false))
            return false;

        level = station.Comp.CurrentAlertLevel;
        return true;
    }

    /// <summary>
    /// Gets the default alert level for this station entity.
    /// </summary>
    public bool TryGetDefaultLevel(Entity<AlertLevelComponent?> station, [NotNullWhen(true)] out ProtoId<AlertLevelPrototype>? level)
    {
        level = null;
        if (!Resolve(station, ref station.Comp, false))
            return false;

        level = station.Comp.DefaultAlertLevel;
        return true;
    }

    /// <summary>
    /// Gets the remaining cooldown for changing the alert level from the communications console.
    /// </summary>
    public TimeSpan GetAlertLevelDelay(Entity<AlertLevelComponent?> station)
    {
        if (!Resolve(station, ref station.Comp, false))
            return TimeSpan.Zero;

        if (station.Comp.DelayedUntil == null)
            return TimeSpan.Zero;

        return station.Comp.DelayedUntil.Value - _timing.CurTime;
    }

    /// <summary>
    /// Set the alert level based on the station's entity uid.
    /// </summary>
    /// <param name="station">Station entity UID.</param>
    /// <param name="level">Level to change the station's alert level to.</param>
    /// <param name="playSound">Play the alert level's sound?</param>
    /// <param name="announce">Announce the alert level change in chat?</param>
    /// <param name="force">Force the alert change. This applies if the alert level is not selectable.</param>
    /// <param name="locked">Override to decide if the crew will be able to change from this alert level via the console. Will use the value given in the prototype if null.</param>
    public void SetLevel(
        Entity<AlertLevelComponent?> station,
        ProtoId<AlertLevelPrototype> level,
        bool playSound = true,
        bool announce = true,
        bool force = false,
        bool? locked = null)
    {
        if (!Resolve(station, ref station.Comp))
            return;

        if (station.Comp.CurrentAlertLevel == level)
            return;

        if (!_prototype.Resolve(level, out var prototype))
            return;

        if (!force)
        {
            if (!CanChangeAlertLevel(station) || !prototype.Selectable)
                return;

            station.Comp.DelayedUntil = _timing.CurTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.GameAlertLevelChangeDelay));
        }

        station.Comp.CurrentAlertLevel = level;
        station.Comp.IsLevelLocked = prototype.DisableSelection;
        if (locked != null) // optional override
            station.Comp.IsLevelLocked = locked.Value;
        Dirty(station);

        var stationName = MetaData(station.Owner).EntityName;

        var name = Loc.GetString($"alert-level-{level}");

        // Announcement text passed into announcementFull.
        var announcement = string.Empty;
        if (prototype.Announcement != null)
            announcement = Loc.GetString(prototype.Announcement);

        // The full announcement to be spat out into chat.
        var announcementFull = Loc.GetString("alert-level-announcement", ("name", name), ("announcement", announcement));

        var ev = new AlertLevelChangedEvent(station, level);
        RaiseLocalEvent(ref ev);

        if (_net.IsClient)
            return;

        var playDefault = false;
        if (playSound)
        {
            if (prototype.Sound != null)
            {
                var filter = _station.GetInOwningStation(station);
                _audio.PlayGlobal(prototype.Sound, filter, true);
            }
            else
            {
                playDefault = true;
            }
        }

        if (announce)
        {
            _chat.DispatchStationAnnouncement(
                station,
                announcementFull,
                playDefaultSound: playDefault,
                colorOverride: prototype.Color,
                sender: stationName);
        }

    }
}

/// <summary>
/// Broadcast event that is raised when the alert level selection delay for a station is over and it can be switched again.
/// </summary>
[ByRefEvent]
public record struct AlertLevelDelayFinishedEvent;

/// <summary>
/// Broadcast event that is raised when the a station changes its alert level.
/// </summary>
[ByRefEvent]
public record struct AlertLevelChangedEvent(EntityUid Station, ProtoId<AlertLevelPrototype> AlertLevel);
