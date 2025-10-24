using System.Linq;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.Timing;

/// <summary>
/// For events that cause the UseDelay to trigger.
/// </summary>
public partial class UseDelaySystem
{
    public void InitializeEvents()
    {
        SubscribeLocalEvent<UseDelayOnShootComponent, GunShotEvent>(OnUseShoot);
        SubscribeLocalEvent<UseDelayOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<UseDelayOnThrowHitComponent, ThrowDoHitEvent>(OnThrowHitEvent);
    }

    private void OnUseShoot(Entity<UseDelayOnShootComponent> ent, ref GunShotEvent args)
    {
        TryResetDelay(ent, checkDelayed: true, id: ent.Comp.UseDelayId);
    }

    private void OnMeleeHit(Entity<UseDelayOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Any() || ent.Comp.IncludeMiss)
            TryResetDelay(ent, checkDelayed: true, id: ent.Comp.UseDelayId);
    }

    private void OnThrowHitEvent(Entity<UseDelayOnThrowHitComponent> ent, ref ThrowDoHitEvent args)
    {
        TryResetDelay(ent, checkDelayed: true, id: ent.Comp.UseDelayId);
    }
}
