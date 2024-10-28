using Content.Shared.Explosion.Components;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks for the presence of <see cref="ActiveTimerTriggerComponent"/>
/// </summary>
public sealed partial class ActiveTimerPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    /// <summary>
    /// When true, passes this precondition when the entity has an active timer.
    /// Otherwise, passes this precondition when the entity does not have an active timer.
    /// </summary>
    [DataField]
    public bool Active = true;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return Active == _entManager.HasComponent<ActiveTimerTriggerComponent>(owner);
    }
}
