using System.Numerics;
using Content.Shared.Storage;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Random;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAESpawnSystem : BaseXAESystem<XAESpawnComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAESpawnComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var component = ent.Comp;
        if (component.Spawns is not { } spawns)
            return;

        var artifactCord = _transform.GetMapCoordinates(ent.Owner);
        foreach (var spawn in EntitySpawnCollection.GetSpawns(spawns, _random))
        {
            var dx = _random.NextFloat(-component.Range, component.Range);
            var dy = _random.NextFloat(-component.Range, component.Range);
            var spawnCord = artifactCord.Offset(new Vector2(dx, dy));
            var spawnedEnt = Spawn(spawn, spawnCord);
            _transform.AttachToGridOrMap(spawnedEnt);
        }
    }
}
