using Content.Shared.Buckle.Components;
using Robust.Shared.Network;

namespace Content.Shared._Starlight.Railroading;

public sealed partial class ChildEntitiesSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChildEntitiesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChildEntitiesComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<ChildEntitiesComponent> ent, ref MapInitEvent args)
    {
        foreach (var child in ent.Comp.ChildPrototypes)
        {
            var coords = Transform(ent).Coordinates;

            coords = coords.WithPosition(coords.Position + child.Offset);

            if (_net.IsServer)
            {
                var childEnt = SpawnAttachedTo(child.Prototype, coords);
                ent.Comp.Children.Add(childEnt);
            }
            else
                PredictedSpawnAtPosition(child.Prototype, coords);
        }
    }

    private void OnShutdown(Entity<ChildEntitiesComponent> ent, ref ComponentShutdown args)
    {
        foreach (var child in ent.Comp.Children)
        {
            if (TerminatingOrDeleted(child))
                continue;

            if (_net.IsServer)
                QueueDel(child);
            else
                PredictedQueueDel(child);
        }
    }
}
