using Content.Shared.DeadSpace.BSAA;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Server.DeadSpace.BSAA;

public sealed class BSAASpecialistSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _factions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BSAASpecialistComponent, ComponentInit>(OnSpecialistInit);
        SubscribeLocalEvent<NecromorfComponent, ComponentInit>(OnNecromorphInit);
    }

    private void OnSpecialistInit(Entity<BSAASpecialistComponent> ent, ref ComponentInit args)
    {
        var query = EntityQueryEnumerator<NecromorfComponent>();
        while (query.MoveNext(out var necromorph, out _))
            _factions.IgnoreEntity(necromorph, ent.Owner);
    }

    private void OnNecromorphInit(Entity<NecromorfComponent> ent, ref ComponentInit args)
    {
        var query = EntityQueryEnumerator<BSAASpecialistComponent>();
        while (query.MoveNext(out var specialist, out _))
            _factions.IgnoreEntity(ent.Owner, specialist);
    }
}
