using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class AntagonistTagEntityConditionSystem : EntityConditionSystem<MindComponent, AntagonistTagCondition>
{
    [Dependency] private SharedRoleSystem _roleSystem = default!;

    protected override void Condition(Entity<MindComponent> entity, ref EntityConditionEvent<AntagonistTagCondition> args)
    {
        var conditionTags = args.Condition.Tags;

        if (!_roleSystem.TryGetAllAntagTags(entity.AsNullable(), out var antagTags))
        {
            args.Result = args.Condition is { AllowNonAntags: true, Inverted: false };
            return;
        }

        args.Result = antagTags.Overlaps(conditionTags);
    }
}

/// <summary>
/// Checks if the given mind is an antagonist with specified tag.
/// </summary>
public sealed partial class AntagonistTagCondition : EntityConditionBase<AntagonistTagCondition>
{
    /// <summary>
    /// The tags this check will succeed for.
    /// For example, if "OnStation" is provided, all on-station antags will pass the check.
    /// If <see cref="AllowNonAntags"/> is true, it will additionally allow every non-antag to pass the check.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<AntagTagPrototype>> Tags = new();

    /// <summary>
    /// Whether non-antagonists should always pass this condition.
    /// </summary>
    [DataField]
    public bool AllowNonAntags = true;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}

