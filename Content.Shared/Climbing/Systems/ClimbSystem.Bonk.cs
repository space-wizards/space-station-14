using Content.Shared.Climbing.Components;

namespace Content.Shared.Climbing.Systems;

// This partial handles BonkableComponent.
public sealed partial class ClimbSystem
{
    /// <summary>
    /// A foolish creature has bonked their head upon this bonkable.
    /// </summary>
    public void Bonk(Entity<BonkableComponent?> table, EntityUid victim)
    {
        if (!_bonkQuery.Resolve(table, ref table.Comp, false))
            return;

        if (table.Comp.BonkDamage != null)
            _damageableSystem.ChangeDamage(victim, table.Comp.BonkDamage, true);

        _stunSystem.TryUpdateParalyzeDuration(victim, table.Comp.BonkTime);
        _audio.PlayPvs(table.Comp.BonkSound, table);
    }
}
