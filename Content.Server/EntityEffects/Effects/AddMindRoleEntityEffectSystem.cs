using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class AddMindRoleEntityEffectSystem : EntityEffectSystem<MetaDataComponent, AddMindRole>
{
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _role = default!;

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

public sealed partial class AddMindRole : EntityEffectBase<AddMindRole>
{
    [DataField(required: true)]
    public EntProtoId Role;
}
