using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.CartridgeLoader;

public abstract partial class SharedCartridgeLoaderSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeRelay();

        SubscribeLocalEvent<CartridgeLoaderComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CartridgeLoaderComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<CartridgeLoaderComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<CartridgeLoaderComponent, EntRemovedFromContainerMessage>(OnItemRemoved);

        SubscribeLocalEvent<CartridgeLoaderComponent, CartridgeLoaderUiMessage>(OnLoaderUiMessage);
        SubscribeLocalEvent<CartridgeLoaderComponent, CartridgeUiMessage>(OnUiMessage);
    }

    private void OnComponentInit(Entity<CartridgeLoaderComponent> ent, ref ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(ent, CartridgeLoaderComponent.CartridgeSlotId, ent.Comp.CartridgeSlot);
        _container.EnsureContainer<Container>(ent, CartridgeLoaderComponent.RemovableContainerId);
        _container.EnsureContainer<Container>(ent, CartridgeLoaderComponent.UnremovableContainerId);
    }

    /// <summary>
    /// Marks installed program entities for deletion when the component gets removed
    /// </summary>
    private void OnComponentRemove(Entity<CartridgeLoaderComponent> ent, ref ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(ent, ent.Comp.CartridgeSlot);
        if (_container.TryGetContainer(ent, CartridgeLoaderComponent.RemovableContainerId, out var removable))
            _container.ShutdownContainer(removable);
        if (_container.TryGetContainer(ent, CartridgeLoaderComponent.UnremovableContainerId, out var unremovable))
            _container.ShutdownContainer(unremovable);
    }

    private void OnItemInserted(Entity<CartridgeLoaderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != CartridgeLoaderComponent.RemovableContainerId && args.Container.ID != CartridgeLoaderComponent.UnremovableContainerId && args.Container.ID != CartridgeLoaderComponent.CartridgeSlotId)
            return;

        if (TryComp<CartridgeComponent>(args.Entity, out var cartridge))
        {
            cartridge.LoaderUid = ent;
            Dirty(args.Entity, cartridge);

            if (args.Container.ID == CartridgeLoaderComponent.RemovableContainerId)
                UpdateCartridgeInstallationStatus((args.Entity, cartridge), InstallationStatus.Installed);
            else if (args.Container.ID == CartridgeLoaderComponent.UnremovableContainerId)
                UpdateCartridgeInstallationStatus((args.Entity, cartridge), InstallationStatus.Readonly);
            else
                UpdateCartridgeInstallationStatus((args.Entity, cartridge), InstallationStatus.Cartridge);
        }

        RaiseLocalEvent(args.Entity, new CartridgeAddedEvent(ent));
        UpdateUiState(ent.AsNullable());
        UpdateAppearanceData(ent);
    }

    private void OnItemRemoved(Entity<CartridgeLoaderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != CartridgeLoaderComponent.RemovableContainerId && args.Container.ID != CartridgeLoaderComponent.UnremovableContainerId && args.Container.ID != CartridgeLoaderComponent.CartridgeSlotId)
            return;

        if (ent.Comp.ActiveProgram == args.Entity)
        {
            ent.Comp.ActiveProgram = null;
            Dirty(ent);
            RaiseLocalEvent(args.Entity, new CartridgeDeactivatedEvent(ent));
        }

        if (TryComp<CartridgeComponent>(args.Entity, out var cartridge))
        {
            cartridge.LoaderUid = null;
            Dirty(args.Entity, cartridge);
        }

        RaiseLocalEvent(args.Entity, new CartridgeRemovedEvent(ent));
        UpdateUiState(ent.AsNullable());
        UpdateAppearanceData(ent);
    }

    private void UpdateAppearanceData(Entity<CartridgeLoaderComponent> ent)
    {
        _appearanceSystem.SetData(ent.Owner, CartridgeLoaderVisuals.CartridgeInserted, ent.Comp.CartridgeSlot.HasItem);
    }

    public void SendNotification(EntityUid loaderUid, string header, string message, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!loader.NotificationsEnabled)
            return;

        var args = new CartridgeLoaderNotificationSentEvent(header, message);
        RaiseLocalEvent(loaderUid, ref args);
    }
}

/// <summary>
/// Gets sent to program / cartridge entities when they get inserted or installed
/// </summary>
public sealed class CartridgeAddedEvent : EntityEventArgs
{
    public readonly Entity<CartridgeLoaderComponent> Loader;

    public CartridgeAddedEvent(Entity<CartridgeLoaderComponent> loader)
    {
        Loader = loader;
    }
}

/// <summary>
/// Gets sent to cartridge entities when they get ejected
/// </summary>
public sealed class CartridgeRemovedEvent : EntityEventArgs
{
    public readonly Entity<CartridgeLoaderComponent> Loader;

    public CartridgeRemovedEvent(Entity<CartridgeLoaderComponent> loader)
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
    public readonly Entity<CartridgeLoaderComponent> Loader;

    public CartridgeActivatedEvent(Entity<CartridgeLoaderComponent> loader)
    {
        Loader = loader;
    }
}

/// <summary>
/// Gets sent to program / cartridge entities when they get deactivated
/// </summary>
public sealed class CartridgeDeactivatedEvent : EntityEventArgs
{
    public readonly Entity<CartridgeLoaderComponent> Loader;

    public CartridgeDeactivatedEvent(Entity<CartridgeLoaderComponent> loader)
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
    public readonly Entity<CartridgeLoaderComponent> Loader;

    public CartridgeUiReadyEvent(Entity<CartridgeLoaderComponent> loader)
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

/// <summary>
/// Raised on an attempt of program installation.
/// </summary>
[ByRefEvent]
public record struct ProgramInstallationAttempt(EntityUid LoaderUid, string Prototype, bool Cancelled = false);
