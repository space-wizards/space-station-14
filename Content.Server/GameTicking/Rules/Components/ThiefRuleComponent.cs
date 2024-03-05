using Content.Shared.Random;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="ThiefRuleSystem/">.
/// </summary>
[RegisterComponent, Access(typeof(ThiefRuleSystem))]
public sealed partial class ThiefRuleComponent : Component
{
    [DataField]
    public ProtoId<WeightedRandomPrototype> BigObjectiveGroup = "ThiefBigObjectiveGroups";

    [DataField]
    public ProtoId<WeightedRandomPrototype> SmallObjectiveGroup = "ThiefObjectiveGroups";

    [DataField]
    public ProtoId<WeightedRandomPrototype> EscapeObjectiveGroup = "ThiefEscapeObjectiveGroups";

    [DataField]
    public float BigObjectiveChance = 0.7f;

    /// <summary>
    /// Add a Pacified comp to thieves
    /// </summary>
    [DataField]
    public bool PacifistThieves = true;

    [DataField]
    public ProtoId<AntagPrototype> ThiefPrototypeId = "Thief";

    [DataField]
    public float MaxObjectiveDifficulty = 2.5f;

    [DataField]
    public int MaxStealObjectives = 10;

    /// <summary>
    /// Things that will be given to thieves
    /// </summary>
    [DataField]
    public List<EntProtoId> StarterItems = new() { "ToolboxThief", "ClothingHandsChameleonThief" };

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
