using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Shared.CartridgeLoader;

public abstract class SharedCartridgeLoaderSystem : EntitySystem
{
    public const string InstalledContainerId = "program-container";

    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartridgeLoaderComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CartridgeLoaderComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<CartridgeLoaderComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<CartridgeLoaderComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
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
        if (_container.TryGetContainer(uid, InstalledContainerId, out var cont))
            _container.ShutdownContainer(cont);
    }

    protected virtual void OnItemInserted(EntityUid uid, CartridgeLoaderComponent loader, EntInsertedIntoContainerMessage args)
    {
        UpdateAppearanceData(uid, loader);
    }

    protected virtual void OnItemRemoved(EntityUid uid, CartridgeLoaderComponent loader, EntRemovedFromContainerMessage args)
    {
        UpdateAppearanceData(uid, loader);
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

/// <summary>
/// Gets sent by the cartridge loader system to the cartridge loader entity so another system
/// can handle displaying the notification
/// </summary>
/// <param name="Message">The message to be displayed</param>
[ByRefEvent]
public record struct CartridgeLoaderNotificationSentEvent(string Header, string Message);
