using Content.Shared.NPC.Systems;
using Robust.Shared.Containers;
using Robust.Server.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Recursively checks if the owner is in a friendly container.
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
            return false; // If it doesn't have an xform there's bigger problems

        if (!_container.TryGetContainingContainer((owner, xform), out var container))
            return !IsInFactionContainer;

        return IsInFactionContainer == IsContainerFriendlyRecursive((owner, xform), container, owner);
    }

    /// <summary>
    /// Returns true if a container is friendly with the owner. Recursively checks each parent.
    /// </summary>
    private bool IsContainerFriendlyRecursive(Entity<TransformComponent?> ent, BaseContainer container, EntityUid owner)
    {
        if (_npcFaction.IsEntityFriendly(owner, container.Owner))
            return true;

        if (!_container.TryGetContainingContainer(ent, out var nextContainer))
            return false;

        return IsContainerFriendlyRecursive(container.Owner, nextContainer, owner);
    }
}
