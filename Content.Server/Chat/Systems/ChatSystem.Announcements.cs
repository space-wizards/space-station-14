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
        Color? colorOverride = null,
        string? signature = null,
        ICommonSession? actor = null
        )
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        var wrappedMessage = WrapAnnouncement(sender, message, signature);
        _chatManager.ChatMessageToAll(ChatChannel.Radio, message, wrappedMessage, default, false, true, colorOverride);
        if (playSound)
        {
            _audio.PlayGlobal(announcementSound ?? DefaultAnnouncementSound, Filter.Broadcast(), true, AudioParams.Default.WithVolume(-2f));
        }
        LogAnnouncement("Global station announcement", sender, message, actor);
    }

    /// <inheritdoc />
    public override void DispatchFilteredAnnouncement(
        Filter filter,
        string message,
        EntityUid? source = null,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        string? signature = null,
        ICommonSession? actor = null)
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        var wrappedMessage = WrapAnnouncement(sender, message, signature);
        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, source ?? default, false, true, colorOverride);
        if (playSound)
        {
            _audio.PlayGlobal(announcementSound ?? DefaultAnnouncementSound, filter, true, AudioParams.Default.WithVolume(-2f));
        }
        LogAnnouncement("Station announcement", sender, message, actor);
    }

    /// <inheritdoc />
    public override void DispatchStationAnnouncement(
        EntityUid source,
        string message,
        string? sender = null,
        bool playDefaultSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        string? signature = null,
        ICommonSession? actor = null)
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        var wrappedMessage = WrapAnnouncement(sender, message, signature);
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

        LogAnnouncement($"Station announcement on {station}", sender, message, actor);
    }

    private void LogAnnouncement(string scope, string sender, string message, ICommonSession? actor)
    {
        if (actor == null)
        {
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{scope} from {sender}: {message}");
            return;
        }

        _adminLogger.Add(LogType.Chat, LogImpact.Low,
            $"{scope} from {sender}, initiated by {actor:Player}: {message}");
    }

    private string WrapAnnouncement(string sender, string message, string? signature)
    {
        var escapedSender = FormattedMessage.EscapeText(sender);
        var escapedMessage = FormattedMessage.EscapeText(message);
        return string.IsNullOrWhiteSpace(signature)
            ? Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", escapedSender), ("message", escapedMessage))
            : Loc.GetString("chat-manager-sender-announcement-wrap-message-signed", ("sender", escapedSender), ("message", escapedMessage), ("signature", FormattedMessage.EscapeText(signature)));
    }
}
