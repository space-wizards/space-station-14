using Content.Shared.Emag.Systems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// A precondition which is met if the NPC is emagged with <see cref="EmagType"/>, as computed by
/// <see cref="EmagSystem.CheckFlag"/>. This is useful for changing NPC behavior in the case that the NPC is emagged,
/// eg. like a helper NPC bot turning evil.
/// </summary>
public sealed partial class IsEmaggedPrecondition : HTNPrecondition
{
    [Dependency] private EmagSystem _emag = default!;

    /// <summary>
    /// The type of emagging to check for.
    /// </summary>
    [DataField]
    public EmagType EmagType = EmagType.Interaction;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return _emag.CheckFlag(owner, EmagType);
    }
}
