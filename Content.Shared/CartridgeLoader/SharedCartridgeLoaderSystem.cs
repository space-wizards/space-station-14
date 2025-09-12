using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.CartridgeLoader;

public abstract partial class SharedCartridgeLoaderSystem : EntitySystem
{
    public const string InstalledContainerId = "program-container";

    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeRelay();

        SubscribeLocalEvent<CartridgeLoaderComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<CartridgeLoaderComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CartridgeLoaderComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<CartridgeLoaderComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<CartridgeLoaderComponent, EntRemovedFromContainerMessage>(OnItemRemoved);

        SubscribeLocalEvent<CartridgeLoaderComponent, CartridgeLoaderUiMessage>(OnLoaderUiMessage);
        SubscribeLocalEvent<CartridgeLoaderComponent, CartridgeUiMessage>(OnUiMessage);
    }

    /// <summary>
    /// Installs programs from the list of preinstalled programs
    /// </summary>
    private void OnMapInit(EntityUid uid, CartridgeLoaderComponent component, MapInitEvent args)
    {
        // TODO remove this and use container fill.
        foreach (var prototype in component.PreinstalledPrograms)
        {
            InstallProgram(uid, prototype, deinstallable: false);
        }
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

    private void OnItemInserted(EntityUid uid, CartridgeLoaderComponent loader, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != InstalledContainerId && args.Container.ID != loader.CartridgeSlot.ID)
            return;

        if (TryComp(args.Entity, out CartridgeComponent? cartridge))
            cartridge.LoaderUid = uid;

        RaiseLocalEvent(args.Entity, new CartridgeAddedEvent((uid, loader)));
        UpdateAppearanceData(uid, loader);
    }

    private void OnItemRemoved(EntityUid uid, CartridgeLoaderComponent loader, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != InstalledContainerId && args.Container.ID != loader.CartridgeSlot.ID)
            return;

        if (loader.ActiveProgram == args.Entity)
        {
            loader.ActiveProgram = default;
            RaiseLocalEvent(args.Entity, new CartridgeDeactivatedEvent((uid, loader)));
        }

        if (TryComp(args.Entity, out CartridgeComponent? cartridge))
            cartridge.LoaderUid = null;

        RaiseLocalEvent(args.Entity, new CartridgeRemovedEvent((uid, loader)));
        UpdateAppearanceData(uid, loader);
    }

    private void UpdateAppearanceData(EntityUid uid, CartridgeLoaderComponent loader)
    {
        _appearanceSystem.SetData(uid, CartridgeLoaderVisuals.CartridgeInserted, loader.CartridgeSlot.HasItem);
    }

    private void OnLoaderUiMessage(Entity<CartridgeLoaderComponent> ent, ref CartridgeLoaderUiMessage message)
    {
        var cartridge = GetEntity(message.CartridgeUid);

        switch (message.Action)
        {
            case CartridgeUiMessageAction.Activate:
                ActivateProgram(ent, cartridge);
                break;
            case CartridgeUiMessageAction.Deactivate:
                DeactivateProgram(ent, cartridge);
                break;
            case CartridgeUiMessageAction.Install:
                InstallCartridge(ent, cartridge);
                break;
            case CartridgeUiMessageAction.Uninstall:
                UninstallProgram(ent, cartridge);
                break;
            case CartridgeUiMessageAction.UIReady:
                if (ent.Comp.ActiveProgram is { } foreground)
                    RaiseLocalEvent(foreground, new CartridgeUiReadyEvent(ent));
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unrecognized UI action passed from cartridge loader ui {message.Action}.");
        }
    }

    /// <summary>
    /// Relays ui messages meant for cartridges to the currently active cartridge
    /// </summary>
    private void OnUiMessage(Entity<CartridgeLoaderComponent> ent, ref CartridgeUiMessage args)
    {
        var cartridgeEvent = args.MessageEvent;
        cartridgeEvent.User = args.Actor;
        cartridgeEvent.LoaderUid = GetNetEntity(ent);
        cartridgeEvent.Actor = args.Actor;

        RelayEvent(ent, cartridgeEvent, true);
    }

    /// <summary>
    /// Updates the cartridge loaders ui state.
    /// </summary>
    /// <remarks>
    /// Because the cartridge loader integrates with the ui of the entity using it, the entities ui state needs to inherit from <see cref="CartridgeLoaderUiState"/>
    /// and use this method to update its state so the cartridge loaders state can be added to it.
    /// </remarks>
    /// <seealso cref="PDA.PdaSystem.UpdatePdaUserInterface"/>
    public void UpdateUiState(Entity<CartridgeLoaderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!_userInterface.HasUi(ent.Owner, ent.Comp.UiKey))
            return;

        var programs = GetNetEntityList(GetPrograms(ent));
        var state = new CartridgeLoaderUiState(programs, GetNetEntity(ent.Comp.ActiveProgram));
        _userInterface.SetUiState(ent.Owner, ent.Comp.UiKey, state);
    }

    private void UpdateCartridgeInstallationStatus(EntityUid cartridgeUid, InstallationStatus installationStatus, CartridgeComponent cartridgeComponent)
    {
        cartridgeComponent.InstallationStatus = installationStatus;
        Dirty(cartridgeUid, cartridgeComponent);
    }

    /// <summary>
    /// Updates the programs ui state
    /// </summary>
    /// <param name="loaderUid">The cartridge loaders entity uid</param>
    /// <param name="state">The programs ui state. Programs should use their own ui state class inheriting from <see cref="BoundUserInterfaceState"/></param>
    /// <param name="loader">The cartridge loader component</param>
    /// <remarks>
    /// This method is called "UpdateCartridgeUiState" but cartridges and a programs are the same. A cartridge is just a program as a visible item.
    /// </remarks>
    /// <seealso cref="Cartridges.NotekeeperCartridgeSystem.UpdateUiState"/>
    public void UpdateCartridgeUiState(EntityUid loaderUid, BoundUserInterfaceState state, CartridgeLoaderComponent? loader = null)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (_userInterface.HasUi(loaderUid, loader.UiKey))
            _userInterface.SetUiState(loaderUid, loader.UiKey, state);
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
