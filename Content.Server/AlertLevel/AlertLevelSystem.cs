using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.AlertLevel;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.AlertLevel;

public sealed class AlertLevelSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    // Until stations are a prototype, this is how it's going to have to be.
    public const string DefaultAlertLevelSet = "stationAlerts";

    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialize);
    }

    public override void Update(float time)
    {
        foreach (var station in _stationSystem.Stations)
        {
            if (!TryComp(station, out AlertLevelComponent? alert))
            {
                continue;
            }

            if (alert.CurrentDelay <= 0)
            {
                if (alert.ActiveDelay)
                {
                    RaiseLocalEvent(new AlertLevelDelayFinishedEvent());
                    alert.ActiveDelay = false;
                }
                continue;
            }

            alert.CurrentDelay--;
        }
    }

    private void OnStationInitialize(StationInitializedEvent args)
    {
        var alertLevelComponent = AddComp<AlertLevelComponent>(args.Station);

        if (!_prototypeManager.TryIndex(DefaultAlertLevelSet, out AlertLevelPrototype? alerts))
        {
            return;
        }

        alertLevelComponent.AlertLevels = alerts;

        var defaultLevel = alertLevelComponent.AlertLevels.DefaultLevel;
        if (string.IsNullOrEmpty(defaultLevel))
        {
            defaultLevel = alertLevelComponent.AlertLevels.Levels.Keys.First();
        }

        SetLevel(args.Station, defaultLevel, false, false, true);
    }

    public float GetAlertLevelDelay(EntityUid station, AlertLevelComponent? alert = null)
    {
        if (!Resolve(station, ref alert))
        {
            return float.NaN;
        }

        return alert.CurrentDelay;
    }

    /// <summary>
    /// Set the alert level based on the station's entity ID.
    /// </summary>
    /// <param name="station">Station entity UID.</param>
    /// <param name="level">Level to change the station's alert level to.</param>
    /// <param name="playSound">Play the alert level's sound.</param>
    /// <param name="announce">Say the alert level's announcement.</param>
    /// <param name="force">Force the alert change. This applies if the alert level is not selectable or not.</param>
    public void SetLevel(EntityUid station, string level, bool playSound, bool announce, bool force = false,
        MetaDataComponent? dataComponent = null, AlertLevelComponent? component = null)
    {
        if (!Resolve(station, ref component, ref dataComponent)
            || component.AlertLevels == null
            || !component.AlertLevels.Levels.TryGetValue(level, out var detail)
            || component.CurrentLevel == level)
        {
            return;
        }

        if (!force)
        {
            if (!detail.Selectable
                || component.CurrentDelay > 0)
            {
                return;
            }

            component.CurrentDelay = AlertLevelComponent.Delay;
            component.ActiveDelay = true;
        }

        component.CurrentLevel = level;

        var stationName = dataComponent.EntityName;

        var name = Loc.GetString($"alert-level-{level}").ToLower();

        // Announcement text. Is passed into announcementFull.
        var announcement = Loc.GetString(detail.Announcement);

        // The full announcement to be spat out into chat.
        var announcementFull = Loc.GetString("alert-level-announcement", ("name", name), ("announcement", announcement));

        var playDefault = false;
        if (playSound)
        {
            if (detail.Sound != null)
            {
                SoundSystem.Play(Filter.Broadcast(), detail.Sound.GetSound());
            }
            else
            {
                playDefault = true;
            }
        }

        if (announce)
        {

            _chatManager.DispatchStationAnnouncement(announcementFull, playDefaultSound: playDefault,
                colorOverride: detail.Color, sender: stationName);
        }

        RaiseLocalEvent(new AlertLevelChangedEvent(level));
    }
}

public sealed class AlertLevelDelayFinishedEvent : EntityEventArgs
{}

public sealed class AlertLevelChangedEvent : EntityEventArgs
{
    public string AlertLevel { get; }

    public AlertLevelChangedEvent(string alertLevel)
    {
        AlertLevel = alertLevel;
    }
}
