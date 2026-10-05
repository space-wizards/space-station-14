using Content.Server.StationEvents.Events;
using Content.Shared.Doors.Components;
using Content.Shared.Light.Components;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Content.Server.Atmos.Monitor.Components;
using Content.Shared.CriminalRecords;
using Content.Shared.Lock;
using Content.Shared.StationRecords;
using Content.Shared.VendingMachines.Components;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Solar Flare event specific configuration
/// </summary>
[RegisterComponent, Access(typeof(SolarFlareRule))]
public sealed partial class SolarFlareRuleComponent : Component
{
    /// <summary>
    ///     The station that is affected by the solar flare event.
    /// </summary>
    [ViewVariables]
    public EntityUid? AffectedStation;

    #region Radio

    /// <summary>
    ///     If true, only headsets affected, but e.g. handheld radio will still work
    /// </summary>
    [DataField]
    public bool OnlyJamHeadsets;

    /// <summary>
    ///     Channels that will be disabled for a duration of event
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> AffectedChannels = new();

    /// <summary>
    ///     List of extra channels that can be random disabled on top of the starting channels.
    /// </summary>
    /// <remarks>
    ///     Channels are not removed from this, so its possible to roll the same channel multiple times.
    /// </remarks>
    [DataField]
    public List<ProtoId<RadioChannelPrototype>> ExtraChannels = new();

    /// <summary>
    ///     Number of times to roll a channel from ExtraChannels.
    /// </summary>
    /// <remarks>
    ///     Channels are not removed from it, so its possible to roll the same channel multiple times.
    /// </remarks>
    [DataField]
    public uint ExtraCount;

    #endregion

    #region Affected collections

    /// <summary>
    ///    The collection of lights that will be affected by the solar flare event.
    /// </summary>
    [DataField]
    public HashSet<(EntityUid, PoweredLightComponent)> AffectedLights = [];

    /// <summary>
    ///     The collection of airlocks that will be affected by the solar flare event.
    /// </summary>
    [DataField]
    public HashSet<(EntityUid, AirlockComponent)> AffectedAirlocks = [];

    /// <summary>
    ///     The collection of air alarms that can be affected by the solar flare event.
    /// </summary>
    [DataField]
    public HashSet<(EntityUid, AirAlarmComponent)> AffectedAirAlarms = [];

    /// <summary>
    ///    The collection of station records that can be affected by the solar flare event.
    /// </summary>
    [DataField]
    public HashSet<(StationRecordKey, GeneralStationRecord, CriminalRecord)> AffectedStationRecords = [];

    /// <summary>
    ///    The collection of vending machines that can be affected by the solar flare event.
    /// </summary>
    [DataField]
    public HashSet<(EntityUid, VendingMachineComponent)> AffectedVendingMachines = [];

    /// <summary>
    ///    The collection of Lockable entities that can be affected by the solar flare event.
    /// </summary>
    [DataField]
    public HashSet<(EntityUid, LockComponent)> AffectedLocks = [];

    #endregion

    #region Event probabilities

    /// <summary>
    ///     Chance per second light bulb breaks during event.
    /// </summary>
    [DataField]
    public float LightBreakChance;

    /// <summary>
    ///     Chance per second to apply a random action on a door during event.
    /// </summary>
    [DataField]
    public float DoorAffectChance;

    /// <summary>
    ///     Chance per second for each air alarm to have its mode randomized when the event starts.
    /// </summary>
    [DataField]
    public float AirAlarmModeChangeChance;

    /// <summary>
    ///     Chance per second per crew member to assign a random criminal status.
    /// </summary>
    [DataField]
    public float ChangeCriminalRecordChance;

    /// <summary>
    ///    Chance per second per vending machine to dispense a random item.
    /// </summary>
    [DataField]
    public float VendChance;

    /// <summary>
    ///    Chance per second per lock to be toggled.
    /// </summary>
    [DataField]
    public float LockToggleChance;

    #endregion
}
