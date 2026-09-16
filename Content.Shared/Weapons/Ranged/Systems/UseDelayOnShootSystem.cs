using Content.Shared.Timing.Systems;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class UseDelayOnShootSystem : EntitySystem
{
    [Dependency] private UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UseDelayOnShootComponent, GunShotEvent>(OnUseShoot);
    }

    private void OnUseShoot(Entity<UseDelayOnShootComponent> ent, ref GunShotEvent args)
    {
        _delay.TryResetDelay(ent.Owner);
    }
}
