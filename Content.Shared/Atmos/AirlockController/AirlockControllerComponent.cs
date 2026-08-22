using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos.AirlockController;

/// <summary>
///     A controller that cycles an airlock's atmosphere
///     Uses vents, sensors, doors and cycler panels, with the device network.
///     Has signal outputs for other player use
/// </summary>
/// <remarks>
///     Config is shared / predicted, the cycle is server-side
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AirlockControllerComponent : Component
{
    #region Cycle state

    [DataField]
    public AirlockCycleState State = AirlockCycleState.Idle;

    /// <summary>
    ///     Current atmospheric contents belong to this side
    /// </summary>
    [DataField]
    public AirlockSide CurrentSide = AirlockSide.A;

    /// <summary>
    ///     The side we're currently moving to, if cycling
    /// </summary>
    [DataField]
    public AirlockSide TargetSide = AirlockSide.B;

    /// <summary>
    ///     First cancel press will invert the cycle to the safest possible option.
    /// </summary>
    [DataField]
    public bool CancelRequested;

    /// <summary>
    ///     Why the cycle is stuck, if it is.
    /// </summary>
    [DataField]
    public AirlockStallReason? StallReason;

    /// <summary>
    ///     Configuration mode. Unlocks everything and freezes the controller, for use while installing.
    ///     Starts on since a just made controller has no idea what the right side is
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool MaintenanceMode = true;

    /// <summary>
    ///     Set when the controller needs to put the doors back into the layout expected at Idle in CurrentSide
    /// </summary>
    [DataField]
    public bool RestoreDoors;

    #endregion

    #region Device network

    public readonly Dictionary<string, GasVentPumpData> VentData = new();

    public readonly Dictionary<string, GasVentScrubberData> ScrubberData = new();

    public readonly Dictionary<string, AtmosSensorData> SensorData = new();

    /// <summary>
    ///     Which task each vent is used for
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, AirlockVentRole> VentRoles = new();

    /// <summary>
    ///     Which side each door belongs to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, AirlockSide> DoorRoles = new();

    /// <summary>
    ///     Which side each cycler panel calls the chamber to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, AirlockSide> CyclerRoles = new();

    /// <summary>
    ///     All doors must report status before continuing
    /// </summary>
    public readonly Dictionary<string, AirlockDoorReport> DoorReports = new();

    /// <summary>
    ///     Who we already logged this session
    /// </summary>
    public readonly HashSet<EntityUid> LoggedEditors = new();

    /// <summary>
    ///     If set, these will be used to match target pressure on them for each side. Otherwise, we use presets.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<AirlockSide, EntityUid> TargetSensors = new();

    #endregion

    #region Per-side configuration

    /// <summary>
    ///     Used when no target sensor is assigned to that side.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PresetPressureA = Atmospherics.OneAtmosphere;

    [DataField, AutoNetworkedField]
    public float PresetPressureB;

    #endregion

    #region Tuning

    [DataField]
    public float PressureTolerance = Atmospherics.Epsilon;

    /// <summary>
    ///     At or below this pressure the chamber counts as empty.
    /// </summary>
    [DataField]
    public float EvacuatedPressure = Atmospherics.Epsilon;

    /// <summary>
    ///     Reading is settled when it moves less than this between updates
    /// </summary>
    [DataField]
    public float SettleTolerance = Atmospherics.Epsilon;

    /// <summary>
    ///     Give up waiting for it to settle and move on
    /// </summary>
    [DataField]
    public int MaxSettleTicks = 3;

    [ViewVariables]
    public int SettleTicks;

    [ViewVariables]
    public float LastChamberReading = float.NaN;

    [DataField]
    public TimeSpan StallTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Atmos update pace. Doors run off their replies instead.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextSync;

    /// <summary>
    ///     Set when a door tells us something
    /// </summary>
    [ViewVariables]
    public bool ReportsChanged;

    /// <summary>
    ///     Used for stalling detection
    /// </summary>
    [ViewVariables]
    public float LastProgressValue = float.NaN;

    [ViewVariables]
    public TimeSpan LastProgressTime;

    #endregion

    #region Signals

    /// <summary>
    ///     Held high while the chamber is at side A's atmosphere and idle.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> StateAPort = "AirlockStateA";

    [DataField]
    public ProtoId<SourcePortPrototype> StateBPort = "AirlockStateB";

    /// <summary>
    ///     Held high for the whole cycle. Drives the spinny light and any extra feedback
    ///     someone wires up.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> CyclingPort = "AirlockCycling";

    #endregion

    [DataField]
    public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    /// <summary>
    ///     Bolt wire can becut
    /// </summary>
    [DataField]
    public bool BoltingEnabled = true;

    /// <summary>
    ///     Emergency lights wire can be cut
    /// </summary>
    [DataField]
    public bool EmergencyLightsEnabled = true;

}

/// <summary>
///     The last thing one door told us about itself.
/// </summary>
public struct AirlockDoorReport
{
    public bool Open;

    public bool Bolted;

    /// <summary>
    ///     False for doors with no bolts, like shutters
    /// </summary>
    public bool Boltable;
}
