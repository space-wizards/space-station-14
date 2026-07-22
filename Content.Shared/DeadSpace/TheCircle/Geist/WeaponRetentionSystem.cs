// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Shared.DeadSpace.TheCircle.Geist;

public sealed class WeaponRetentionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeaponRetentionComponent, KnockDownAttemptEvent>(OnKnockdownAttempt);
        SubscribeLocalEvent<WeaponRetentionComponent, DropHandItemsEvent>(OnDropHandItems);
        SubscribeLocalEvent<WeaponRetentionComponent, DisarmAttemptEvent>(OnDisarmAttempt);
    }

    private void OnKnockdownAttempt(Entity<WeaponRetentionComponent> ent, ref KnockDownAttemptEvent args)
    {
        args.Drop = false;
    }

    private void OnDropHandItems(Entity<WeaponRetentionComponent> ent, ref DropHandItemsEvent args)
    {
        args.Cancelled = true;
    }

    private void OnDisarmAttempt(Entity<WeaponRetentionComponent> ent, ref DisarmAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
