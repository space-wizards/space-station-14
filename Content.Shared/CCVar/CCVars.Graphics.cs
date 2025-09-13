using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

// Content-side graphics options
public sealed partial class CCVars
{
    /// <summary>
    /// Enables the heat distortion shader if true.
    /// </summary>
    public static readonly CVarDef<bool> GraphicsHeatDistortion =
        CVarDef.Create("graphics.heat_distortion", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
