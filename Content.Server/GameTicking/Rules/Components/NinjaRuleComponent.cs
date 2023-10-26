using Content.Server.Ninja.Systems;
using Content.Shared.Communications;
using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores some configuration used by the ninja system.
/// Objectives and roundend summary are handled by <see cref="GenericAntagRuleComponent/">.
/// </summary>
[RegisterComponent, Access(typeof(SpaceNinjaSystem))]
public sealed partial class NinjaRuleComponent : Component
{
    /// <summary>
    /// List of threats that can be called in. Copied onto <see cref="CommsHackerComponent"/> when gloves are enabled.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<WeightedRandomPrototype> Threats = string.Empty;

    /// <summary>
    /// Sound played when making the player a ninja via antag control or ghost role
    /// </summary>
    [DataField]
    public SoundSpecifier? GreetingSound = new SoundPathSpecifier("/Audio/Misc/ninja_greeting.ogg");
}
