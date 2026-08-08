using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    // SS13 uses #8589fa but it can come off as more harsh so we muted it a bit more.
    public const string DefaultSpaceLightColor = "#8487db";

    /// <summary>
    /// Default space light color, in sRGB hex.
    /// </summary>
    public static readonly CVarDef<string> SpaceLightColor =
        CVarDef.Create("light.space_light_color", DefaultSpaceLightColor, CVar.SERVERONLY);

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
