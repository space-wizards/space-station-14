using Content.Server.Cloning.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
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
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomCloneSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomCloneSpawnerComponent> ent, ref MapInitEvent args)
    {
        QueueDel(ent.Owner);

        if (!_prototypeManager.TryIndex(ent.Comp.Settings, out var settings))
            return;

        var allHumans = new List<EntityUid>();
        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, HumanoidAppearanceComponent>();
        while (query.MoveNext(out var uid, out var mc, out var mobState, out _))
        {
            // the player needs to have a minde
            if (mc.Mind == null)
                continue;

            // the player has to be alive
            if (_mobState.IsAlive(uid, mobState))
                allHumans.Add(uid);
        }
        if (allHumans.Count == 0)
        {
            Spawn(ent.Comp.FallbackPrototype, Transform(ent.Owner).Coordinates);
            return;
        }

        var bodyToClone = _random.Pick(allHumans);
        _cloning.TryCloning(bodyToClone, _transformSystem.GetMapCoordinates(ent.Owner), settings, out _);
    }
}
