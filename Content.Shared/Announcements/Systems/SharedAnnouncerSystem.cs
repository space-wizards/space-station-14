using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Announcements.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Announcements.Systems;

public abstract class SharedAnnouncerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;


    /// <summary>
    ///     Gets an announcement path from the announcer
    /// </summary>
    /// <param name="announcementId">ID of the announcement from the announcer to get information for</param>
    /// <param name="announcerId">ID of the announcer to use instead of the current one</param>
    public string GetAnnouncementPath(string announcementId, string announcerId)
    {
        if (!_proto.TryIndex<AnnouncerPrototype>(announcerId, out var announcer))
            return "";

        // Get the announcement data from the announcer
        // Will be the fallback if the data for the announcementId is not found
        var announcementType = announcer.Announcements.FirstOrDefault(a => a.ID == announcementId) ??
            announcer.Announcements.First(a => a.ID.ToLower() == "fallback");

        // If the greedy announcementType wants to do the job of announcer, ignore the base path and just return the path
        if (announcementType.IgnoreBasePath)
            return announcementType.Path!;
        // If the announcementType has a collection, get the sound from the collection
        if (announcementType.Collection != null)
            return _audio.GetSound(new SoundCollectionSpecifier(announcementType.Collection));
        // If nothing is overriding the base paths, return the base path + the announcement file path
        return $"{announcer.BasePath}/{announcementType.Path}";
    }

    /// <summary>
    ///     Gets audio params from the announcer
    /// </summary>
    /// <param name="announcementId">ID of the announcement from the announcer to get information for</param>
    /// <param name="announcer">Announcer prototype to get information from</param>
    public string GetAnnouncementPath(string announcementId, AnnouncerPrototype announcer)
    {
        // Get the announcement data from the announcer
        // Will be the fallback if the data for the announcementId is not found
        var announcementType = announcer.Announcements.FirstOrDefault(a => a.ID == announcementId) ??
            announcer.Announcements.First(a => a.ID.ToLower() == "fallback");

        // If the greedy announcementType wants to do the job of announcer, ignore the base path and just return the path
        if (announcementType.IgnoreBasePath)
            return announcementType.Path!;
        // If the announcementType has a collection, get the sound from the collection
        if (announcementType.Collection != null)
            return _audio.GetSound(new SoundCollectionSpecifier(announcementType.Collection));
        // If nothing is overriding the base paths, return the base path + the announcement file path
        return $"{announcer.BasePath}/{announcementType.Path}";
    }

    /// <summary>
    ///     Converts a prototype ID to a consistently used format for announcements
    /// </summary>
    public string GetAnnouncementId(string announcementId, bool ended = false)
    {
        // Replace the first letter with lowercase
        var id = OopsConcat(char.ToLowerInvariant(announcementId[0]).ToString(), announcementId[1..]);

        // If the event has ended, add "Complete" to the end
        if (ended)
            id += "Complete";

        return id;
    }

    private static string OopsConcat(string a, string b)
    {
        // This exists to prevent Roslyn being clever and compiling something that fails sandbox checks.
        return a + b;
    }


    /// <summary>
    ///     Gets audio params from the announcer
    /// </summary>
    /// <param name="announcementId">ID of the announcement from the announcer to get information from</param>
    /// <param name="announcerId">ID of the announcer to use instead of the current one</param>
    public AudioParams? GetAudioParams(string announcementId, string announcerId)
    {
        if (!_proto.TryIndex<AnnouncerPrototype>(announcerId, out var announcer))
            return null;

        // Get the announcement data from the announcer
        // Will be the fallback if the data for the announcementId is not found
        var announcementType = announcer.Announcements.FirstOrDefault(a => a.ID == announcementId) ??
            announcer.Announcements.First(a => a.ID == "fallback");

        // Return the announcer.BaseAudioParams if the announcementType doesn't have an override
        return announcementType.AudioParams ?? announcer.BaseAudioParams ?? null; // For some reason the formatter doesn't warn me about "?? null" being redundant, so it stays for the funnies
    }

    /// <summary>
    ///     Gets audio params from the announcer
    /// </summary>
    /// <param name="announcementId">ID of the announcement from the announcer to get information from</param>
    /// <param name="announcer">Announcer prototype to get information from</param>
    public AudioParams? GetAudioParams(string announcementId, AnnouncerPrototype announcer)
    {
        // Get the announcement data from the announcer
        // Will be the fallback if the data for the announcementId is not found
        var announcementType = announcer.Announcements.FirstOrDefault(a => a.ID == announcementId) ??
            announcer.Announcements.First(a => a.ID == "fallback");

        // Return the announcer.BaseAudioParams if the announcementType doesn't have an override
        return announcementType.AudioParams ?? announcer.BaseAudioParams;
    }


    /// <summary>
    ///     Gets an announcement message from the announcer
    /// </summary>
    /// <param name="announcementId">ID of the announcement from the announcer to get information from</param>
    /// <param name="announcerId">ID of the announcer to get information from</param>
    public string? GetAnnouncementMessage(string announcementId, string announcerId)
    {
        if (!_proto.TryIndex<AnnouncerPrototype>(announcerId, out var announcer))
            return null;

        // Get the announcement data from the announcer
        // Will be the fallback if the data for the announcementId is not found
        var announcementType = announcer.Announcements.FirstOrDefault(a => a.ID == announcementId) ??
            announcer.Announcements.First(a => a.ID == "fallback");

        // Return the announcementType.MessageOverride if it exists, otherwise return null
        return announcementType.MessageOverride != null ? announcementType.MessageOverride : null;
    }

    /// <summary>
    ///     Gets an announcement message from an event ID
    /// </summary>
    /// <param name="eventId">ID of the event to convert</param>
    /// <param name="localeBase">Format for the locale string, replaces "{}" with the converted ID</param>
    /// <remarks>The IDs use a hardcoded format, you can probably handle other formats yourself</remarks>
    /// <returns>Localized announcement</returns>
    public string GetEventLocaleString(string eventId, string localeBase = "station-event-{}-announcement")
    {
        // Replace capital letters with lowercase plus a hyphen before it
        var capsCapture = new Regex("([A-Z])");
        var id = capsCapture.Replace(eventId, "-$1").ToLower();

        // Replace {} with the converted ID
        return localeBase.Replace("{}", id);
    }
}
