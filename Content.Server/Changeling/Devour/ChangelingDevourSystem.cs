using Content.Shared.Changeling.Devour;
using Content.Shared.Nutrition.Components;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Changeling.Devour;

public sealed partial class ChangelingDevourSystem : SharedChangelingDevourSystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    protected override void StartSound(Entity<ChangelingDevourComponent> ent, SoundSpecifier? sound)
    {
        if (sound is not null)
            ent.Comp.CurrentDevourSound = _audioSystem.PlayPvs(sound, ent)!.Value.Entity;
    }

    protected override void StopSound(Entity<ChangelingDevourComponent> ent)
    {
        if (ent.Comp.CurrentDevourSound is not null)
            _audioSystem.Stop(ent.Comp.CurrentDevourSound);

        ent.Comp.CurrentDevourSound = null;
    }

    protected override void RipClothing(EntityUid victim, Entity<ButcherableComponent> item)
    {
        var spawnEntities = EntitySpawnCollection.GetSpawns(item.Comp.SpawnedEntities, _robustRandom);
        var coords = _transform.GetMapCoordinates(victim);

        foreach (var proto in spawnEntities)
        {
            Spawn(proto, coords.Offset(_robustRandom.NextVector2(0.25f)));
        }

        QueueDel(item);
    }
}
