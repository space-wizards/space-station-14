using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Controls the strength of the sharpness effect.
    /// </summary>
    public static readonly CVarDef<int> DisplaySharpness =
        CVarDef.Create("display.sharpness", 0, CVar.ARCHIVE | CVar.CLIENTONLY);
}
