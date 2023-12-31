using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Content.Shared.Preferences;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="ThiefRuleSystem/">.
/// </summary>
[RegisterComponent, Access(typeof(ThiefRuleSystem))]
public sealed partial class ThiefRuleComponent : Component
{
    /// <summary>
    /// Add a Pacified comp to thieves
    /// </summary>
    [DataField]
    public bool PacifistThieves = true;

    /// <summary>
    /// A chance for this mode to be added to the game.
    /// </summary>
    [DataField]
    public float RuleChance = 1f;

    [DataField]
    public ProtoId<AntagPrototype> ThiefPrototypeId = "Thief";

    public Dictionary<ICommonSession, HumanoidCharacterProfile> StartCandidates = new();

    [DataField]
    public float MaxObjectiveDifficulty = 2.5f;

    [DataField]
    public int MaxStealObjectives = 10;

    /// <summary>
    /// Things that will be given to thieves
    /// </summary>
    [DataField]
    public List<EntProtoId> StarterItems = new List<EntProtoId> { "ToolboxThief", "ClothingHandsChameleonThief" }; //TO DO - replace to chameleon thieving gloves whem merg

    /// <summary>
    /// All Thieves created by this rule
    /// </summary>
    [DataField]
    public List<EntityUid> ThievesMinds = new();

    /// <summary>
    /// Max Thiefs created by rule on roundstart
    /// </summary>
    [DataField]
    public int MaxAllowThief = 3;

    /// <summary>
    /// Sound played when making the player a thief via antag control or ghost role
    /// </summary>
    [DataField]
    public SoundSpecifier? GreetingSound = new SoundPathSpecifier("/Audio/Misc/thief_greeting.ogg");
}
