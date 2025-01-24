using Content.Shared.NPC.Systems;
using Robust.Shared.Containers;
using Robust.Server.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the owner is in a container.
/// Checks if the outer-most container is part of their faction.
/// </summary>
public sealed partial class InFactionContainerPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private ContainerSystem _container = default!;
    private NpcFactionSystem _npcFaction = default!;

    [DataField] public bool IsInFactionContainer = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _container = sysManager.GetEntitySystem<ContainerSystem>();
        _npcFaction = sysManager.GetEntitySystem<NpcFactionSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<TransformComponent>(owner, out var xform))
            return !IsInFactionContainer; // If it doesn't have an xform it's probably not in a container

        if (!_container.TryGetOuterContainer(owner, xform, out var container))
            return !IsInFactionContainer;

        return IsInFactionContainer == _npcFaction.IsEntityFriendly(owner, container.Owner);
    }
}
