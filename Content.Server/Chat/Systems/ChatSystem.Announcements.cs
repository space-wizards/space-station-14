using System.Text.Json;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    /// <inheritdoc />
    public override void DispatchGlobalAnnouncement(
        string message,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null
        )
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", FormattedMessage.EscapeText(message)));
        _chatManager.ChatMessageToAll(ChatChannel.Radio, message, wrappedMessage, default, false, true, colorOverride);
        if (playSound)
        {
            _audio.PlayGlobal(announcementSound ?? DefaultAnnouncementSound, Filter.Broadcast(), true, AudioParams.Default.WithVolume(-2f));
        }
        _adminLogger.Add(
            LogType.Chat,
            LogImpact.Low,
            $"Global station announcement from {sender}: {message}",
            JsonSerializer.SerializeToDocument(new
            {
                speaker = (int) EntityUid.Invalid,
                message,
                channel = ChatChannel.Radio.ToString(),
                sender,
                scope = "global"
            }));
    }

    /// <inheritdoc />
    public override void DispatchFilteredAnnouncement(
        Filter filter,
        string message,
        EntityUid? source = null,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null)
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", FormattedMessage.EscapeText(message)));
        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, source ?? default, false, true, colorOverride);
        if (playSound)
        {
            _audio.PlayGlobal(announcementSound ?? DefaultAnnouncementSound, filter, true, AudioParams.Default.WithVolume(-2f));
        }
        if (source is { } sourceUid)
        {
            _adminLogger.Add(
                LogType.Chat,
                LogImpact.Low,
                $"Station announcement from {sourceUid:actor} as {sender}: {message}",
                JsonSerializer.SerializeToDocument(new
                {
                    speaker = (int) sourceUid,
                    message,
                    channel = ChatChannel.Radio.ToString(),
                    sender,
                    scope = "filtered"
                }));
        }
        else
        {
            _adminLogger.Add(
                LogType.Chat,
                LogImpact.Low,
                $"Station announcement from {sender}: {message}",
                JsonSerializer.SerializeToDocument(new
                {
                    speaker = (int) EntityUid.Invalid,
                    message,
                    channel = ChatChannel.Radio.ToString(),
                    sender,
                    scope = "filtered"
                }));
        }
    }

    /// <inheritdoc />
    public override void DispatchStationAnnouncement(
        EntityUid source,
        string message,
        string? sender = null,
        bool playDefaultSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null)
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", FormattedMessage.EscapeText(message)));
        var station = _stationSystem.GetOwningStation(source);

        if (station == null)
        {
            // you can't make a station announcement without a station
            return;
        }

        if (!TryComp<StationDataComponent>(station, out var stationDataComp)) return;

        var filter = _stationSystem.GetInStation(stationDataComp);

        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, source, false, true, colorOverride);

        if (playDefaultSound)
        {
            _audio.PlayGlobal(announcementSound ?? DefaultAnnouncementSound, filter, true, AudioParams.Default.WithVolume(-2f));
        }

        _adminLogger.Add(
            LogType.Chat,
            LogImpact.Low,
            $"Station announcement on {station} from {source:actor} as {sender}: {message}",
            JsonSerializer.SerializeToDocument(new
            {
                speaker = (int) source,
                message,
                channel = ChatChannel.Radio.ToString(),
                sender,
                station = station.Value.ToString(),
                scope = "station"
            }));
    }
}
