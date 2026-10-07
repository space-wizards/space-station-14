using Content.Server.StationEvents.Events;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Content.Shared.Whitelist;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Solar Flare event specific configuration
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(SolarFlareRule))]
public sealed partial class SolarFlareRuleComponent : Component
{
    /// <summary>
    ///     The station that is affected by the solar flare event.
    /// </summary>
    [ViewVariables]
    public EntityUid? AffectedStation;

    [DataField, AutoPausedField]
    public TimeSpan EffectTimer = TimeSpan.Zero;

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
    ///    The entities with lights that were present when the solar flare started.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> AffectedLights = [];

    /// <summary>
    ///     The entities with airlocks that were present when the solar flare started.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> AffectedAirlocks = [];

    /// <summary>
    ///     The entities with air alarms that were present when the solar flare started.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> AffectedAirAlarms = [];

    /// <summary>
    ///    The collection of station records that can be affected by the solar flare event.
    /// </summary>
    [DataField]
    public HashSet<(StationRecordKey, GeneralStationRecord, CriminalRecord)> AffectedStationRecords = [];

    /// <summary>
    ///    The entities with vending machines that were present when the solar flare started.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> AffectedVendingMachines = [];

    /// <summary>
    ///    The lockable entities that were present when the solar flare started.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> AffectedLocks = [];

    /// <summary>
    ///    The link source entities that were present when the solar flare started.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> AffectedLinkSources = [];

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
    ///     Chance per second for each air alarm to have its mode randomized.
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

    /// <summary>
    ///    Chance per second per link source to have its ports invoked.
    /// </summary>
    [DataField]
    public float LinkPortInvokeChance;

    #endregion

    /// <summary>
    ///     A blacklist of device link source prototypes that will not be affected by the solar flare event.
    /// </summary>
    [DataField]
    public EntityWhitelist DeviceLinkSourceBlacklist = new();
}
