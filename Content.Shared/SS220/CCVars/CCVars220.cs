using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

[CVarDefs]
public sealed class CCVars220
{
    /// <summary>
    /// Whether is bloom lighting eanbled or not
    /// </summary>
    public static readonly CVarDef<bool> BloomLightingEnabled =
        CVarDef.Create("bloom_lighting.enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether to rotate doors when map is loaded
    /// </summary>
    public static readonly CVarDef<bool> MigrationAlignDoors =
        CVarDef.Create("map_migration.align_doors", false, CVar.SERVERONLY | CVar.ARCHIVE);
}
