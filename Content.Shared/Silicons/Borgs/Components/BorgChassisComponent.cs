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
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BorgChassisComponent : Component
{
    [DataField("activated"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Activated;

    #region Brain
    [DataField("brainWhitelist")]
    public EntityWhitelist? BrainWhitelist;

    [DataField("brainContainerId")]
    public string BrainContainerId = "borg_brain";

    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot BrainContainer = default!;

    public EntityUid? BrainEntity => BrainContainer.ContainedEntity;

    [DataField("startingBrain", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? StartingBrain;
    #endregion

    #region Modules
    [DataField("moduleWhitelist")]
    public EntityWhitelist? ModuleWhitelist;

    [DataField("maxModules"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxModules = 3;

    [DataField("moduleContainerId")]
    public string ModuleContainerId = "borg_module";

    [ViewVariables(VVAccess.ReadWrite)]
    public Container ModuleContainer = default!;

    public int ModuleCount => ModuleContainer.ContainedEntities.Count;

    [DataField("startingModules", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> StartingModules = new();
    #endregion

    [DataField("borgJobId", customTypeSerializer: typeof(PrototypeIdSerializer<JobPrototype>))]
    public string BorgJobId = "Borg";

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
