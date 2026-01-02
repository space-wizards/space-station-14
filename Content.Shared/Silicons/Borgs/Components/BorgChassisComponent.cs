using Content.Shared.Alert;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for the core body of a borg. This manages a borg's
/// "brain", legs, modules, and battery. Essentially the master component
/// for borg logic.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedBorgSystem))]
public sealed partial class BorgChassisComponent : Component
{
    /// <summary>
    /// Is this borg currently activated?
    /// If activated the borg
    /// - can use modules and
    /// - has full movement speed.
    /// To be activated the borg
    /// - needs to have a player controlling it (a mind),
    /// - needs enough charge in its power cell and
    /// - needs to be alive (not crit or dead).
    /// </summary>
    /// <remarks>
    /// Don't try to use ItemToggle for this.
    /// It used that before and it had a ton of conflicts with other item toggling behavior from the billion components that use it somehow.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// The sound to play when the borg activates.
    /// </summary>
    /// <remarks>
    /// The same as the flashlight. This playing used to be a bug, but the sound is nostalgic at this point, so I'm keeping it.
    /// </remarks>
    [DataField]
    public SoundSpecifier ActivateSound = new SoundPathSpecifier("/Audio/Items/flashlight_on.ogg");

    /// <summary>
    /// The sound to play when the borg deactivates.
    /// </summary>
    [DataField]
    public SoundSpecifier DeactivateSound = new SoundPathSpecifier("/Audio/Items/flashlight_off.ogg");

    #region Brain
    /// <summary>
    /// A whitelist for which entities count as valid brains.
    /// </summary>
    [DataField]
    public EntityWhitelist? BrainWhitelist;

    /// <summary>
    /// The container ID for the posibrain or MMI.
    /// </summary>
    [DataField]
    public string BrainContainerId = "borg_brain";

    /// <summary>
    /// The container for the posibrain or MMI.
    /// </summary>
    [ViewVariables]
    public ContainerSlot BrainContainer = default!;

    /// <summary>
    /// The posibrain or MMI inserted into this borg, if any.
    /// </summary>
    [ViewVariables]
    public EntityUid? BrainEntity => BrainContainer?.ContainedEntity;
    #endregion

    #region Modules
    /// <summary>
    /// A whitelist for what types of modules can be installed into this borg.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? ModuleWhitelist;

    /// <summary>
    /// How many modules can be installed in this borg?
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxModules = 3;

    /// <summary>
    /// The ID for the module container.
    /// </summary>
    [DataField]
    public string ModuleContainerId = "borg_module";

    /// <summary>
    /// The module container.
    /// </summary>
    [ViewVariables]
    public Container ModuleContainer = default!;

    /// <summary>
    /// How many modules are currently installed?
    /// </summary>
    [ViewVariables]
    public int ModuleCount => ModuleContainer.ContainedEntities.Count;
    #endregion

    /// <summary>
    /// The currently selected module.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SelectedModule;

    #region Visuals
    [DataField]
    public string HasMindState = string.Empty;

    [DataField]
    public string NoMindState = string.Empty;
    #endregion

    /// <summary>
    /// The battery charge alert.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    /// <summary>
    /// The alert for a missing battery.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";

    /// <summary>
    /// The next update time the battery is checked for automatic reactivation.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBatteryUpdate = TimeSpan.Zero;

    /// <summary>
    /// If the entity can open own UI.
    /// </summary>
    [DataField]
    public bool CanOpenSelfUi;
}

[Serializable, NetSerializable]
public enum BorgVisuals : byte
{
    HasPlayer,
    Powered,
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
