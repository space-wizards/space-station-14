using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class NotAntagonistEntityConditionSystem : EntityConditionSystem<MindComponent, NotAntagonistCondition>
{
    [Dependency] private SharedRoleSystem _roleSystem = default!;

    protected override void Condition(Entity<MindComponent> entity, ref EntityConditionEvent<NotAntagonistCondition> args)
    {
        var excludedProtos = args.Condition.Excluded;

        var antagRoles = _roleSystem.MindGetAllRoleInfo(entity.AsNullable()).Where(role => role.Antagonist);

        var excluded = antagRoles.Any(role => excludedProtos.Contains(role.Prototype));

        args.Result = !_roleSystem.MindIsAntagonist(entity) || excluded;
    }
}

/// <summary>
/// Checks if the given mind is not an antagonist.
/// Allows to exclude specific antags from the check.
/// </summary>
public sealed partial class NotAntagonistCondition : EntityConditionBase<NotAntagonistCondition>
{
    /// <summary>
    /// The antagonists to exclude from the check.
    /// For example, if "Traitor" is provided, this condition will pass for every single non-antagonist + traitors.
    /// </summary>
    [DataField]
    public List<ProtoId<AntagPrototype>> Excluded = new();

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}

