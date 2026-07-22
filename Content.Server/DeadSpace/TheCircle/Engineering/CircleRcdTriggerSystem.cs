// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.TheCircle.Engineering;
using Content.Shared.Flash;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Server.DeadSpace.TheCircle.Engineering;

public sealed class CircleRcdTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly NpcFactionSystem _factions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CircleRcdTriggerComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<CircleRcdTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.Triggered || args.OurFixtureId != ent.Comp.FixtureId)
            return;

        if (TryComp<NpcFactionMemberComponent>(args.OtherEntity, out var factions) &&
            _factions.IsMemberOfAny((args.OtherEntity, factions), ent.Comp.IgnoredFactions))
            return;

        ent.Comp.Triggered = true;
        Dirty(ent);

        if (ent.Comp.FlashTarget)
        {
            _flash.Flash(args.OtherEntity,
                null,
                ent.Owner,
                ent.Comp.FlashDuration,
                0.8f,
                stunDuration: ent.Comp.FlashDuration);
        }

        Spawn(ent.Comp.SpawnPrototype, Transform(ent).Coordinates);
        QueueDel(ent);
    }
}
