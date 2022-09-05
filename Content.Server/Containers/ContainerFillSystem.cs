using Content.Server.Construction;
using Content.Server.Construction.Components;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Containers;

public sealed class ContainerFillSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly ConstructionSystem _constructionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainerFillComponent, MapInitEvent>(OnContainerFillMapInit);
    }

    private void OnContainerFillMapInit(EntityUid uid, ContainerFillComponent component, MapInitEvent args)
    {
        if (component.Containers.Count == 0) return;

        if (!TryComp<ContainerManagerComponent>(uid, out var containerManagerComponent))
            return;

        ConstructionComponent? construction = null;
        if (component.Construction && !TryComp(uid, out construction))
            Logger.Warning($"{ToPrettyString(uid)} doesn't have a construction component even though Construction is true!");

        var transform = Transform(uid);
        var coordinates = transform.Coordinates;

        foreach (var (containerId, contents) in component.Containers)
        {
            var container = _containerSystem.EnsureContainer<IContainer>(uid, containerId, containerManagerComponent);
            foreach (var prototypeName in contents)
            {
                var entity = EntityManager.SpawnEntity(prototypeName, coordinates);
                if (!container.Insert(entity, EntityManager, ownerTransform: transform))
                    Logger.Warning($"Couldn't insert {ToPrettyString(entity)} into {ToPrettyString(uid)}!");
            }

            if (construction != null)
                _constructionSystem.AddContainer(uid, containerId, construction);
        }
    }
}