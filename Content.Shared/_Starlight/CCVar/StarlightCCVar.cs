using Robust.Shared.Configuration;
using Content.Shared.CCVar.CVarAccess;
using Content.Shared.Administration;

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

    [CVarControl(AdminFlags.Adminchat)]
    public static readonly CVarDef<string> OverrideGamemodeName =
        CVarDef.Create("lobby.gamemode_name_override", "", CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);
    [CVarControl(AdminFlags.Adminchat)]
    public static readonly CVarDef<string> OverrideGamemodeDescription =
        CVarDef.Create("lobby.gamemode_desc_override", "", CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);
}
