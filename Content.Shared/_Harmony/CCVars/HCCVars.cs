using Robust.Shared.Configuration;

namespace Content.Shared._Harmony.CCVars;

/// <summary>
/// Harmony-specific cvars.
/// </summary>
[CVarDefs]
public sealed class HCCVars
{
    /// <summary>
    /// Anti-EORG measure. Will add pacified to all players upon round end.
    /// Its not perfect, but gets the job done.
    /// </summary>
    public static readonly CVarDef<bool> RoundEndPacifist =
        CVarDef.Create("game.round_end_pacifist", false, CVar.SERVERONLY);

    /// <summary>
    /// Modifies suicide command to ghost without killing the entity.
    /// </summary>
    public static readonly CVarDef<bool> DisableSuicide =
        CVarDef.Create("ic.disable_suicide", false, CVar.SERVER);
}
