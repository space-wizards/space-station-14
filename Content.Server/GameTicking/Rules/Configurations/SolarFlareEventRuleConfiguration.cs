using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.GameTicking.Rules.Configurations;

public sealed class SolarFlareEventRuleConfiguration : StationEventRuleConfiguration
{
    [DataField("minEndAfter")]
    public int MinEndAfter = 120;

    [DataField("maxEndAfter")]
    public int MaxEndAfter = 240;

    [DataField("onlyJamHeadsets")]
    public bool OnlyJamHeadsets = true;

    [DataField("affectedChannels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    public readonly HashSet<string> AffectedChannels = new() { "Common", "Service" };
}