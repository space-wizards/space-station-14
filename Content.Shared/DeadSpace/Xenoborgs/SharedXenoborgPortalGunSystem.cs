// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Xenoborgs.Components;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.DeadSpace.Xenoborgs;

/// <summary>
/// Predicts the portal gun cooldown so holding the trigger cannot create client-only shots.
/// </summary>
public sealed class SharedXenoborgPortalGunSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoborgPortalGunComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    private void OnShotAttempted(Entity<XenoborgPortalGunComponent> ent, ref ShotAttemptedEvent args)
    {
        if (TryComp<UseDelayComponent>(ent, out var delay) && _useDelay.IsDelayed((ent.Owner, delay)))
            args.Cancel();
    }
}
