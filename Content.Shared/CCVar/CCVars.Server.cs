using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /*
     * Server
     */

    /// <summary>
    ///     Change this to have the changelog and rules "last seen" date stored separately.
    /// </summary>
    public static readonly CVarDef<string> ServerId =
        CVarDef.Create("server.id", "unknown_server_id", CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Guide Entry Prototype ID to be displayed as the server rules.
    /// </summary>
    public static readonly CVarDef<string> RulesFile =
        CVarDef.Create("server.rules_file", "DefaultRuleset", CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Guide entry that is displayed by default when a guide is opened.
    /// </summary>
    public static readonly CVarDef<string> DefaultGuide =
        CVarDef.Create("server.default_guide", "NewPlayer", CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     If greater than 0, automatically restart the server after this many minutes of uptime.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This is intended to work around various bugs and performance issues caused by long continuous server uptime.
    /// </para>
    /// <para>
    ///     This uses the same non-disruptive logic as update restarts,
    ///     i.e. the game will only restart at round end or when there is nobody connected.
    /// </para>
    /// </remarks>
    public static readonly CVarDef<int> ServerUptimeRestartMinutes =
        CVarDef.Create("server.uptime_restart_minutes", 0, CVar.SERVERONLY);

    /// <summary>
    ///     This will be the title shown in the lobby
    ///     If empty, the title will be {ui-lobby-title} + the server's full name from the hub
    /// </summary>
    public static readonly CVarDef<string> ServerLobbyName =
        CVarDef.Create("server.lobby_name", "", CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     The width of the right side (chat) panel in the lobby
    /// </summary>
    public static readonly CVarDef<int> ServerLobbyRightPanelWidth =
        CVarDef.Create("server.lobby_right_panel_width", 650, CVar.REPLICATED | CVar.SERVER);
}
