using System.Linq;
using Content.Shared.Conditions;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class AntagonistTagEntityConditionSystem : EntitySystem
{
    [Dependency] private SharedRoleSystem _roleSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MindComponent> entity, ref ConditionEvaluationEvent args)
    {
        if (args.Handled || args.Condition is not AntagonistTagCondition condition)
            return;
        args.Handled = true;
        var conditionTags = condition.Tags;

        if (!_roleSystem.TryGetAllAntagTags(entity.AsNullable(), out var antagTags))
        {
            args.Value = condition is { AllowNonAntags: true, Inverted: false } ? 1 : 0;
            return;
        }

        args.Value = (float)antagTags.Intersect(conditionTags).Count() / (float)condition.Tags.Count;
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
