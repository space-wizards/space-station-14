using Content.Shared.Buckle;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.ReplaceRandom;

public sealed class ReplaceRandomSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ReplaceRandomComponent, MapInitEvent>(OnMapInit);
    }

    public void OnMapInit(Entity<ReplaceRandomComponent> entity, ref MapInitEvent args)
    {
        if (!_random.Prob(entity.Comp.Chance)) return;

        // mostly stolen code from polymorph system
        _buckle.TryUnbuckle(entity, entity, true);

        var targetTransformComp = Transform(entity);
        var child = Spawn(entity.Comp.Prototype, targetTransformComp.Coordinates);
        var childXform = Transform(child);
        _transform.SetLocalRotation(child, targetTransformComp.LocalRotation, childXform);

        if (_container.TryGetContainingContainer(entity, out var cont))
        {
            _container.Remove(entity.Owner, cont, force: true);
            _container.Insert(child, cont, force: true);
        }

        QueueDel(entity);
    }
}
