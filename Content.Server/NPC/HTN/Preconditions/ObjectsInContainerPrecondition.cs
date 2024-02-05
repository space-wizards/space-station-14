using Content.Server.Transporters.Systems;
using Robust.Server.Containers;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class ObjectsInContainerPrecondition : HTNPrecondition
{
    private ContainerSystem _containers = default!;

    [DataField("container", required:true)]
    public string Container = string.Empty;

    [DataField()]
    public bool NoObjects;

    public override void Initialize(IEntitySystemManager systemManager)
    {
        base.Initialize(systemManager);
        _containers = systemManager.GetEntitySystem<ContainerSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var container = _containers.GetContainer(owner, Container);
        return (container.ContainedEntities.Count > 0) ^ NoObjects;
    }
}
