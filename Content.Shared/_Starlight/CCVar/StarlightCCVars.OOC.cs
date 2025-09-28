using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;

public sealed partial class StarlightCCVars
{
    /// <summary>
    ///     Allow Roleplaying Notes, both public and personal
    /// </summary>
    public static readonly CVarDef<bool> OOCNotes =
        CVarDef.Create("ooc.rp_notes", false, CVar.SERVER | CVar.REPLICATED);
}