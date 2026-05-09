using Robust.Shared.Containers;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the owner in container or not
/// </summary>
public sealed partial class InContainerPrecondition : HTNPrecondition
{
    [Dependency] private SharedContainerSystem _container = default!;

    [ViewVariables(VVAccess.ReadWrite)] [DataField("isInContainer")] public bool IsInContainer = true;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return IsInContainer && _container.IsEntityInContainer(owner) ||
               !IsInContainer && !_container.IsEntityInContainer(owner);
    }
}
