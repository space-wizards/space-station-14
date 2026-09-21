using System.Linq;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

/// <summary>
/// Checks if the given mind is an antagonist with specified tag.
/// </summary>
public sealed partial class AntagonistTagCondition : EntityConditionBase<IAntagonistTagCondition>, IAntagonistTagCondition
{
    /// <summary>
    /// The tags this check will succeed for.
    /// For example, if "OnStation" is provided, all on-station antags will pass the check.
    /// If <see cref="AllowNonAntags"/> is true, it will additionally allow every non-antag to pass the check.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<AntagTagPrototype>> Tags { get; set; }= new();

    /// <summary>
    /// Whether non-antagonists should always pass this condition.
    /// </summary>
    [DataField]
    public bool AllowNonAntags { get; set; } = true;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}
