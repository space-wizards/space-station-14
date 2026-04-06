using System.Diagnostics.CodeAnalysis;
using Content.Shared.Implants;
using Content.Shared.Store.Components;

namespace Content.Shared.Store;

/// <summary>
/// Manages general interactions with a store and different entities,
/// getting listings for stores, and interfacing with the store UI.
/// </summary>
public abstract partial class SharedStoreSystem : EntitySystem
{
    [Dependency] protected readonly EntityQuery<StoreComponent> StoreQuery = default!;
    [Dependency] protected readonly EntityQuery<RemoteStoreComponent> RemoteStoreQuery = default!;

    /// <summary>
    /// Attempts to find a store connected to this entity.
    /// First checking for a <see cref="StoreComponent"/> on this entity,
    /// then checking for a <see cref="RemoteStoreComponent"/> to find a remotely connected store.
    /// </summary>
    /// <param name="entity">Entity we're checking for an attached store on</param>
    /// <param name="store">Store entity we're returning.</param>
    /// <returns>True if a store was found.</returns>
    public bool TryGetStore(Entity<RemoteStoreComponent?> entity, [NotNullWhen(true)] out Entity<StoreComponent>? store)
    {
        store = GetStore(entity);
        return store != null;
    }

    /// <summary>
    /// Attempts to find a store connected to this entity.
    /// First checking for a <see cref="StoreComponent"/> on this entity,
    /// then checking for a <see cref="RemoteStoreComponent"/> to find a remotely connected store.
    /// </summary>
    /// <param name="entity">Entity we're checking for an attached store on</param>
    /// <returns>The store entity and component if found.</returns>
    public Entity<StoreComponent>? GetStore(Entity<RemoteStoreComponent?> entity)
    {
        if (StoreQuery.TryComp(entity, out var storeComp))
            return (entity, storeComp);

        return GetRemoteStore(entity);
    }

    /// <summary>
    /// Attempts to find a remote store connected to this entity.
    /// Checking for a <see cref="RemoteStoreComponent"/> with an attached store entity.
    /// </summary>
    /// <param name="entity">Entity we're checking for an attached store on</param>
    /// <returns>The store entity and component if found.</returns>
    public Entity<StoreComponent>? GetRemoteStore(Entity<RemoteStoreComponent?> entity)
    {
        if (RemoteStoreQuery.Resolve(entity, ref entity.Comp)
            && entity.Comp.Store != null
            && StoreQuery.TryComp(entity.Comp.Store, out var storeComp))
            return (entity.Comp.Store.Value, storeComp);

        return null;
    }

    public void SetRemoteStore(Entity<RemoteStoreComponent?> entity, EntityUid? store)
    {
        if (!RemoteStoreQuery.Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.Store = store;
    }
}
