using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> PlayTimeServerEnabled =
        CVarDef.Create("playtimeServer.enabled", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<string> PlayTimeServerUrl =
        CVarDef.Create("playtimeServer.api_url", "", CVar.SERVERONLY);

    public static readonly CVarDef<string> PlayTimeServerApiKey =
        CVarDef.Create("playtimeServer.api_key", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<bool> PlayTimeServerSaveLocally =
        CVarDef.Create("playtimeServer.also_save_locally", false, CVar.SERVERONLY);
}
