using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> AdminAnnounceLogin =
        CVarDef.Create("admin.announce_login", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> AdminAnnounceLogout =
        CVarDef.Create("admin.announce_logout", true, CVar.SERVERONLY);

    /// <summary>
    ///     The token used to authenticate with the admin API. Leave empty to disable the admin API. This is a secret! Do not share!
    /// </summary>
    public static readonly CVarDef<string> AdminApiToken =
        CVarDef.Create("admin.api_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Should users be able to see their own notes? Admins will be able to see and set notes regardless
    /// </summary>
    public static readonly CVarDef<bool> SeeOwnNotes =
        CVarDef.Create("admin.see_own_notes", false, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Should the server play a quick sound to the active admins whenever a new player joins?
    /// </summary>
    public static readonly CVarDef<bool> AdminNewPlayerJoinSound =
        CVarDef.Create("admin.new_player_join_sound", false, CVar.SERVERONLY);

    /// <summary>
    ///     The amount of days before the note starts fading. It will slowly lose opacity until it reaches stale. Set to 0 to disable.
    /// </summary>
    public static readonly CVarDef<double> NoteFreshDays =
        CVarDef.Create("admin.note_fresh_days", 91.31055, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     The amount of days before the note completely fades, and can only be seen by admins if they press "see more notes". Set to 0
    ///     if you want the note to immediately disappear without fading.
    /// </summary>
    public static readonly CVarDef<double> NoteStaleDays =
        CVarDef.Create("admin.note_stale_days", 365.2422, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     How much time does the user have to wait in seconds before confirming that they saw an admin message?
    /// </summary>
    public static readonly CVarDef<float> MessageWaitTime =
        CVarDef.Create("admin.message_wait_time", 3f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Default severity for role bans
    /// </summary>
    public static readonly CVarDef<string> RoleBanDefaultSeverity =
        CVarDef.Create("admin.role_ban_default_severity", "medium", CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Default severity for department bans
    /// </summary>
    public static readonly CVarDef<string> DepartmentBanDefaultSeverity =
        CVarDef.Create("admin.department_ban_default_severity", "medium", CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Default severity for server bans
    /// </summary>
    public static readonly CVarDef<string> ServerBanDefaultSeverity =
        CVarDef.Create("admin.server_ban_default_severity", "High", CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Whether a server ban will ban the player's ip by default.
    /// </summary>
    public static readonly CVarDef<bool> ServerBanIpBanDefault =
        CVarDef.Create("admin.server_ban_ip_ban_default", true, CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Whether a server ban will ban the player's hardware id by default.
    /// </summary>
    public static readonly CVarDef<bool> ServerBanHwidBanDefault =
        CVarDef.Create("admin.server_ban_hwid_ban_default", true, CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Whether to use details from last connection for ip/hwid in the BanPanel.
    /// </summary>
    public static readonly CVarDef<bool> ServerBanUseLastDetails =
        CVarDef.Create("admin.server_ban_use_last_details", true, CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Whether to erase a player's chat messages and their entity from the game when banned.
    /// </summary>
    public static readonly CVarDef<bool> ServerBanErasePlayer =
        CVarDef.Create("admin.server_ban_erase_player", false, CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Minimum players sharing a connection required to create an alert. -1 to disable the alert.
    /// </summary>
    /// <remarks>
    ///     If you set this to 0 or 1 then it will alert on every connection, so probably don't do that.
    /// </remarks>
    public static readonly CVarDef<int> AdminAlertMinPlayersSharingConnection =
        CVarDef.Create("admin.alert.min_players_sharing_connection", -1, CVar.SERVERONLY);

    /// <summary>
    ///     Minimum explosion intensity to create an admin alert message. -1 to disable the alert.
    /// </summary>
    public static readonly CVarDef<int> AdminAlertExplosionMinIntensity =
        CVarDef.Create("admin.alert.explosion_min_intensity", 60, CVar.SERVERONLY);

    /// <summary>
    ///     Minimum particle accelerator strength to create an admin alert message.
    /// </summary>
    public static readonly CVarDef<int> AdminAlertParticleAcceleratorMinPowerState =
        CVarDef.Create("admin.alert.particle_accelerator_min_power_state", 5, CVar.SERVERONLY); // strength 4

    /// <summary>
    ///     Should the ban details in admin channel include PII? (IP, HWID, etc)
    /// </summary>
    public static readonly CVarDef<bool> AdminShowPIIOnBan =
        CVarDef.Create("admin.show_pii_onban", false, CVar.SERVERONLY);

    /// <summary>
    ///     If an admin joins a round by reading up or using the late join button, automatically
    ///     de-admin them.
    /// </summary>
    public static readonly CVarDef<bool> AdminDeadminOnJoin =
        CVarDef.Create("admin.deadmin_on_join", false, CVar.SERVERONLY);

    /// <summary>
    ///     Overrides the name the client sees in ahelps. Set empty to disable.
    /// </summary>
    public static readonly CVarDef<string> AdminAhelpOverrideClientName =
        CVarDef.Create("admin.override_adminname_in_client_ahelp", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     The threshold of minutes to appear as a "new player" in the ahelp menu
    ///     If 0, appearing as a new player is disabled.
    /// </summary>
    public static readonly CVarDef<int> NewPlayerThreshold =
        CVarDef.Create("admin.new_player_threshold", 0, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     How long an admin client can go without any input before being considered AFK.
    /// </summary>
    public static readonly CVarDef<float> AdminAfkTime =
        CVarDef.Create("admin.afk_time", 600f, CVar.SERVERONLY);

    /// <summary>
    ///     If true, admins are able to connect even if
    ///     <see cref="SoftMaxPlayers"/> would otherwise block regular players.
    /// </summary>
    public static readonly CVarDef<bool> AdminBypassMaxPlayers =
        CVarDef.Create("admin.bypass_max_players", true, CVar.SERVERONLY);

    /// <summary>
    ///     Determines whether admins count towards the total playercount when determining whether the server is over <see cref="SoftMaxPlayers"/>
    ///     Ideally this should be used in conjuction with <see cref="AdminBypassPlayers"/>.
    ///     This also applies to playercount limits in whitelist conditions
    ///     If false, then admins will not be considered when checking whether the playercount is already above the soft player cap
    /// </summary>
    public static readonly CVarDef<bool> AdminsCountForMaxPlayers =
        CVarDef.Create("admin.admins_count_for_max_players", false, CVar.SERVERONLY);

    /// <summary>
    /// Should admins be hidden from the player count reported to the launcher/via api?
    /// This is hub advert safe, in case that's a worry.
    /// </summary>
    public static readonly CVarDef<bool> AdminsCountInReportedPlayerCount =
        CVarDef.Create("admin.admins_count_in_playercount", false, CVar.SERVERONLY);

    /// <summary>
    ///     Determine if custom rank names are used.
    ///     If it is false, it'd use the actual rank name regardless of the individual's title.
    /// </summary>
    /// <seealso cref="AhelpAdminPrefix"/>
    /// <seealso cref="AhelpAdminPrefixWebhook"/>
    public static readonly CVarDef<bool> AdminUseCustomNamesAdminRank =
        CVarDef.Create("admin.use_custom_names_admin_rank", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> BanHardwareIds =
        CVarDef.Create("ban.hardware_ids", true, CVar.SERVERONLY);
}
