using Content.Shared.Throwing;
using Content.Shared.Timing.Components;
using Content.Shared.Timing.Systems;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Weapons.Melee;

/// <inheritdoc cref="UseDelayOnMeleeHitComponent"/>
public sealed partial class UseDelayOnMeleeHitSystem : EntitySystem
{
    [Dependency] private UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UseDelayOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<UseDelayOnMeleeHitComponent, ThrowDoHitEvent>(OnThrowHitEvent);
    }

    private void OnThrowHitEvent(Entity<UseDelayOnMeleeHitComponent> ent, ref ThrowDoHitEvent args)
    {
        TryResetDelay(ent);
    }

    private void OnMeleeHit(Entity<UseDelayOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        TryResetDelay(ent);
    }

    private void TryResetDelay(Entity<UseDelayOnMeleeHitComponent> ent)
    {
        var uid = ent.Owner;

        _delay.TryResetDelay(uid, checkDelayed: true);
    }
}
