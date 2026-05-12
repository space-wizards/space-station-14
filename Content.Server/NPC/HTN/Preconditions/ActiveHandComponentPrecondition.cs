using Content.Server.Hands.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if the active hand entity has the specified components.
/// </summary>
public sealed partial class ActiveHandComponentPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("invert")]
    public bool Invert;

    [DataField("components", required: true)]
    public ComponentRegistry Components = new();

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager) ||
            !blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var hand, _entManager))
        {
            return Invert;
        }

        if (!_entManager.System<HandsSystem>().TryGetHeldItem(owner, hand, out var entity))
            return Invert;

        foreach (var comp in Components)
        {
            var hasComp = _entManager.HasComponent(entity, comp.Value.Component.GetType());

            if (!hasComp ||
                Invert && hasComp)
            {
                return false;
            }
        }

        return true;
    }
}
