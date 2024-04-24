using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="ThiefRuleSystem"/>.
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

    [DataField]
    public float MaxObjectiveDifficulty = 2.5f;

    [DataField]
    public int MaxStealObjectives = 10;
}
