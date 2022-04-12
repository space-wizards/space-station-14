using Content.Shared.Storage;
using static Robust.Client.UserInterface.Controls.BaseButton;
using Content.Client.Items.Managers;

namespace Content.Client.Storage;

// TODO kill this is all horrid.
public sealed class StorageSystem : EntitySystem
{

    [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AnimateInsertingEntitiesEvent>(OnAnimateInsertingEntities);
    }

    /// <summary>
    /// Function for clicking one of the stored entity buttons in the UI, tells server to remove that entity
    /// </summary>
    /// <param name="entity"></param>
    public void Interact(ButtonEventArgs args, EntityUid entity)
    {
        if (EntityManager.EntityExists(entity))
            _itemSlotManager.OnButtonPressed(args.Event, entity);
    }

    private void OnAnimateInsertingEntities(AnimateInsertingEntitiesEvent ev)
    {
        if (TryComp<ClientStorageComponent>(ev.Storage, out var storage))
        {
            storage.HandleAnimatingInsertingEntities(ev);
        }
    }
}
