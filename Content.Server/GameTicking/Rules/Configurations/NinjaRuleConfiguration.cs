using Content.Server.Objectives;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.GameTicking.Rules.Configurations;

/// <summary>
/// Configuration for the Space Ninja antag.
/// </summary>
public sealed class NinjaRuleConfiguration : StationEventRuleConfiguration
{
    /// <summary>
    /// List of objective prototype ids to add
    /// </summary>
    [DataField("objectives", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<ObjectivePrototype>))]
    public readonly List<string> Objectives = new();

    // TODO: move to job and use job???
    /// <summary>
    /// List of implants to inject on spawn
    /// </summary>
    [DataField("implants", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public readonly List<string> Implants = new();

    /// <summary>
    /// List of threats that can be called in
    /// </summary>
    [DataField("threats", required: true)]
    public readonly List<Threat> Threats = new();

    /// <summary>
    /// Sound played when making the player a ninja via antag control or ghost role
    /// </summary>
    [DataField("greetingSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? GreetingSound = new SoundPathSpecifier("/Audio/Misc/ninja_greeting.ogg");

    /// <summary>
    /// Distance that the ninja spawns from the station's half AABB radius
    /// </summary>
    [DataField("spawnDistance")]
    public float SpawnDistance = 20f;
}

/// <summary>
/// A threat that can be called in to the station by a ninja hacking a communications console.
/// Generally some kind of mid-round antag, though you could make it call in scrubber backflow if you wanted to.
/// You wouldn't do that, right?
/// </summary>
[DataDefinition]
public sealed class Threat
{
    /// <summary>
    /// Locale id for the announcement to be made from CentCom.
    /// </summary>
    [DataField("announcement")]
    public readonly string Announcement = default!;

    /// <summary>
    /// The game rule for the threat to be added, it should be able to work when added mid-round otherwise this will do nothing.
    /// </summary>
    [DataField("rule", customTypeSerializer: typeof(PrototypeIdSerializer<GameRulePrototype>))]
    public readonly string Rule = default!;
}
