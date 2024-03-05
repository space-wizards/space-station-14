using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class UseDelayOnShootSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UseDelayOnShootComponent, GunShotEvent>(OnUseShoot);
    }

    private void OnUseShoot(EntityUid uid, UseDelayOnShootComponent component, ref GunShotEvent args)
    {
        if (TryComp(uid, out UseDelayComponent? useDelay))
            _delay.TryResetDelay((uid, useDelay));
    }
}
