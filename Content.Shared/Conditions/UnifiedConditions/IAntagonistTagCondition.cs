using System.Linq;
using Content.Shared.Conditions.Interfaces;
using Content.Shared.EntityConditions.Conditions.Mind;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;
/// <summary>
/// Checks if the given mind is an antagonist with specified tag.
/// </summary>
public interface IAntagonistTagCondition : ICondition<IAntagonistTagCondition>, IWithInverted
{

    /// <summary>
    /// The tags this check will succeed for.
    /// For example, if "OnStation" is provided, all on-station antags will pass the check.
    /// If <see cref="AllowNonAntags"/> is true, it will additionally allow every non-antag to pass the check.
    /// </summary>
    HashSet<ProtoId<AntagTagPrototype>> Tags { get; }

    /// <summary>
    /// Whether non-antagonists should always pass this condition.
    /// </summary>

    bool AllowNonAntags { get; }

}

public sealed partial class AntagonistTagEntityConditionSystem : EntitySystem
{
    [Dependency] private SharedRoleSystem _roleSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MindComponent> entity, ref ConditionEvaluationEvent<IAntagonistTagCondition> args)
    {
        args.Handled = true;
        var conditionTags = args.Condition.Tags;

        if (!_roleSystem.TryGetAllAntagTags(entity.AsNullable(), out var antagTags))
        {
            args.Value = args.Condition is { AllowNonAntags: true, Inverted: false } ? 1 : 0;
            return;
        }

        args.Value = (float)antagTags.Intersect(conditionTags).Count() / (float)args.Condition.Tags.Count;
    }
}
