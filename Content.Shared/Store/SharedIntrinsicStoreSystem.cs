using Content.Shared.Actions;
using Content.Shared.Store.Components;

namespace Content.Shared.Store;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedIntrinsicStoreSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntrinsicStoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IntrinsicStoreComponent, ComponentShutdown>(OnShutdown);
    }


    private void OnMapInit(Entity<IntrinsicStoreComponent> ent, ref MapInitEvent args)
    {
        _action.AddAction(ent.Owner, ref ent.Comp.StoreAction, ent.Comp.StoreActionId);
    }

    private void OnShutdown(Entity<IntrinsicStoreComponent> ent, ref ComponentShutdown args)
    {
        _action.RemoveAction(ent.Owner, ent.Comp.StoreAction);
    }


}
