using Content.Server.Administration.Logs;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat.V2;
using Content.Shared.Database;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Replays;

namespace Content.Server.Chat.V2;

public sealed class ServerAnnouncementSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private const string DefaultAnnouncementSound = "/Audio/Announcements/announce.ogg";
    private const string DefaultAnnouncementChannel = "Common";

    /// <summary>
    /// Dispatches an announcement to all.
    /// </summary>
    /// <param name="message">The contents of the message</param>
    /// <param name="sender">The sender (Communications Console in Communications Console Announcement)</param>
    /// <param name="playSound">Play the announcement sound</param>
    /// <param name="colorOverride">Optional color for the announcement message</param>
    public void DispatchGlobalAnnouncement(
        string message,
        string sender = "Central Command",
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null
        )
    {
        var msgOut = new EntityRadioedEvent(
            GetNetEntity(default),
            sender,
            message,
            DefaultAnnouncementChannel,
            isAnnouncement: true,
            messageColorOverride: colorOverride
        );

        RaiseNetworkEvent(msgOut);
        _replay.RecordServerMessage(msgOut);

        if (playSound)
        {
            _audio.PlayGlobal(announcementSound?.GetSound() ?? DefaultAnnouncementSound,
                Filter.Broadcast(),
                true, AudioParams.Default.WithVolume(-2f)
            );
        }

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Global station announcement from {sender}: {message}");
    }

    /// <summary>
    /// Dispatches an announcement on a specific station
    /// </summary>
    /// <param name="source">The entity making the announcement (used to determine the station)</param>
    /// <param name="message">The contents of the message</param>
    /// <param name="sender">The sender (Communications Console in Communications Console Announcement)</param>
    /// <param name="playDefaultSound">Play the announcement sound</param>
    /// <param name="colorOverride">Optional color for the announcement message</param>
    public void DispatchStationAnnouncement(
        EntityUid source,
        string message,
        string sender = "Central Command",
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null)
    {
        var station = _stationSystem.GetOwningStation(source);

        if (station == null)
            return;

        if (!TryComp<StationDataComponent>(station, out var stationDataComp))
            return;

        var filter = _stationSystem.GetInStation(stationDataComp);

        var msgOut = new EntityRadioedEvent(
            GetNetEntity(default),
            sender,
            message,
            DefaultAnnouncementChannel,
            isAnnouncement: true,
            messageColorOverride: colorOverride
        );

        RaiseNetworkEvent(msgOut, filter);

        _replay.RecordServerMessage(msgOut);

        if (playSound)
        {
            _audio.PlayGlobal(announcementSound?.GetSound() ?? DefaultAnnouncementSound,
                Filter.Broadcast(),
                true, AudioParams.Default.WithVolume(-2f)
            );
        }

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Station Announcement on {station} from {sender}: {message}");
    }
}
