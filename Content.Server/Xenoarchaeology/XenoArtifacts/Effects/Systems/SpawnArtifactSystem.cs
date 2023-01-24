using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class SpawnArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    public const string NodeDataSpawnAmount = "nodeDataSpawnAmount";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, SpawnArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (!_artifact.TryGetNodeData(uid, NodeDataSpawnAmount, out int amount))
            amount = 0;

        if (amount >= component.MaxSpawns)
            return;

        if (component.Spawns is not {} spawns)
            return;

        var artifactCord = Transform(uid).MapPosition;
        foreach (var spawn in EntitySpawnCollection.GetSpawns(spawns, _random))
        {
            var dx = _random.NextFloat(-component.Range, component.Range);
            var dy = _random.NextFloat(-component.Range, component.Range);
            var spawnCord = artifactCord.Offset(new Vector2(dx, dy));
            EntityManager.SpawnEntity(spawn, spawnCord);
        }
        _artifact.SetNodeData(uid, NodeDataSpawnAmount, amount + 1);
    }
}
