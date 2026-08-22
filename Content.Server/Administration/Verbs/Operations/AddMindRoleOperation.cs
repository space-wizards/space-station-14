using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Verbs.Operations;

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

/// <summary>
/// Adds a mind role unless the target already has one from the same prototype.
/// </summary>
public sealed partial class AddMindRoleOperation : AdminOperationBase<AddMindRoleOperation>
{
    [DataField(required: true)]
    public EntProtoId Role { get; private set; }
}
