using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Gets ammo for this NPC's selected gun; either active hand or itself.
/// </summary>
public sealed partial class GunAmmoPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private SharedGunSystem _gunSystem = default!;

    [DataField]
    public float MinPercent = 0f;

    [DataField]
    public float MaxPercent = 1f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_gunSystem.TryGetGun(owner, out var gun))
        {
            return false;
        }

        var ammoEv = new GetAmmoCountEvent();
        _entManager.EventBus.RaiseLocalEvent(gun, ref ammoEv);
        float percent;

        if (ammoEv.Capacity == 0)
            percent = 0f;
        else
            percent = ammoEv.Count / (float)ammoEv.Capacity;

        percent = System.Math.Clamp(percent, 0f, 1f);

        if (MaxPercent < percent)
            return false;

        if (MinPercent > percent)
            return false;

        return true;
    }
}
