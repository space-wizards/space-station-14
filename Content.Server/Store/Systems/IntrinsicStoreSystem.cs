using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Store.Events;

namespace Content.Server.Store.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class IntrinsicStoreSystem : SharedIntrinsicStoreSystem
{
    [Dependency] private readonly StoreSystem _store = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntrinsicStoreComponent, IntrinsicStoreActionEvent>(OnIntrinsicStore);
    }

    private void OnIntrinsicStore(Entity<IntrinsicStoreComponent> ent, ref IntrinsicStoreActionEvent args)
    {
        if (!TryComp<StoreComponent>(ent.Owner, out var store))
            return;

        _store.ToggleUi(args.Performer, ent.Owner, store);
    }

}
