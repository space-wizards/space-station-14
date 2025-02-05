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
    [Dependency] private readonly IEntityManager _entManager = default!;
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

        // if (!_entManager.TryGetComponent<TransformComponent>(owner, out var xform))
        //     return false; // If it doesn't have an xform there's bigger problems

        if (!_container.TryGetContainingContainer(owner, out var container))
            return !IsInFriendlyContainer;

        return IsInFriendlyContainer == IsContainerFriendlyRecursive(owner, container, owner);
    }

    /// <summary>
    /// Recursively check a container's container f.
    /// </summary>
    /// <param name="ent">Entity that might be inside a container.</param>
    /// <param name="owner">The NPC.</param>
    /// <param name="container">Container containing <paramref name="ent"/>.</param>
    /// <returns>True if container.Owner is friendly with <paramref name="owner"/>.</returns>
    private bool IsContainerFriendlyRecursive(EntityUid ent, BaseContainer container, EntityUid owner)
    {
        if (_npcFaction.IsEntityFriendly(owner, container.Owner))
            return true;

        if (!_container.TryGetContainingContainer(ent, out var nextContainer))
            return false;

        return IsContainerFriendlyRecursive(container.Owner, nextContainer, owner);
    }
}
