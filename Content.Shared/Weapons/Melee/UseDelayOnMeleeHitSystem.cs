using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Weapons.Melee;

/// <inheritdoc cref="UseDelayOnMeleeHitComponent"/>
public sealed class UseDelayOnMeleeHitSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _delay = default!;

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

        if (!TryComp<UseDelayComponent>(uid, out var useDelay))
            return;

        _delay.TryResetDelay((uid, useDelay), checkDelayed: true);
    }
}
