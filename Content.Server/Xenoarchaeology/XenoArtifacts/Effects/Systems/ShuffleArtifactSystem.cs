using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class ShuffleArtifactSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ShuffleArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, ShuffleArtifactComponent component, ArtifactActivatedEvent args)
    {
        var mobState = GetEntityQuery<MobStateComponent>();

        List<EntityCoordinates> allCoords = new();
        List<TransformComponent> toShuffle = new();

        foreach (var ent in _lookup.GetEntitiesInRange(uid, component.Radius, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (!mobState.HasComponent(ent))
                continue;

            var xform = Transform(ent);

            toShuffle.Add(xform);
            allCoords.Add(xform.Coordinates);
        }

        foreach (var xform in toShuffle)
        {
            _xform.SetCoordinates(xform, _random.PickAndTake(allCoords));
        }
    }
}
