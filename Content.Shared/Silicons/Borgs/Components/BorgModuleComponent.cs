using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for modules that can be inserted into borgs
/// to give them unique abilities and attributes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBorgSystem))]
public sealed partial class BorgModuleComponent : Component
{
    /// <summary>
    /// The entity this module is installed into.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? InstalledEntity;

    /// <summary>
    /// Is this module currently installed?
    /// </summary>
    [ViewVariables]
    public bool Installed => InstalledEntity != null;

    /// <summary>
    /// If true, this is a "default" module that cannot be removed from a borg.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DefaultModule;

    /// <summary>
    /// List of types of borgs this module fits into.
    /// This only affects examine text. The actual whitelist for modules that can be inserted into a borg is defined in its <see cref="BorgChassisComponent"/>.
    /// </summary>
    [DataField]
    public HashSet<LocId>? BorgFitTypes;
}

/// <summary>
/// Raised on a module when it is installed in order to add specific behavior to an entity.
/// </summary>
/// <param name="ChassisEnt">The borg the module is being installed in.</param>
[ByRefEvent]
public readonly record struct BorgModuleInstalledEvent(EntityUid ChassisEnt);

/// <summary>
/// Raised on a module when it's uninstalled in order to
/// </summary>
/// <param name="ChassisEnt">The borg the module is being uninstalled from.</param>
[ByRefEvent]
public readonly record struct BorgModuleUninstalledEvent(EntityUid ChassisEnt);
