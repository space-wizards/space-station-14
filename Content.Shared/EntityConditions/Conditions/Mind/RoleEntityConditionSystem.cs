using System.Linq;
using Content.Shared.Localizations;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

/// <summary>
/// Returns true if this entity has any of the specified jobs. False if the entity has no mind, none of the specified jobs, or is jobless.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class MindContainerRoleEntityConditionSystem : EntityConditionSystem<MindContainerComponent, RoleCondition>
{
    [Dependency] private SharedRoleSystem _role = default!;

    protected override void Condition(Entity<MindContainerComponent> entity, ref EntityConditionEvent<RoleCondition> args)
    {
        if (!TryComp<MindComponent>(entity.Comp.Mind, out var mind))
            return;

        args.Result = _role.MindHasRole((entity.Comp.Mind.Value, mind), args.Condition.Whitelist);
    }
}

/// <summary>
/// Returns true if this mind has any of the specified jobs. False if the mind has none of the specified jobs, or is jobless.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class MindRoleEntityConditionSystem : EntityConditionSystem<MindComponent, RoleCondition>
{
    [Dependency] private SharedRoleSystem _role = default!;

    protected override void Condition(Entity<MindComponent> entity, ref EntityConditionEvent<RoleCondition> args)
    {
        args.Result = _role.MindHasRole(entity, args.Condition.Whitelist);
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class RoleCondition : EntityConditionBase<RoleCondition>
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new ();

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}
