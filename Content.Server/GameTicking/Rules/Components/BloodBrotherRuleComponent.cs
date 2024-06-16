using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(BloodBrotherRuleSystem))]
public sealed partial class BloodBrotherRuleComponent : Component
{
    public readonly List<EntityUid> Minds = new();

    [DataField("prototypeId")]
    public ProtoId<AntagPrototype> PrototypeId = "BloodBrother";

    /// <summary>
    /// The total number of active blood brothers.
    /// </summary>
    public int NumberOfAntags => Minds.Count;

    /// <summary>
    /// Path to the traitor greeting sound.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetingSound = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");
}
