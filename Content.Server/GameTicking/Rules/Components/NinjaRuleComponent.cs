using Content.Server.Ninja.Systems;
using Content.Server.Objectives;
using Content.Shared.Communications;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(SpaceNinjaSystem))]
public sealed partial class NinjaRuleComponent : Component
{
    /// <summary>
    /// List of objective prototype ids to add
    /// </summary>
    [DataField("objectives", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<ObjectivePrototype>))]
    public List<string> Objectives = new();

    /// <summary>
    /// List of implants to inject on spawn.
    /// </summary>
    [DataField("implants", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> Implants = new();

    /// <summary>
    /// List of threats that can be called in. Copied onto <see cref="CommsHackerComponent"/> when gloves are enabled.
    /// </summary>
    [DataField("threats", required: true)]
    public List<Threat> Threats = new();

    /// <summary>
    /// Sound played when making the player a ninja via antag control or ghost role
    /// </summary>
    [DataField("greetingSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? GreetingSound = new SoundPathSpecifier("/Audio/Misc/ninja_greeting.ogg");
}
