using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class SpawnArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    public const string NodeDataSpawnAmount = "nodeDataSpawnAmount";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnArtifactComponent, ArtifactNodeEnteredEvent>(OnNodeEntered);
        SubscribeLocalEvent<SpawnArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }
    private void OnNodeEntered(EntityUid uid, SpawnArtifactComponent component, ArtifactNodeEnteredEvent args)
    {
        if (component.PossiblePrototypes.Count == 0)
            return;

        var proto = component.PossiblePrototypes[args.RandomSeed % component.PossiblePrototypes.Count];
        component.Prototype = proto;
    }

    private void OnActivate(EntityUid uid, SpawnArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (component.Prototype == null)
            return;

        if (!_artifact.TryGetNodeData(uid, NodeDataSpawnAmount, out int amount))
            amount = 0;

        if (amount >= component.MaxSpawns)
            return;

        var toSpawn = component.Prototype;
        if (!component.ConsistentSpawn)
            toSpawn = _random.Pick(component.PossiblePrototypes);

        // select spawn position near artifact
        var artifactCord = Transform(uid).MapPosition;
        var dx = _random.NextFloat(-component.Range, component.Range);
        var dy = _random.NextFloat(-component.Range, component.Range);
        var spawnCord = artifactCord.Offset(new Vector2(dx, dy));

        // spawn entity
        var spawned = EntityManager.SpawnEntity(toSpawn, spawnCord);
        _artifact.SetNodeData(uid, NodeDataSpawnAmount, amount+1);

        // if there is an user - try to put spawned item in their hands
        // doesn't work for spawners
        _handsSystem.PickupOrDrop(args.Activator, spawned);
    }
}
