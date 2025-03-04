using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> LoocEnabled =
        CVarDef.Create("looc.enabled", true, CVar.NOTIFY | CVar.REPLICATED);

    public static readonly CVarDef<bool> AdminLoocEnabled =
        CVarDef.Create("looc.enabled_admin", true, CVar.NOTIFY);

    /// <summary>
    ///     True: Dead players can use LOOC
    ///     False: Dead player LOOC gets redirected to dead chat
    /// </summary>
    public static readonly CVarDef<bool> DeadLoocEnabled =
        CVarDef.Create("looc.enabled_dead", false, CVar.NOTIFY | CVar.REPLICATED);

    /// <summary>
    ///     True: Crit players can use LOOC
    ///     False: Crit player LOOC gets redirected to dead chat
    /// </summary>
    public static readonly CVarDef<bool> CritLoocEnabled =
        CVarDef.Create("looc.enabled_crit", false, CVar.NOTIFY | CVar.REPLICATED);
}
