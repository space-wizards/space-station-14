using Content.Server.Clothing.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Hands.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class SpawnArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnArtifactComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SpawnArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }
    private void OnMapInit(EntityUid uid, SpawnArtifactComponent component, MapInitEvent args)
    {
        ChooseRandomPrototype(uid, component);
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
        if (args.Activator != null &&
            EntityManager.TryGetComponent(args.Activator.Value, out SharedHandsComponent? hands) &&
            EntityManager.HasComponent<ItemComponent>(spawned))
        {
            hands.TryPutInAnyHand(spawned);
        }
    }

    private void ChooseRandomPrototype(EntityUid uid, SpawnArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.RandomPrototype)
            return;
        if (component.PossiblePrototypes.Count == 0)
            return;

        var proto = _random.Pick(component.PossiblePrototypes);
        component.Prototype = proto;
    }
}
