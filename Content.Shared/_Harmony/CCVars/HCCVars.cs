using Robust.Shared.Configuration;

namespace Content.Shared._Harmony.CCVars;

/// <summary>
/// Harmony-specific cvars.
/// </summary>
[CVarDefs]
public sealed class HCCVars
{
    /// <summary>
    /// Boolean to define if the round end no EORG message should be skipped.
    /// </summary>
    public static readonly CVarDef<bool> SkipRoundEndNoEorgMessage =
        CVarDef.Create("harmony.skip_roundend_noeorg", false, CVar.CLIENTONLY | CVar.ARCHIVE, "Toggles displaying of No EORG reminder.");

    /// <summary>
    /// Anti-EORG measure. Will add pacified to all players upon round end.
    /// Its not perfect, but gets the job done.
    /// </summary>
    public static readonly CVarDef<bool> RoundEndPacifist =
        CVarDef.Create("game.round_end_pacifist", false, CVar.SERVERONLY);
}
