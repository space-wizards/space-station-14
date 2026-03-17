using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> AmbientOcclusion =
        CVarDef.Create("light.ambient_occlusion", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Distance in world-pixels of ambient occlusion.
    /// </summary>
    public static readonly CVarDef<string> AmbientOcclusionColor =
        CVarDef.Create("light.ambient_occlusion_color", "#04080FAA", CVar.CLIENTONLY);

    /// <summary>
    /// Distance in world-pixels of ambient occlusion.
    /// </summary>
    public static readonly CVarDef<float> AmbientOcclusionDistance =
        CVarDef.Create("light.ambient_occlusion_distance", 4f, CVar.CLIENTONLY);
}
