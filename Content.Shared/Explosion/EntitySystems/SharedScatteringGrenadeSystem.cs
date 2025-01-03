using Content.Shared.Explosion.Components;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Explosion.EntitySystems;

public abstract class SharedScatteringGrenadeSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScatteringGrenadeComponent, ComponentInit>(OnScatteringInit);
        SubscribeLocalEvent<ScatteringGrenadeComponent, ComponentStartup>(OnScatteringStartup);
        SubscribeLocalEvent<ScatteringGrenadeComponent, InteractUsingEvent>(OnScatteringInteractUsing);
    }

    private void OnScatteringInit(Entity<ScatteringGrenadeComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Container = _container.EnsureContainer<Container>(entity.Owner, "cluster-payload");
    }

    /// <summary>
    /// Setting the unspawned count based on capacity, so we know how many new entities to spawn
    /// Update appearance based on initial fill amount
    /// </summary>
    private void OnScatteringStartup(Entity<ScatteringGrenadeComponent> entity, ref ComponentStartup args)
    {
        if (entity.Comp.FillPrototype == null)
            return;

        entity.Comp.UnspawnedCount = Math.Max(0, entity.Comp.Capacity - entity.Comp.Container.ContainedEntities.Count);
        UpdateAppearance(entity);
        Dirty(entity, entity.Comp);

    }

    /// <summary>
    /// There are some scattergrenades you can fill up with more grenades (like clusterbangs)
    /// This covers how you insert more into it
    /// </summary>
    private void OnScatteringInteractUsing(Entity<ScatteringGrenadeComponent> entity, ref InteractUsingEvent args)
    {
        if (entity.Comp.Whitelist == null)
            return;

        if (args.Handled || !_whitelistSystem.IsValid(entity.Comp.Whitelist, args.Used))
            return;

        _container.Insert(args.Used, entity.Comp.Container);
        UpdateAppearance(entity);
        args.Handled = true;
    }

    /// <summary>
    /// Update appearance based off of total count of contents
    /// </summary>
    private void UpdateAppearance(Entity<ScatteringGrenadeComponent> entity)
    {
        if (!TryComp<AppearanceComponent>(entity, out var appearanceComponent))
            return;

        _appearance.SetData(entity, ClusterGrenadeVisuals.GrenadesCounter, entity.Comp.UnspawnedCount + entity.Comp.Container.ContainedEntities.Count, appearanceComponent);
    }
}
