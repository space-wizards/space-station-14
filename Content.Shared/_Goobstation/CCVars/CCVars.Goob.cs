using Robust.Shared.Configuration;
// Note: The "atmos.strict_pipe_stacking" CCVar was originally from https://github.com/RonRonstation/ronstation/pull/60, which is mirrored from https://github.com/Goob-Station/Goob-Station/pull/391 with permission, however the _Goobstation folder and the files within it are licensed under AGPL-v3.0 for legal reasons.
namespace Content.Shared._Goobstation.CCVar;

[CVarDefs]
public sealed partial class GoobCVars
{
    /// <summary>
    ///     Whether pipes will unanchor on ANY conflicting connection. May break maps.
    ///     If false, allows you to stack pipes as long as new directions are added (i.e. in a new pipe rotation, layer or multi-Z link), otherwise unanchoring them.
    /// </summary>
    public static readonly CVarDef<bool> StrictPipeStacking =
        CVarDef.Create("atmos.strict_pipe_stacking", false, CVar.SERVERONLY);
}
