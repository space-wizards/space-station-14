using Content.Server.Thief.Systems;
using Content.Shared.Communications;
using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="ThiefSystem/">.
/// </summary>
[RegisterComponent, Access(typeof(ThiefSystem))]
public sealed partial class ThiefRuleComponent : Component
{
    /// <summary>
    /// Is set to false if the current game mode is not suitable for role spawning. Prohibits spawning thieves.
    /// </summary>
    public bool AllowSpawningThiefs = true;

    /// <summary>
    /// All Thiefes created by this rule
    /// </summary>
    public readonly List<EntityUid> ThiefMinds = new();

    /// <summary>
    /// Max Thiefs created by rule
    /// </summary>
    public int MaxAllowThief = 2;

    /// <summary>
    /// Sound played when making the player a thief via antag control or ghost role
    /// </summary>
    [DataField]
    public SoundSpecifier? GreetingSound = new SoundPathSpecifier("/Audio/Voice/Cluwne/cluwnelaugh1.ogg"); //TO DO NEW SOUND
}
