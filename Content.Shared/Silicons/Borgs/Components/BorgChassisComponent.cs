using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for the core body of a borg. This manages a borg's
/// "brain", legs, modules, and battery. Essentially the master component
/// for borg logic.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedBorgSystem)), AutoGenerateComponentState]
public sealed partial class BorgChassisComponent : Component
{
    /// <summary>
    /// Whether or not the borg is activated, meaning it has access to modules and a heightened movement speed
    /// </summary>
    [DataField("activated"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Activated;

    #region Brain
    /// <summary>
    /// A whitelist for which entities count as valid brains
    /// </summary>
    [DataField("brainWhitelist")]
    public EntityWhitelist? BrainWhitelist;

    /// <summary>
    /// The container ID for the brain
    /// </summary>
    [DataField("brainContainerId")]
    public string BrainContainerId = "borg_brain";

    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot BrainContainer = default!;

    public EntityUid? BrainEntity => BrainContainer.ContainedEntity;
    #endregion

    #region Modules
    /// <summary>
    /// A whitelist for what types of modules can be installed into this borg
    /// </summary>
    [DataField("moduleWhitelist")]
    public EntityWhitelist? ModuleWhitelist;

    /// <summary>
    /// How many modules can be installed in this borg
    /// </summary>
    [DataField("maxModules"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxModules = 3;

    /// <summary>
    /// The ID for the module container
    /// </summary>
    [DataField("moduleContainerId")]
    public string ModuleContainerId = "borg_module";

    [ViewVariables(VVAccess.ReadWrite)]
    public Container ModuleContainer = default!;

    public int ModuleCount => ModuleContainer.ContainedEntities.Count;
    #endregion

    /// <summary>
    /// The currently selected module
    /// </summary>
    [DataField("selectedModule")]
    public EntityUid? SelectedModule;

    #region Visuals
    [DataField("hasMindState")]
    public string HasMindState = string.Empty;

    [DataField("noMindState")]
    public string NoMindState = string.Empty;
    #endregion
}

[Serializable, NetSerializable]
public enum BorgVisuals : byte
{
    HasPlayer
}

[Serializable, NetSerializable]
public enum BorgVisualLayers : byte
{
    Light
}
