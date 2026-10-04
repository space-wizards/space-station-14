using Content.Server.StationEvents.Events;
using Content.Shared.Doors.Components;
using Content.Shared.Light.Components;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Content.Server.Atmos.Monitor.Components;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Solar Flare event specific configuration
/// </summary>
[RegisterComponent, Access(typeof(SolarFlareRule))]
public sealed partial class SolarFlareRuleComponent : Component
{
    #region Radio

    /// <summary>
    ///     If true, only headsets affected, but e.g. handheld radio will still work
    /// </summary>
    [DataField("onlyJamHeadsets")]
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
    [DataField("extraCount")]
    public uint ExtraCount;

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

    #endregion

    /// <summary>
    ///     Chance light bulb breaks per second during event
    /// </summary>
    [DataField("lightBreakChancePerSecond")]
    public float LightBreakChancePerSecond;

    /// <summary>
    ///     Chance to apply a random action on a door per second during event.
    /// </summary>
    [DataField("doorToggleChancePerSecond")]
    public float DoorAffectChancePerSecond;

    /// <summary>
    ///     Chance for each air alarm to have its mode randomized when the event starts
    /// </summary>
    [DataField]
    public float AirAlarmModeChangeChancePerSecond;
}
