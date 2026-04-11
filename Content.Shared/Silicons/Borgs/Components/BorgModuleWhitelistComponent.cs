using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for modules which require the presence or absence
/// of a specific type of module to be installed
/// </summary>
[RegisterComponent, Access(typeof(SharedBorgSystem))]
public sealed partial class BorgModuleWhitelistComponent : Component
{
    /// <summary>
    /// List of module groups this module is a part of.
    /// This only affects examine text.
    /// </summary>
    [DataField]
    public List<LocId>? ModuleTypes;

    /// <summary>
    /// List of module groups this module is incompatible with.
    /// This only affects examine text.
    /// </summary>
    [DataField]
    public List<LocId>? BlacklistedTypes;

    /// <summary>
    /// List of module tags this module is incompatible with
    /// </summary>
    [DataField]
    public EntityWhitelist? ModuleBlacklist;

    /// <summary>
    /// List of module groups this module required to be installed beforehand.
    /// This only affects examine text.
    /// </summary>
    [DataField]
    public List<LocId>? RequiredTypes;

    /// <summary>
    /// List of module tags required for the module to be installed
    /// </summary>
    [DataField]
    public EntityWhitelist? ModuleWhitelist;
}
