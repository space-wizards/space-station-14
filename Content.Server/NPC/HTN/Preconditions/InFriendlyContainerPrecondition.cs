using Content.Shared.NPC.Systems;
using Robust.Shared.Containers;
using Robust.Server.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the owner is in a friendly container.
/// Recursively checks if container's container is friendly.
/// </summary>
public sealed partial class InFriendlyContainerPrecondition : HTNPrecondition
{
    private ContainerSystem _container = default!;
    private NpcFactionSystem _npcFaction = default!;

    [DataField] public bool IsInFriendlyContainer = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _container = sysManager.GetEntitySystem<ContainerSystem>();
        _npcFaction = sysManager.GetEntitySystem<NpcFactionSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_container.TryGetContainingContainer(owner, out var container))
            return !IsInFriendlyContainer;

        return IsInFriendlyContainer == IsContainerOrParentFriendly(owner, container.Owner);
    }

    /// <summary>
    /// Recursively check if a container or any parent container is friendly.
    /// </summary>
    /// <returns>True if any container is friendly.</returns>
    private bool IsContainerOrParentFriendly(EntityUid owner, EntityUid containerOwner)
    {
        if (_npcFaction.IsEntityFriendly(owner, containerOwner))
            return true;

        if (!_container.TryGetContainingContainer(containerOwner, out var nextContainer))
            return false;

        return IsContainerOrParentFriendly(owner, nextContainer.Owner);
    }
}
