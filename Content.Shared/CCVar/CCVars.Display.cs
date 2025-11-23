using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Controls the strength of the sharpening effect.
    /// </summary>
    public static readonly CVarDef<int> DisplaySharpening =
        CVarDef.Create("display.sharpening", 0, CVar.ARCHIVE | CVar.CLIENTONLY);
}
