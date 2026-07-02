using Content.Shared.NPC.Systems;
using Robust.Shared.Containers;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the owner is in a friendly container.
/// Recursively checks if container's container is friendly.
/// </summary>
public sealed partial class InFriendlyContainerPrecondition : HTNPrecondition
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;

    [DataField] public bool IsInFriendlyContainer = true;

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
