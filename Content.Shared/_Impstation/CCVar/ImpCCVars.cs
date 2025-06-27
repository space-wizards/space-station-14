using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Impstation.CCVar;

// ReSharper disable once InconsistentNaming
[CVarDefs]
public sealed class ImpCCVars : CVars
{
    /// <summary>
    /// Toggles the proximity warping effect on the singularity.
    /// This option is for people who generally do not mind motion, but find
    /// the singularity warping especially egregious.
    /// </summary>
    public static readonly CVarDef<bool> DisableSinguloWarping =
        CVarDef.Create("accessibility.disable_singulo_warping", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Should the player automatically get up after being knocked down
    /// </summary>
    public static readonly CVarDef<bool> AutoGetUp =
        CVarDef.Create("white.auto_get_up", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED); // WD EDIT

    /// <summary>
    /// The number of shared moods to give thaven by default.
    /// </summary>
    public static readonly CVarDef<uint> ThavenSharedMoodCount =
        CVarDef.Create<uint>("thaven.shared_mood_count", 1, CVar.SERVERONLY);

}
