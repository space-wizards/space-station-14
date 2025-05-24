using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Language used for the in-game localization.
    /// </summary>
    public static readonly CVarDef<string> ServerLanguage =
        CVarDef.Create("loc.server_language", "en-US", CVar.SERVER | CVar.REPLICATED);
}
