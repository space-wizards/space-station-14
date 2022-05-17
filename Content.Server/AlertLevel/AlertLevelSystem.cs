using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.AlertLevel;

public sealed class AlertLevelSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly AdminLogSystem _adminLogSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    // Until stations are a prototype, this is how it's going to have to be.
    public const string DefaultAlertLevelSet = "stationAlerts";

    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialize);
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

        SetLevel(args.Station, defaultLevel, false, false);
    }

    // Set the alert level based on the station's entity ID.
    public void SetLevel(EntityUid station, string level, bool playSound, bool announce, bool force = false, MetaDataComponent? dataComponent = null, AlertLevelComponent? component = null)
    {
        if (!Resolve(station, ref component, ref dataComponent)
            || component.AlertLevels == null
            || !component.AlertLevels.Levels.TryGetValue(level, out var detail)
            || (!detail.Selectable && !force))
        {
            return;
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
            if (!Color.TryFromName(detail.Color, out var color))
            {
                color = Color.White;
            }
            _chatManager.DispatchStationAnnouncement(announcementFull, playDefaultSound: playDefault,
                colorOverride: color, sender: stationName);
        }

        RaiseLocalEvent(new AlertLevelChangedEvent(level));
    }
}

public sealed class AlertLevelChangedEvent : EntityEventArgs
{
    public string AlertLevel { get; }

    public AlertLevelChangedEvent(string alertLevel)
    {
        AlertLevel = alertLevel;
    }
}
