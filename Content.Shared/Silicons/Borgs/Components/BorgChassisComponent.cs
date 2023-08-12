using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

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
    /// Whether or not the borg currently has a player occupying it
    /// </summary>
    [DataField("hasPlayer")]
    public bool HasPlayer;

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

    /// <summary>
    /// A brain entity that fills the <see cref="BrainContainer"/> on roundstart
    /// </summary>
    [DataField("startingBrain", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? StartingBrain;
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

    /// <summary>
    /// A list of modules that fill the borg on round start.
    /// </summary>
    [DataField("startingModules", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> StartingModules = new();
    #endregion

    /// <summary>
    /// The job that corresponds to borgs
    /// </summary>
    [DataField("borgJobId", customTypeSerializer: typeof(PrototypeIdSerializer<JobPrototype>))]
    public string BorgJobId = "Borg";

    /// <summary>
    /// The currently selected module
    /// </summary>
    [DataField("selectedModule")]
    public EntityUid? SelectedModule;

    /// <summary>
    /// The access this cyborg has when a player is inhabiting it.
    /// </summary>
    [DataField("access"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public string AccessGroup = "AllAccess";

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
