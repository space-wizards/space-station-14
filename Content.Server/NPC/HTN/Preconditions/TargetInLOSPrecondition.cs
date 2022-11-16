using Content.Server.Interaction;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed class TargetInLOSPrecondition : HTNPrecondition
{
    private InteractionSystem _interaction = default!;

    [DataField("targetKey")]
    public string TargetKey = "CombatTarget";

    [DataField("rangeKey")]
    public string RangeKey = "RangeKey";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _interaction = sysManager.GetEntitySystem<InteractionSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target))
            return false;

        var range = blackboard.GetValueOrDefault<float>(RangeKey);

        return _interaction.InRangeUnobstructed(owner, target, range);
    }
}
