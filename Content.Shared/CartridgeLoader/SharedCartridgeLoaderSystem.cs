using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.CartridgeLoader;

public abstract class SharedCartridgeLoaderSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartridgeLoaderComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CartridgeLoaderComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<CartridgeLoaderComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<CartridgeLoaderComponent, EntRemovedFromContainerMessage>(OnItemRemoved);

        SubscribeLocalEvent<CartridgeComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<CartridgeComponent, ComponentHandleState>(OnHandleState);

    }

    private void OnComponentInit(EntityUid uid, CartridgeLoaderComponent loader, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, CartridgeLoaderComponent.CartridgeSlotId, loader.CartridgeSlot);
    }

    /// <summary>
    /// Marks installed program entities for deletion when the component gets removed
    /// </summary>
    private void OnComponentRemove(EntityUid uid, CartridgeLoaderComponent loader, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, loader.CartridgeSlot);

        foreach (var program in loader.InstalledPrograms)
        {
               EntityManager.QueueDeleteEntity(program);
        }
    }

    protected virtual void OnItemInserted(EntityUid uid, CartridgeLoaderComponent loader, EntInsertedIntoContainerMessage args)
    {
        UpdateAppearanceData(uid, loader);
    }

    protected virtual void OnItemRemoved(EntityUid uid, CartridgeLoaderComponent loader, EntRemovedFromContainerMessage args)
    {
        UpdateAppearanceData(uid, loader);
    }

    private void OnGetState(EntityUid uid, CartridgeComponent component, ref ComponentGetState args)
    {
        var state = new CartridgeComponentState();
        state.InstallationStatus = component.InstallationStatus;

        args.State = state;
    }

    private void OnHandleState(EntityUid uid, CartridgeComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not CartridgeComponentState state)
            return;

        component.InstallationStatus = state.InstallationStatus;
    }

    private void UpdateAppearanceData(EntityUid uid, CartridgeLoaderComponent loader)
    {
        _appearanceSystem.SetData(uid, CartridgeLoaderVisuals.CartridgeInserted, loader.CartridgeSlot.HasItem);
    }
}

/// <summary>
/// Gets sent to program / cartridge entities when they get inserted or installed
/// </summary>
public sealed class CartridgeAddedEvent : EntityEventArgs
{
    public readonly EntityUid Loader;

    public CartridgeAddedEvent(EntityUid loader)
    {
        Loader = loader;
    }
}

/// <summary>
/// Gets sent to cartridge entities when they get ejected
/// </summary>
public sealed class CartridgeRemovedEvent : EntityEventArgs
{
    public readonly EntityUid Loader;

    public CartridgeRemovedEvent(EntityUid loader)
    {
        Loader = loader;
    }
}

/// <summary>
/// Gets sent to program / cartridge entities when they get activated
/// </summary>
/// <remarks>
/// Don't update the programs ui state in this events listener
/// </remarks>
public sealed class CartridgeActivatedEvent : EntityEventArgs
{
    public readonly EntityUid Loader;

    public CartridgeActivatedEvent(EntityUid loader)
    {
        Loader = loader;
    }
}

/// <summary>
/// Gets sent to program / cartridge entities when they get deactivated
/// </summary>
public sealed class CartridgeDeactivatedEvent : EntityEventArgs
{
    public readonly EntityUid Loader;

    public CartridgeDeactivatedEvent(EntityUid loader)
    {
        Loader = loader;
    }
}

/// <summary>
/// Gets sent to program / cartridge entities when the ui is ready to be updated by the cartridge.
/// </summary>
/// <remarks>
/// This is used for the initial ui state update because updating the ui in the activate event doesn't work
/// </remarks>
public sealed class CartridgeUiReadyEvent : EntityEventArgs
{
    public readonly EntityUid Loader;

    public CartridgeUiReadyEvent(EntityUid loader)
    {
        Loader = loader;
    }
}
