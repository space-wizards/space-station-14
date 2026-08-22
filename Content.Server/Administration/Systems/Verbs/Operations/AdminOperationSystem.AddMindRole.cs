using Content.Server.Administration.Verbs.Operations;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnAddMindRole(Entity<MetaDataComponent> entity, ref AdminOperationEvent<AddMindRoleOperation> args)
    {
        if (!_mind.TryGetMind(entity, out var mindId, out var mind))
            return;

        foreach (var role in mind.MindRoleContainer.ContainedEntities)
        {
            if (args.Operation.Role.Equals(MetaData(role).EntityPrototype?.ID))
                return;
        }

        _role.MindAddRole(mindId, args.Operation.Role, mind);
    }
}
