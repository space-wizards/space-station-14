using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;
[CVarDefs]
public sealed partial class StarlightCCVars
{
    /// <summary>
    /// Basic CCVars
    /// </summary>
    public static readonly CVarDef<string> LobbyChangelogsList =
        CVarDef.Create("lobby_changelog.list", "ChangelogStarlight.yml,Changelog.yml", CVar.SERVER | CVar.REPLICATED);
        
    public static readonly CVarDef<string> ServerName =
        CVarDef.Create("lobby.server_name", "☆ Starlight ☆", CVar.SERVER | CVar.REPLICATED);
        
    /// <summary>
    /// Making everyone a pacifist at the end of a round.
    /// </summary>
    public static readonly CVarDef<bool> PeacefulRoundEnd =
        CVarDef.Create("game.peaceful_end", true, CVar.SERVERONLY);
}
