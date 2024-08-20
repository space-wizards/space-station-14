using System.Linq;
using Content.Shared.Announcements.Events;
using Content.Shared.Announcements.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Announcements.Systems;

public sealed partial class AnnouncerSystem
{
    /// <summary>
    ///     Gets an announcement message from the announcer
    /// </summary>
    /// <param name="announcementId">ID of the announcement from the announcer to get information from</param>
    private string? GetAnnouncementMessage(string announcementId)
    {
        // Get the announcement data from the announcer
        // Will be the fallback if the data for the announcementId is not found
        var announcementType = Announcer.Announcements.FirstOrDefault(a => a.ID == announcementId) ??
            Announcer.Announcements.First(a => a.ID == "fallback");

        // Return the announcementType.MessageOverride if it exists, otherwise return null
        return announcementType.MessageOverride != null ? Loc.GetString(announcementType.MessageOverride) : null;
    }


    /// <summary>
    ///     Sends an announcement audio
    /// </summary>
    /// <param name="announcementId">ID of the announcement to get information from</param>
    /// <param name="filter">Who hears the announcement audio</param>
    /// <param name="announcerOverride">Uses this announcer instead of the current global one</param>
    public void SendAnnouncementAudio(string announcementId, Filter filter, AnnouncerPrototype? announcerOverride = null)
    {
        var ev = new AnnouncementSendEvent(
            announcerOverride?.ID ?? Announcer.ID,
            announcementId,
            filter.Recipients.ToList().ConvertAll(p => p.UserId), // I hate this but IEnumerable isn't serializable, and then ICommonSession wasn't, so you get the User ID
            GetAudioParams(announcementId, Announcer) ?? AudioParams.Default
        );

        RaiseNetworkEvent(ev);
    }

    /// <summary>
    ///     Sends an announcement message
    /// </summary>
    /// <param name="announcementId">ID of the announcement to get information from</param>
    /// <param name="locale">Text to send in the announcement</param>
    /// <param name="sender">Who to show as the announcement announcer, defaults to the current announcer's name</param>
    /// <param name="colorOverride">What color the announcement should be</param>
    /// <param name="station">Station ID to send the announcement to</param>
    /// <param name="announcerOverride">Uses this announcer instead of the current global one</param>
    /// <param name="localeArgs">Locale arguments to pass to the announcement message</param>
    public void SendAnnouncementMessage(string announcementId, string locale, string? sender = null,
        Color? colorOverride = null, EntityUid? station = null, AnnouncerPrototype? announcerOverride = null,
        params (string, object)[] localeArgs)
    {
        sender ??= Loc.GetString($"announcer-{announcerOverride?.ID ?? Announcer.ID}-name");

        // If the announcement has a message override, use that instead of the message parameter
        if (GetAnnouncementMessage(announcementId, announcerOverride?.ID ?? Announcer.ID) is { } announcementMessage)
            locale = Loc.GetString(announcementMessage, localeArgs);
        else
            locale = Loc.GetString(locale, localeArgs);

        // Don't send nothing
        if (string.IsNullOrEmpty(locale))
            return;

        // If there is a station, send the announcement to the station, otherwise send it to everyone
        if (station == null)
            _chat.DispatchGlobalAnnouncement(locale, sender, false, colorOverride: colorOverride);
        else
            _chat.DispatchStationAnnouncement(station.Value, locale, sender, false, colorOverride: colorOverride);
    }

    /// <summary>
    ///     Sends an announcement with a message and audio
    /// </summary>
    /// <param name="announcementId">ID of the announcement to get information from</param>
    /// <param name="filter">Who hears the announcement audio</param>
    /// <param name="locale">Text to send in the announcement</param>
    /// <param name="sender">Who to show as the announcement announcer, defaults to the current announcer's name</param>
    /// <param name="colorOverride">What color the announcement should be</param>
    /// <param name="station">Station ID to send the announcement to</param>
    /// <param name="announcerOverride">Uses this announcer instead of the current global one</param>
    /// <param name="localeArgs">Locale arguments to pass to the announcement message</param>
    public void SendAnnouncement(string announcementId, Filter filter, string locale, string? sender = null,
        Color? colorOverride = null, EntityUid? station = null, AnnouncerPrototype? announcerOverride = null,
        params (string, object)[] localeArgs)
    {
        SendAnnouncementAudio(announcementId, filter, announcerOverride);
        SendAnnouncementMessage(announcementId, locale, sender, colorOverride, station, announcerOverride, localeArgs);
    }
}
