namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

/// <summary>
/// Raises an <see cref="HTNRaisedEvent"/> on the <see cref="NPCBlackboard.Owner">owner</see>. The event will contain
/// the specified <see cref="Args"/>, and if not null, the value of <see cref="TargetKey"/>.
/// </summary>
public sealed partial class RaiseEventForOwnerOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    /// <summary>
    /// The conceptual "target" of this event. Note that this is NOT the entity for which the event is raised. If null,
    /// <see cref="HTNRaisedEvent.Target"/> will be null.
    /// </summary>
    [DataField]
    public string? TargetKey;

    /// <summary>
    /// The data contained in the raised event. Since <see cref="HTNRaisedEvent"/> is itself pretty meaningless, this is
    /// included to give some context of what the event is actually supposed to mean.
    /// </summary>
    [DataField(required: true)]
    public EntityEventArgs Args;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        _entMan.EventBus.RaiseLocalEvent(
            blackboard.GetValue<EntityUid>(NPCBlackboard.Owner),
            new HTNRaisedEvent(
                blackboard.GetValue<EntityUid>(NPCBlackboard.Owner),
                TargetKey is { } targetKey ? blackboard.GetValue<EntityUid>(targetKey) : null,
                Args
            )
        );

        return HTNOperatorStatus.Finished;
    }
}

public sealed partial class HTNRaisedEvent(EntityUid owner, EntityUid? target, EntityEventArgs args) : EntityEventArgs
{
    // Owner and target are both included here in case we want to add a "RaiseEventForTargetOperator" in the future
    // while reusing this event.
    public EntityUid Owner = owner;
    public EntityUid? Target = target;
    public EntityEventArgs Args = args;
}
