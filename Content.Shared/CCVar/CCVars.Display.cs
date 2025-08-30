using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    // Sharpening shader
    public static readonly CVarDef<int> DisplaySharpening =
        CVarDef.Create("display.sharpening", 0, CVar.ARCHIVE | CVar.CLIENTONLY);
}
