using Robust.Server.Containers;
using Content.Shared.Containers;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the owner is in a container with the PacifyingContainer component
/// </summary>
public sealed partial class InPacifyingContainerPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private ContainerSystem _container = default!;

    [DataField] public bool IsInPacifyingContainer = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _container = sysManager.GetEntitySystem<ContainerSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
            return false;

        if (!_container.TryGetContainingContainer(owner, out var container))
            return !IsInPacifyingContainer;

        return IsInPacifyingContainer == IsContainerOrParentPacifying(owner, container.Owner);
    }

    /// <summary>
    /// Recursively check if a container or any parent container has the PacifyingContainer component
    /// </summary>
    /// <returns>True if any container has the PacifyingContainer component.</returns>
    private bool IsContainerOrParentPacifying(EntityUid owner, EntityUid containerOwner)
    {
        if (_entManager.HasComponent<PacifyingContainerComponent>(containerOwner))
            return true;

        if (!_container.TryGetContainingContainer(containerOwner, out var nextContainer))
            return false;

        return IsContainerOrParentPacifying(owner, nextContainer.Owner);
    }
}
