using Content.Server.StationEvents.Events;
using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Solar Flare event specific configuration
/// </summary>
[RegisterComponent, Access(typeof(SolarFlareRule))]
public sealed partial class SolarFlareRuleComponent : Component
{
    /// <summary>
    ///     If true, only headsets affected, but e.g. handheld radio will still work
    /// </summary>
    [DataField("onlyJamHeadsets")]
    public bool OnlyJamHeadsets;

    /// <summary>
    ///     Channels that will be disabled for a duration of event
    /// </summary>
    [DataField("affectedChannels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    public HashSet<string> AffectedChannels = new();

    /// <summary>
    ///     List of extra channels that can be random disabled on top of the starting channels.
    /// </summary>
    /// <remarks>
    ///     Channels are not removed from this, so its possible to roll the same channel multiple times.
    /// </remarks>
    [DataField("extraChannels", customTypeSerializer: typeof(PrototypeIdListSerializer<RadioChannelPrototype>))]
    public List<String> ExtraChannels = new();

    /// <summary>
    ///     Number of times to roll a channel from ExtraChannels.
    /// </summary>
    /// <remarks>
    ///     Channels are not removed from it, so its possible to roll the same channel multiple times.
    /// </remarks>
    [DataField("extraCount")]
    public uint ExtraCount;

    /// <summary>
    ///     Chance light bulb breaks per second during event
    /// </summary>
    [DataField("lightBreakChancePerSecond")]
    public float LightBreakChancePerSecond;

    /// <summary>
    ///     Chance door toggles per second during event
    /// </summary>
    [DataField("doorToggleChancePerSecond")]
    public float DoorToggleChancePerSecond;
}
