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
    /// Examine text informing the player of what type of module this is
    /// as well as which modules are incompatible or required
    /// </summary>
    [DataField]
    public LocId WhitelistInfo;

    /// <summary>
    /// List of module tags this module is incompatible with
    /// </summary>
    [DataField]
    public EntityWhitelist? ModuleBlacklist;

    /// <summary>
    /// List of module tags required for the module to be installed
    /// </summary>
    [DataField]
    public EntityWhitelist? ModuleWhitelist;
}
