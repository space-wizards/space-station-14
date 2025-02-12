using Content.Shared.Alert;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
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
    #region Brain
    /// <summary>
    /// A whitelist for which entities count as valid brains
    /// </summary>
    [DataField("brainWhitelist")]
    public EntityWhitelist? BrainWhitelist;

    /// <summary>
    /// The container ID for the brain
    /// </summary>
    [DataField]
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
    [DataField, ViewVariables]
    public int MaxModules = 3;

    /// <summary>
    /// The ID for the module container
    /// </summary>
    [DataField]
    public string ModuleContainerId = "borg_module";

    [ViewVariables(VVAccess.ReadWrite)]
    public Container ModuleContainer = default!;

    public int ModuleCount => ModuleContainer.ContainedEntities.Count;
    #endregion

    /// <summary>
    /// The currently selected module
    /// </summary>
    [DataField("selectedModule"), AutoNetworkedField]
    public EntityUid? SelectedModule;

    #region Visuals
    [DataField]
    public string HasMindState = string.Empty;

    [DataField]
    public string NoMindState = string.Empty;
    #endregion

    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";
}

[Serializable, NetSerializable]
public enum BorgVisuals : byte
{
    HasPlayer
}

[Serializable, NetSerializable]
public enum BorgVisualLayers : byte
{
    /// <summary>
    /// Main borg body layer.
    /// </summary>
    Body,

    /// <summary>
    /// Layer for the borg's mind state.
    /// </summary>
    Light,

    /// <summary>
    /// Layer for the borg flashlight status.
    /// </summary>
    LightStatus,
}
