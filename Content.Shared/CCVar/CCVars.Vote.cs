using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Allows enabling/disabling player-started votes for ultimate authority
    /// </summary>
    public static readonly CVarDef<bool> VoteEnabled =
        CVarDef.Create("vote.enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     See vote.enabled, but specific to restart votes
    /// </summary>
    public static readonly CVarDef<bool> VoteRestartEnabled =
        CVarDef.Create("vote.restart_enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     Config for when the restart vote should be allowed to be called regardless with less than this amount of players.
    /// </summary>
    public static readonly CVarDef<int> VoteRestartMaxPlayers =
        CVarDef.Create("vote.restart_max_players", 20, CVar.SERVERONLY);

    /// <summary>
    ///     Config for when the restart vote should be allowed to be called based on percentage of ghosts.
    /// </summary>
    public static readonly CVarDef<int> VoteRestartGhostPercentage =
        CVarDef.Create("vote.restart_ghost_percentage", 55, CVar.SERVERONLY);

    /// <summary>
    ///     See vote.enabled, but specific to preset votes
    /// </summary>
    public static readonly CVarDef<bool> VotePresetEnabled =
        CVarDef.Create("vote.preset_enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     See vote.enabled, but specific to map votes
    /// </summary>
    public static readonly CVarDef<bool> VoteMapEnabled =
        CVarDef.Create("vote.map_enabled", false, CVar.SERVERONLY);

    /// <summary>
    ///     The required ratio of the server that must agree for a restart round vote to go through.
    /// </summary>
    public static readonly CVarDef<float> VoteRestartRequiredRatio =
        CVarDef.Create("vote.restart_required_ratio", 0.85f, CVar.SERVERONLY);

    /// <summary>
    /// Whether or not to prevent the restart vote from having any effect when there is an online admin
    /// </summary>
    public static readonly CVarDef<bool> VoteRestartNotAllowedWhenAdminOnline =
        CVarDef.Create("vote.restart_not_allowed_when_admin_online", true, CVar.SERVERONLY);

    /// <summary>
    ///     The delay which two votes of the same type are allowed to be made by separate people, in seconds.
    /// </summary>
    public static readonly CVarDef<float> VoteSameTypeTimeout =
        CVarDef.Create("vote.same_type_timeout", 240f, CVar.SERVERONLY);

    /// <summary>
    ///     Sets the duration of the map vote timer.
    /// </summary>
    public static readonly CVarDef<int>
        VoteTimerMap = CVarDef.Create("vote.timermap", 90, CVar.SERVERONLY);

    /// <summary>
    ///     Sets the duration of the restart vote timer.
    /// </summary>
    public static readonly CVarDef<int>
        VoteTimerRestart = CVarDef.Create("vote.timerrestart", 60, CVar.SERVERONLY);

    /// <summary>
    ///     Sets the duration of the gamemode/preset vote timer.
    /// </summary>
    public static readonly CVarDef<int>
        VoteTimerPreset = CVarDef.Create("vote.timerpreset", 30, CVar.SERVERONLY);

    /// <summary>
    ///     Sets the duration of the map vote timer when ALONE.
    /// </summary>
    public static readonly CVarDef<int>
        VoteTimerAlone = CVarDef.Create("vote.timeralone", 10, CVar.SERVERONLY);

    /// <summary>
    ///     Allows enabling/disabling player-started votekick for ultimate authority
    /// </summary>
    public static readonly CVarDef<bool> VotekickEnabled =
        CVarDef.Create("votekick.enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     Config for when the votekick should be allowed to be called based on number of eligible voters.
    /// </summary>
    public static readonly CVarDef<int> VotekickEligibleNumberRequirement =
        CVarDef.Create("votekick.eligible_number", 5, CVar.SERVERONLY);

    /// <summary>
    ///     Whether a votekick initiator must be a ghost or not.
    /// </summary>
    public static readonly CVarDef<bool> VotekickInitiatorGhostRequirement =
        CVarDef.Create("votekick.initiator_ghost_requirement", true, CVar.SERVERONLY);

    /// <summary>
    ///     Should the initiator be whitelisted to initiate a votekick?
    /// </summary>
    public static readonly CVarDef<bool> VotekickInitiatorWhitelistedRequirement =
        CVarDef.Create("votekick.initiator_whitelist_requirement", true, CVar.SERVERONLY);

    /// <summary>
    ///     Should the initiator be able to start a votekick if they are bellow the votekick.voter_playtime requirement?
    /// </summary>
    public static readonly CVarDef<bool> VotekickInitiatorTimeRequirement =
        CVarDef.Create("votekick.initiator_time_requirement", false, CVar.SERVERONLY);

    /// <summary>
    ///     Whether a votekick voter must be a ghost or not.
    /// </summary>
    public static readonly CVarDef<bool> VotekickVoterGhostRequirement =
        CVarDef.Create("votekick.voter_ghost_requirement", true, CVar.SERVERONLY);

    /// <summary>
    ///     Config for how many hours playtime a player must have to be able to vote on a votekick.
    /// </summary>
    public static readonly CVarDef<int> VotekickEligibleVoterPlaytime =
        CVarDef.Create("votekick.voter_playtime", 100, CVar.SERVERONLY);

    /// <summary>
    ///     Config for how many seconds a player must have been dead to initiate a votekick / be able to vote on a votekick.
    /// </summary>
    public static readonly CVarDef<int> VotekickEligibleVoterDeathtime =
        CVarDef.Create("votekick.voter_deathtime", 30, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     The required ratio of eligible voters that must agree for a votekick to go through.
    /// </summary>
    public static readonly CVarDef<float> VotekickRequiredRatio =
        CVarDef.Create("votekick.required_ratio", 0.6f, CVar.SERVERONLY);

    /// <summary>
    ///     Whether or not to prevent the votekick from having any effect when there is an online admin.
    /// </summary>
    public static readonly CVarDef<bool> VotekickNotAllowedWhenAdminOnline =
        CVarDef.Create("votekick.not_allowed_when_admin_online", true, CVar.SERVERONLY);

    /// <summary>
    ///     The delay for which two votekicks are allowed to be made by separate people, in seconds.
    /// </summary>
    public static readonly CVarDef<float> VotekickTimeout =
        CVarDef.Create("votekick.timeout", 120f, CVar.SERVERONLY);

    /// <summary>
    ///     Sets the duration of the votekick vote timer.
    /// </summary>
    public static readonly CVarDef<int>
        VotekickTimer = CVarDef.Create("votekick.timer", 60, CVar.SERVERONLY);

    /// <summary>
    ///     Config for how many hours playtime a player must have to get protection from the Raider votekick type when playing as an antag.
    /// </summary>
    public static readonly CVarDef<int> VotekickAntagRaiderProtection =
        CVarDef.Create("votekick.antag_raider_protection", 10, CVar.SERVERONLY);

    /// <summary>
    ///     Default severity for votekick bans
    /// </summary>
    public static readonly CVarDef<string> VotekickBanDefaultSeverity =
        CVarDef.Create("votekick.ban_default_severity", "High", CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Duration of a ban caused by a votekick (in minutes).
    /// </summary>
    public static readonly CVarDef<int> VotekickBanDuration =
        CVarDef.Create("votekick.ban_duration", 180, CVar.SERVERONLY);

    /// <summary>
    ///     Whether the ghost requirement settings for votekicks should be ignored for the lobby.
    /// </summary>
    public static readonly CVarDef<bool> VotekickIgnoreGhostReqInLobby =
        CVarDef.Create("votekick.ignore_ghost_req_in_lobby", true, CVar.SERVERONLY);
}
