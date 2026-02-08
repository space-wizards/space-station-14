using Content.Server.Interaction;
using Content.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class TargetInLOSPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private InteractionSystem _interaction = default!;

    /// <summary>
    /// Key for retrieving the current target from the NPCBlackboard.
    /// </summary>
    [DataField]
    public string TargetKey = "Target";

    /// <summary>
    /// Key for retrieving the max distance checked from the NPCBlackboard.
    /// </summary>
    [DataField]
    public string RangeKey = "RangeKey";

    /// <summary>
    /// Collision group(s) that block line of sight to the target.
    /// </summary>
    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionLayer>))]
    public int BlockingGroup = (int)(CollisionGroup.Impassable | CollisionGroup.InteractImpassable);

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _interaction = sysManager.GetEntitySystem<InteractionSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
            return false;

        var range = blackboard.GetValueOrDefault<float>(RangeKey, _entManager);
        return _interaction.InRangeUnobstructed(owner, target, range, (CollisionGroup)BlockingGroup);
    }
}
