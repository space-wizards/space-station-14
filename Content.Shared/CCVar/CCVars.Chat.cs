using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Chat rate limit values are accounted in periods of this size (seconds).
    ///     After the period has passed, the count resets.
    /// </summary>
    /// <seealso cref="ChatRateLimitCount"/>
    public static readonly CVarDef<float> ChatRateLimitPeriod =
        CVarDef.Create("chat.rate_limit_period", 2f, CVar.SERVERONLY);

    /// <summary>
    ///     How many chat messages are allowed in a single rate limit period.
    /// </summary>
    /// <remarks>
    ///     The total rate limit throughput per second is effectively
    ///     <see cref="ChatRateLimitCount"/> divided by <see cref="ChatRateLimitCount"/>.
    /// </remarks>
    /// <seealso cref="ChatRateLimitPeriod"/>
    public static readonly CVarDef<int> ChatRateLimitCount =
        CVarDef.Create("chat.rate_limit_count", 10, CVar.SERVERONLY);

    /// <summary>
    ///     Minimum delay (in seconds) between notifying admins about chat message rate limit violations.
    ///     A negative value disables admin announcements.
    /// </summary>
    public static readonly CVarDef<int> ChatRateLimitAnnounceAdminsDelay =
        CVarDef.Create("chat.rate_limit_announce_admins_delay", 15, CVar.SERVERONLY);

    public static readonly CVarDef<int> ChatMaxMessageLength =
        CVarDef.Create("chat.max_message_length", 1000, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> ChatMaxAnnouncementLength =
        CVarDef.Create("chat.max_announcement_length", 256, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> ChatSanitizerEnabled =
        CVarDef.Create("chat.chat_sanitizer_enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> ChatShowTypingIndicator =
        CVarDef.Create("chat.show_typing_indicator", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> ChatEnableFancyBubbles =
        CVarDef.Create("chat.enable_fancy_bubbles",
            true,
            CVar.CLIENTONLY | CVar.ARCHIVE,
            "Toggles displaying fancy speech bubbles, which display the speaking character's name.");

    public static readonly CVarDef<bool> ChatFancyNameBackground =
        CVarDef.Create("chat.fancy_name_background",
            false,
            CVar.CLIENTONLY | CVar.ARCHIVE,
            "Toggles displaying a background under the speaking character's name.");

    /// <summary>
    ///     A message broadcast to each player that joins the lobby.
    ///     May be changed by admins ingame through use of the "set-motd" command.
    ///     In this case the new value, if not empty, is broadcast to all connected players and saved between rounds.
    ///     May be requested by any player through use of the "get-motd" command.
    /// </summary>
    public static readonly CVarDef<string> MOTD =
        CVarDef.Create("chat.motd",
            "",
            CVar.SERVER | CVar.SERVERONLY | CVar.ARCHIVE,
            "A message broadcast to each player that joins the lobby.");
}
