using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(BloodBrotherRuleSystem))]
public sealed partial class BloodBrotherRuleComponent : Component
{
    public readonly Dictionary<string, List<EntityUid>> Teams = new();
    /// <summary>
    ///     The total number of active blood brothers.
    /// </summary>
    public int NumberOfTeams => Teams.Count;

    [DataField("prototypeId")]
    public ProtoId<AntagPrototype> PrototypeId = "BloodBrother";

    /// <summary>
    ///     Path to the traitor greeting sound.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetingSound = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");
}
