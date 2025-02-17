// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Mobs.Systems;
using Content.Shared.Storage;
using System.Linq;
using Content.Shared.Mobs;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Necromorphs.Pregnant;

public sealed class PregnantSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PregnantComponent, MobStateChangedEvent>(OnState);
    }

    private void OnState(EntityUid uid, PregnantComponent component, MobStateChangedEvent args)
    {
        if (!_mobState.IsDead(uid))
            return;

        _audio.PlayPvs("/Audio/Effects/Fluids/splat.ogg", uid, AudioParams.Default.WithVariation(0.2f).WithVolume(1f));

        var spawns = EntitySpawnCollection.GetSpawns(component.SpawnedEntities).Cast<string?>().ToList();
        EntityManager.SpawnEntities(_transform.GetMapCoordinates(uid), spawns);
    }
}
