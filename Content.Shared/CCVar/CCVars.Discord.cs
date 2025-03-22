using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     The role that will get mentioned if a new SOS ahelp comes in.
    /// </summary>
    public static readonly CVarDef<string> DiscordAhelpMention =
        CVarDef.Create("discord.on_call_ping", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     URL of the discord webhook to relay unanswered ahelp messages.
    /// </summary>
    public static readonly CVarDef<string> DiscordOnCallWebhook =
        CVarDef.Create("discord.on_call_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     URL of the Discord webhook which will relay all ahelp messages.
    /// </summary>
    public static readonly CVarDef<string> DiscordAHelpWebhook =
        CVarDef.Create("discord.ahelp_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     The server icon to use in the Discord ahelp embed footer.
    ///     Valid values are specified at https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure.
    /// </summary>
    public static readonly CVarDef<string> DiscordAHelpFooterIcon =
        CVarDef.Create("discord.ahelp_footer_icon", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     The avatar to use for the webhook. Should be an URL.
    /// </summary>
    public static readonly CVarDef<string> DiscordAHelpAvatar =
        CVarDef.Create("discord.ahelp_avatar", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord webhook which will relay all custom votes. If left empty, disables the webhook.
    /// </summary>
    public static readonly CVarDef<string> DiscordVoteWebhook =
        CVarDef.Create("discord.vote_webhook", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord webhook which will relay all votekick votes. If left empty, disables the webhook.
    /// </summary>
    public static readonly CVarDef<string> DiscordVotekickWebhook =
        CVarDef.Create("discord.votekick_webhook", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord webhook which will relay round restart messages.
    /// </summary>
    public static readonly CVarDef<string> DiscordRoundUpdateWebhook =
        CVarDef.Create("discord.round_update_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Role id for the Discord webhook to ping when the round ends.
    /// </summary>
    public static readonly CVarDef<string> DiscordRoundEndRoleWebhook =
        CVarDef.Create("discord.round_end_role", string.Empty, CVar.SERVERONLY);
  
    /// <summary>
    ///     URL of the Discord webhook which will relay watchlist connection notifications. If left empty, disables the webhook.
    /// </summary>
    public static readonly CVarDef<string> DiscordWatchlistConnectionWebhook =
        CVarDef.Create("discord.watchlist_connection_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     How long to buffer watchlist connections for, in seconds.
    ///     All connections within this amount of time from the first one will be batched and sent as a single
    ///     Discord notification. If zero, always sends a separate notification for each connection (not recommended).
    /// </summary>
    public static readonly CVarDef<float> DiscordWatchlistConnectionBufferTime =
        CVarDef.Create("discord.watchlist_connection_buffer_time", 5f, CVar.SERVERONLY);
  
    /// <summary>
    /// URL of the Discord webhook which will relay last messages before death.
    /// </summary>
    public static readonly CVarDef<string> DiscordLastMessageBeforeDeathWebhook =
        CVarDef.Create("discord.last_message_before_death_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// A maximum length before an IC message is cut off in LastMessageBeforeDeathSystem during formatting.
    /// Can't be less than 1.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxICLength =
        CVarDef.Create("discord.last_message_system_max_ic_length", 100, CVar.SERVERONLY);

    /// <summary>
    /// A maximum length of a discord message that a webhook sends.
    /// Can't be more than 2000 and can't be less than 1.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxMessageLength =
        CVarDef.Create("discord.last_message_system_max_message_length", 2000, CVar.SERVERONLY);

    /// <summary>
    /// A maximum amount of a discord messages that a webhook sends in one batch.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxMessageBatch =
        CVarDef.Create("discord.last_message_system_max_message_batch", 15, CVar.SERVERONLY);

    /// <summary>
    /// Delay in milliseconds between each message the discord webhook sends.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMessageDelay =
        CVarDef.Create("discord.last_message_system_message_delay", 2000, CVar.SERVERONLY);

    /// <summary>
    /// If a maximum amount of messages per batch has been reached, we wait this amount of time (in milliseconds) to send what's left.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxMessageBatchOverflowDelay =
        CVarDef.Create("discord.last_message_system_max_message_batch_overflow_delay", 60000, CVar.SERVERONLY);
}


