using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Adds a role to this entity's mind unless that role prototype is already present.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class AddMindRoleEntityEffectSystem : EntityEffectSystem<MetaDataComponent, AddMindRole>
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<AddMindRole> args)
    {
        if (!_mind.TryGetMind(entity, out var mindId, out var mind))
            return;

        var rolePrototype = args.Effect.Role;
        foreach (var role in mind.MindRoleContainer.ContainedEntities)
        {
            if (rolePrototype.Equals(MetaData(role).EntityPrototype?.ID))
                return;
        }

        _role.MindAddRole(mindId, rolePrototype, mind);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddMindRole : EntityEffectBase<AddMindRole>
{
    [DataField(required: true)]
    public EntProtoId Role;
}
