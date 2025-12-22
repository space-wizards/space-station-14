using Content.Server.Cloning.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Cloning;

/// <summary>
///     This deals with spawning and setting up a clone of a random crew member.
/// </summary>
public sealed class RandomCloneSpawnerSystem : EntitySystem
{
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomCloneSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomCloneSpawnerComponent> ent, ref MapInitEvent args)
    {
        QueueDel(ent.Owner);

        if (!_prototypeManager.TryIndex(ent.Comp.Settings, out var settings))
        {
            Log.Error($"Used invalid cloning settings {ent.Comp.Settings} for RandomCloneSpawner");
            return;
        }

        var allHumans = _mind.GetAliveHumans();

        if (allHumans.Count == 0)
            return;

        var bodyToClone = _random.Pick(allHumans).Comp.OwnedEntity;

        if (bodyToClone != null)
            _cloning.TryCloning(bodyToClone.Value, _transformSystem.GetMapCoordinates(ent.Owner), settings, out _);
    }
}
