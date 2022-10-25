using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class SpawnArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnArtifactComponent, ArtifactNodeEnteredEvent>(OnNodeEntered);
        SubscribeLocalEvent<SpawnArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }
    private void OnNodeEntered(EntityUid uid, SpawnArtifactComponent component, ArtifactNodeEnteredEvent args)
    {
        if (!component.RandomPrototype)
            return;
        if (component.PossiblePrototypes.Count == 0)
            return;

        var proto = component.PossiblePrototypes[args.RandomSeed % component.PossiblePrototypes.Count];
        component.Prototype = proto;
    }

    private void OnActivate(EntityUid uid, SpawnArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (component.Prototype == null)
            return;
        if (component.SpawnsCount >= component.MaxSpawns)
            return;

        // select spawn position near artifact
        var artifactCord = Transform(uid).Coordinates;
        var dx = _random.NextFloat(-component.Range, component.Range);
        var dy = _random.NextFloat(-component.Range, component.Range);
        var spawnCord = artifactCord.Offset(new Vector2(dx, dy));

        // spawn entity
        var spawned = EntityManager.SpawnEntity(component.Prototype, spawnCord);
        component.SpawnsCount++;

        // if there is an user - try to put spawned item in their hands
        // doesn't work for spawners
        _handsSystem.PickupOrDrop(args.Activator, spawned);
    }
}
