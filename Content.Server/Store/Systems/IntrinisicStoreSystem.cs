using Content.Server.Actions;
using Content.Shared.Store.Components;
using Content.Shared.Store.Events;

namespace Content.Server.Store.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class IntrinisicStoreSystem : EntitySystem
{
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ActionsSystem _action = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntrinsicStoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IntrinsicStoreComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<IntrinsicStoreComponent, IntrinisicStoreActionEvent>(OnIntrinsicStore);
    }

    private void OnMapInit(Entity<IntrinsicStoreComponent> ent, ref MapInitEvent args)
    {
        _action.AddAction(ent.Owner, ent.Comp.ShopActionId);
    }

    private void OnShutdown(Entity<IntrinsicStoreComponent> ent, ref ComponentShutdown args)
    {
        _action.RemoveAction(ent.Owner, ent.Comp.ShopAction);
    }

    private void OnIntrinsicStore(Entity<IntrinsicStoreComponent> ent, ref IntrinisicStoreActionEvent args)
    {
        if (!TryComp<StoreComponent>(ent.Owner, out var store))
            return;
        _store.ToggleUi(ent.Owner, ent.Owner, store);
    }

}
