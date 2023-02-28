using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.GameTicking.Rules.Configurations;

/// <summary>
///     Solar Flare event specific configuration
/// </summary>
public sealed class SolarFlareEventRuleConfiguration : StationEventRuleConfiguration
{
    /// <summary>
    ///     In seconds, most early moment event can end
    /// </summary>
    [DataField("minEndAfter")]
    public int MinEndAfter;

    /// <summary>
    ///     In seconds, most late moment event can end
    /// </summary>
    [DataField("maxEndAfter")]
    public int MaxEndAfter;

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
    ///     Chance any given light bulb breaks due to event
    /// </summary>
    [DataField("lightBreakChance")]
    public float LightBreakChance;
}