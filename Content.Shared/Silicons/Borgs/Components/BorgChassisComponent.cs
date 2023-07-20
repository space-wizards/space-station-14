using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for the core body of a borg. This manages a borg's
/// "brain", legs, modules, and battery. Essentially the master component
/// for borg logic.
/// </summary>
[RegisterComponent]
public sealed class BorgChassisComponent : Component
{
    #region Brain
    [DataField("brainWhitelist")]
    public EntityWhitelist? BrainWhitelist;

    [DataField("brainContainerId")]
    public string BrainContainerId = "borg_brain";

    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot BrainContainer = default!;

    public EntityUid? BrainEntity => BrainContainer.ContainedEntity;
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
    #endregion

    [DataField("borgJobId", customTypeSerializer: typeof(PrototypeIdSerializer<JobPrototype>))]
    public string BorgJobId = "Borg";

    [DataField("currentProviderModule")]
    public EntityUid? CurrentProviderModule;
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
