using System.Numerics;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Shared.Containers;

public sealed class ContainerFillSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContainerFillComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, ContainerFillComponent component, MapInitEvent args)
    {
        if (!TryComp(uid, out ContainerManagerComponent? containerComp))
            return;

        var xform = Transform(uid);
        var coords = new EntityCoordinates(uid, Vector2.Zero);

        foreach (var (containerId, prototypes) in component.Containers)
        {
            if (!_containerSystem.TryGetContainer(uid, containerId, out var container, containerComp))
            {
                Log.Error($"Entity {ToPrettyString(uid)} with a {nameof(ContainerFillComponent)} is missing a container ({containerId}).");
                continue;
            }

            foreach (var proto in prototypes)
            {
                var ent = Spawn(proto, coords);
                if (!_containerSystem.Insert(ent, container, containerXform: xform))
                {
                    Log.Error($"Entity {ToPrettyString(uid)} with a {nameof(ContainerFillComponent)} failed to insert an entity: {ToPrettyString(ent)}.");
                    Transform(ent).AttachToGridOrMap();
                    break;
                }
            }
        }
    }
}
