using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Whether or not the primary account of a bank should be listed
    ///     in the funding allocation console
    /// </summary>
    public static readonly CVarDef<bool> AllowPrimaryAccountAllocation =
        CVarDef.Create("cargo.allow_primary_account_allocation", false, CVar.REPLICATED);

    /// <summary>
    ///     Whether or not the primary cut of a bank should be manipulable
    ///     in the funding allocation console
    /// </summary>
    public static readonly CVarDef<bool> AllowPrimaryCutAdjustment =
        CVarDef.Create("cargo.allow_primary_cut_adjustment", true, CVar.REPLICATED);

    /// <summary>
    ///     Whether or not the separate lockbox cut is enabled
    /// </summary>
    public static readonly CVarDef<bool> LockboxCutEnabled =
        CVarDef.Create("cargo.enable_lockbox_cut", true, CVar.REPLICATED);
}
