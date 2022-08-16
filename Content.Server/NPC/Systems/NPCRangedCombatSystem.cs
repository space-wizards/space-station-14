using Content.Server.NPC.Components;
using Content.Server.Weapon.Ranged.Systems;

namespace Content.Server.NPC.Systems;

public sealed class NPCRangedCombatSystem : EntitySystem
{
    [Dependency] private readonly GunSystem _gun = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var comp in EntityQuery<NPCRangedCombatComponent>())
        {
            if (!xformQuery.TryGetComponent(comp.Target, out var targetXform))
            {
                continue;
            }

            var gun = _gun.GetGun(comp.Owner);

            if (gun == null)
            {
                comp.Status = CombatStatus.NoWeapon;
                continue;
            }

            // TODO: LOS
            // TODO: Ammo checks
            // TODO: Burst fire
            // TODO: Cycling
            _gun.AttemptShoot(comp.Owner, gun, targetXform.Coordinates);
        }
    }
}
