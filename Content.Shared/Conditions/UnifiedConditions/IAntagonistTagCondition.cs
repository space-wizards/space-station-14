using System.Linq;
using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

/// <summary>
/// Checks if the given mind is an antagonist with specified tag.
/// </summary>
public interface IAntagonistTagCondition : ICondition, IWithInverted
{
    /// <summary>
    /// The tags this check will succeed for.
    /// For example, if "OnStation" is provided, all on-station antags will pass the check.
    /// If <see cref="AllowNonAntags" /> is true, it will additionally allow every non-antag to pass the check.
    /// </summary>
    HashSet<ProtoId<AntagTagPrototype>> Tags { get; }

    /// <summary>
    /// Whether non-antagonists should always pass this condition.
    /// </summary>

    bool AllowNonAntags { get; }
}

public sealed partial class AntagonistTagEntityConditionSystem : ConditionEvaluatorSystem<IAntagonistTagCondition>
{
    [Dependency] private SharedRoleSystem _roleSystem = default!;

    public override float Evaluate(IAntagonistTagCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        var conditionTags = condition.Tags;

        if (!_roleSystem.TryGetAllAntagTags(entityUid, out var antagTags))
        {
            return condition is { AllowNonAntags: true, Inverted: false } ? 1 : 0;

        }

        return antagTags.Intersect(conditionTags).Count() / (float)condition.Tags.Count;
    }
}
