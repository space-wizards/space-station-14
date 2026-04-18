using Robust.Shared.Prototypes;

namespace Content.Shared.RPGoals;

[Prototype("rpGoalDefinition")]
public sealed partial class RPGoalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string LocaleKey { get; private set; } = default!;

    [DataField]
    public string Category { get; private set; } = "General";

    [DataField]
    public float Weight { get; private set; } = 1.0f;

    [DataField]
    public HashSet<string> AllowedRoles { get; private set; } = new();

    [DataField]
    public HashSet<string> BlockedRoles { get; private set; } = new();

    [DataField]
    public HashSet<string> UnsafeTags { get; private set; } = new();

    [DataField]
    public HashSet<string> ForbiddenTags { get; private set; } = new();

    [DataField]
    public RPGoalRequirements Requirements { get; private set; } = new();
}

[DataDefinition]
public sealed partial class RPGoalRequirements
{
    [DataField]
    public int? MinRoundMinutes { get; private set; }

    [DataField]
    public string? Department { get; private set; }

    [DataField]
    public HashSet<string> RequiredJobTags { get; private set; } = new();

    [DataField]
    public HashSet<string> ExcludedJobTags { get; private set; } = new();
}
