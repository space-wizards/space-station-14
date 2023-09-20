using Content.Server.Ninja.Systems;
using Content.Shared.Communications;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(SpaceNinjaSystem))]
public sealed partial class NinjaRuleComponent : Component
{
    /// <summary>
    /// All ninja minds that are using this rule.
    /// Their SpaceNinjaComponent Rule field should point back to this rule.
    /// </summary>
    [DataField("minds")]
    public List<EntityUid> Minds = new();

    /// <summary>
    /// List of objective entity prototypes to add
    /// </summary>
    [DataField("objectives", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> Objectives = new();

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
