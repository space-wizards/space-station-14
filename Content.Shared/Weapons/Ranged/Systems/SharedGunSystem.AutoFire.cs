using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //Automatic firing without stopping if the AutoShootGunComponent component is exist and enabled
        var query = EntityQueryEnumerator<AutoShootGunComponent, GunComponent>();
        while (query.MoveNext(out var uid, out var autoShoot, out var gun))
        {
            if (!autoShoot.Enabled)
                continue;

            if (gun.NextFire > _timing.CurTime)
                continue;

            AttemptShoot(null, uid, gun, null);
        }
    }

    public void SetEnabled(EntityUid uid, AutoShootGunComponent component, bool status)
    {
        component.Enabled = status;
    }
}
