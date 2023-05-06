using Content.Server.StationEvents.Events;
using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Solar Flare event specific configuration
/// </summary>
[RegisterComponent, Access(typeof(SolarFlareRule))]
public sealed class SolarFlareRuleComponent : Component
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
    public readonly HashSet<string> AffectedChannels = new();

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
