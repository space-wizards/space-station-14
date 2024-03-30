using Content.Shared.Buckle;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Random;

namespace Content.Server.ReplaceOnSpawn;

public sealed class ReplaceOnSpawn : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Update(float deltaTime)
    {
        // because OnMapInit is called when entity is spawned it can be put in container later which breaks stuff
        // so have to do it this way instead
        var toReplace = new ValueList<(EntityUid, ReplaceOnSpawnComponent)>();
        var query = EntityQueryEnumerator<ReplaceOnSpawnComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_random.Prob(comp.Chance))
            {
                RemCompDeferred<ReplaceOnSpawnComponent>(uid);
                continue;
            }
            toReplace.Add((uid, comp));
        }
        foreach (var (uid, comp) in toReplace)
        {
            // mostly stolen code from polymorph system
            _buckle.TryUnbuckle(uid, uid, true);

            var targetTransformComp = Transform(uid);
            var child = Spawn(comp.Prototype, targetTransformComp.Coordinates);
            var childXform = Transform(child);
            _transform.SetLocalRotation(child, targetTransformComp.LocalRotation, childXform);

            if (_container.TryGetContainingContainer(uid, out var cont))
            {
                _container.Remove(uid, cont, force: true);
                _container.Insert(child, cont, force: true);
            }
            QueueDel(uid);
        }
    }
}
