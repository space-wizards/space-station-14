using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Robust.Server.Containers;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class ObjectInContainerPrecondition : HTNPrecondition
{
    private ContainerSystem _containers = default!;
    [DataField(required: true)] public string Container;
    [DataField] public bool Exists = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _containers = sysManager.GetEntitySystem<ContainerSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var container = _containers.GetContainer(owner, Container);
        if (container.ContainedEntities.Count > 0)
            return Exists;

        return !Exists;
    }
}
