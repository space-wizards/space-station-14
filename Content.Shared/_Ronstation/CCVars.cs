using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{  
    /// <summary>
    ///     Controls the centcomm map prototype to load. SS14 stores these prototypes in Prototypes/Maps.
    /// </summary>
    public static readonly CVarDef<string> CentComm =
        CVarDef.Create("game.centcomm", string.Empty, CVar.SERVERONLY);
}
