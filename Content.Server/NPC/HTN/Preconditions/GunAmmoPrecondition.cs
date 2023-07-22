using Content.Shared.Hands.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed class GunAmmoPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("minPercent")]
    public float MinPercent = 0f;

    [DataField("maxPercent")]
    public float MaxPercent = 1f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue(NPCBlackboard.ActiveHand, out Hand? hand, _entManager) ||
            !_entManager.TryGetComponent<GunComponent>(hand.HeldEntity, out var gun))
        {
            return false;
        }

        var ammoEv = new GetAmmoCountEvent();
        _entManager.EventBus.RaiseLocalEvent(hand.HeldEntity.Value, ref ammoEv);
        float percent;

        if (ammoEv.Capacity == 0)
            percent = 0f;
        else
            percent = ammoEv.Count / (float) ammoEv.Capacity;

        percent = Math.Clamp(percent, 0f, 1f);

        if (MaxPercent < percent)
            return false;

        if (MinPercent > percent)
            return false;

        return true;
    }
}
