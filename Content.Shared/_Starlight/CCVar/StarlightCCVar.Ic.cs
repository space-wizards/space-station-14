using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;

public sealed partial class StarlightCCVars
{
    /// <summary>
    /// Restricts IC character custom specie names so they cannot be others species.
    /// </summary>
    public static readonly CVarDef<bool> RestrictedCustomSpecieNames =
        CVarDef.Create("ic.restricted_customspecienames", true, CVar.SERVER | CVar.REPLICATED);
}
