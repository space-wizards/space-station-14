using Content.Server.Objectives;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.GameTicking.Rules.Configurations;

public sealed class NinjaRuleConfiguration : StationEventRuleConfiguration
{
    /// <summary>
    /// List of objective prototype ids to add
    /// </summary>
    [DataField("objectives", customTypeSerializer: typeof(PrototypeIdListSerializer<ObjectivePrototype>))]
    public readonly List<string> Objectives = default!;

    // TODO: move to job and use job???
    /// <summary>
    /// List of implants to inject on spawn
    /// </summary>
    [DataField("implants", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public readonly List<string> Implants = default!;

    /// <summary>
    /// List of threats that can be called in
    /// </summary>
    [DataField("threats")]
    public readonly List<Threat> Threats = default!;

    [DataField("greetingSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? GreetingSound = new SoundPathSpecifier("/Audio/Misc/ninja_greeting.ogg");
}

[DataDefinition]
public sealed class Threat
{
    [DataField("announcement")]
    public readonly string Announcement = default!;

    [DataField("rule")]
    public readonly string Rule = default!;
}
