using Content.Shared.FixedPoint;
using Content.Shared.Alert;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mech.Components;

/// <summary>
/// A large, pilotable machine that has equipment that is
/// powered via an internal battery.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechComponent : Component
{
    /// <summary>
    /// Whether or not an emag disables it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnEmag = true;

    /// <summary>
    /// How much "health" the mech has left.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Integrity;

    /// <summary>
    /// The maximum amount of damage the mech can take.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxIntegrity = 250;

    /// <summary>
    /// The health threshold below which the mech enters broken state.
    /// Broken state is between 0 HP and this value.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BrokenThreshold = 25;

    /// <summary>
    /// Whether this mech can ever be airtight (pressurized cabin capability).
    /// If false, the mech cannot be made airtight.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanAirtight = true;

    /// <summary>
    /// Whether or not the mech is airtight.
    /// When true, the mech uses internal air storage. When false, it uses external air.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Airtight = false;

    /// <summary>
    /// Sound played when entering broken state.
    /// </summary>
    [DataField]
    public SoundSpecifier? BrokenSound;

    /// <summary>
    /// Optional sound played after a pilot successfully enters the mech.
    /// </summary>
    [DataField]
    public SoundSpecifier? EntrySuccessSound;

    /// <summary>
    /// Battery alert to show on the pilot when operating the mech.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    /// <summary>
    /// Alert to show when the mech has no battery installed.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";

    /// <summary>
    /// Health alert to show on the pilot when operating the mech.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> HealthAlert = "MechaHealth";

    /// <summary>
    /// Alert to show when the mech is in a broken state.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BrokenAlert = "MechaBroken";

    /// <summary>
    /// The slot the battery is stored in.
    /// </summary>
    [ViewVariables]
    public ContainerSlot BatterySlot = new();

    [ViewVariables]
    public readonly string BatterySlotId = "cell_slot";

    /// <summary>
    /// Whether the mech is in a broken state.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Broken = false;

    /// <summary>
    /// The slot the pilot is stored in.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot PilotSlot = new();

    [ViewVariables]
    public readonly string PilotSlotId = "mech-pilot-slot";

    #region Equipments
    /// <summary>
    /// The current selected equipment of the mech.
    /// If null, the mech is using just its fists.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? CurrentSelectedEquipment;

    /// <summary>
    /// The maximum amount of equipment items that can be installed in the mech.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxEquipmentAmount = 3;

    /// <summary>
    /// A container for storing the equipment entities.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Container EquipmentContainer = new();

    [ViewVariables]
    public readonly string EquipmentContainerId = "mech-equipment-container";

    /// <summary>
    /// A whitelist for inserting equipment items.
    /// </summary>
    [DataField]
    public EntityWhitelist? EquipmentWhitelist;

    /// <summary>
    /// The equipment that the mech initially has when it spawns.
    /// Good for things like nukie mechs that start with guns.
    /// </summary>
    [DataField]
    public List<EntProtoId> StartingEquipment = [];
    #endregion

    #region Modules
    /// <summary>
    /// Max passive module capacity in space units.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxModuleAmount = 4;

    /// <summary>
    /// A container for storing passive module entities.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Container ModuleContainer = new();

    [ViewVariables]
    public readonly string ModuleContainerId = "mech-passive-module-container";

    /// <summary>
    /// A whitelist for inserting module items.
    /// </summary>
    [DataField]
    public EntityWhitelist? ModuleWhitelist;

    /// <summary>
    /// The passive modules that the mech initially has when it spawns.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> StartingModules = [];
    #endregion

    /// <summary>
    /// How long it takes to enter the mech.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EntryDelay = 3;

    /// <summary>
    /// How long it takes to pull *another person*
    /// outside of the mech.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ExitDelay = 6;

    /// <summary>
    /// How long it takes to pull out the battery.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BatteryRemovalDelay = 2;

    /// <summary>
    /// Energy consumed from the mech's internal battery while actively moving, in charge units per second.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MovementEnergyPerSecond = 5f;

    /// <summary>
    /// Assembly construction graph id to return to on disassembly.
    /// </summary>
    [DataField]
    public string? AssemblyGraphId;

    #region Visualizer States
    [DataField, AutoNetworkedField]
    public string? BaseState;
    [DataField, AutoNetworkedField]
    public string? OpenState;
    [DataField, AutoNetworkedField]
    public string? BrokenState;
    #endregion

    /// <summary>
    /// Time the UI was last updated automatically.
    /// Used to prevent spam updates of energy/pressure values.
    /// </summary>
    public TimeSpan LastUiUpdate;
}

/// <summary>
/// Raised to enable/disable mech movement energy drain for this mech.
/// </summary>
[ByRefEvent]
public readonly record struct MechMovementDrainToggleEvent(bool Enabled);
