using Content.Server.DeviceNetwork.Systems;
using Content.Shared.CartridgeLoader;
using Content.Shared.Interaction;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.CartridgeLoader;

public sealed class CartridgeLoaderSystem : SharedCartridgeLoaderSystem
{
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    private const string ContainerName = "program-container";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartridgeLoaderComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<CartridgeLoaderComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CartridgeLoaderComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<CartridgeLoaderComponent, CartridgeLoaderUiMessage>(OnLoaderUiMessage);
        SubscribeLocalEvent<CartridgeLoaderComponent, CartridgeUiMessage>(OnUiMessage);
    }

    /// <summary>
    /// Updates the cartridge loaders ui state.
    /// </summary>
    /// <remarks>
    /// Because the cartridge loader integrates with the ui of the entity using it, the entities ui state needs to inherit from <see cref="CartridgeLoaderUiState"/>
    /// and use this method to update its state so the cartridge loaders state can be added to it.
    /// </remarks>
    /// <seealso cref="PDA.PDASystem.UpdatePDAUserInterface"/>
    public void UpdateUiState(EntityUid loaderUid, CartridgeLoaderUiState state, IPlayerSession? session = default!, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        state.ActiveUI = loader.ActiveProgram;
        state.Programs = GetAvailablePrograms(loaderUid, loader);

        var ui = _userInterfaceSystem.GetUiOrNull(loader.Owner, loader.UiKey);
        if (ui != null)
            _userInterfaceSystem.SetUiState(ui, state, session);
    }

    /// <summary>
    /// Updates the programs ui state
    /// </summary>
    /// <param name="loaderUid">The cartridge loaders entity uid</param>
    /// <param name="state">The programs ui state. Programs should use their own ui state class inheriting from <see cref="BoundUserInterfaceState"/></param>
    /// <param name="session">The players session</param>
    /// <param name="loader">The cartridge loader component</param>
    /// <remarks>
    /// This method is called "UpdateCartridgeUiState" but cartridges and a programs are the same. A cartridge is just a program as a visible item.
    /// </remarks>
    /// <seealso cref="Cartridges.NotekeeperCartridgeSystem.UpdateUiState"/>
    public void UpdateCartridgeUiState(EntityUid loaderUid, BoundUserInterfaceState state, IPlayerSession? session = default!, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        var ui = _userInterfaceSystem.GetUiOrNull(loader.Owner, loader.UiKey);
        if (ui != null)
            _userInterfaceSystem.SetUiState(ui, state, session);
    }

    /// <summary>
    /// Returns a list of all installed programs and the inserted cartridge if it isn't already installed
    /// </summary>
    /// <param name="uid">The cartridge loaders uid</param>
    /// <param name="loader">The cartridge loader component</param>
    /// <returns>A list of all the available program entity ids</returns>
    public List<EntityUid> GetAvailablePrograms(EntityUid uid, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(uid, ref loader))
            return new List<EntityUid>();

        //Don't count a cartridge that has already been installed as available to avoid confusion
        if (loader.CartridgeSlot.HasItem && IsInstalled(Prototype(loader.CartridgeSlot.Item!.Value)?.ID, loader))
            return loader.InstalledPrograms;

        var available = new List<EntityUid>();
        available.AddRange(loader.InstalledPrograms);

        if (loader.CartridgeSlot.HasItem)
            available.Add(loader.CartridgeSlot.Item!.Value);

        return available;
    }

    /// <summary>
    /// Installs a cartridge by spawning an invisible version of the cartridges prototype into the cartridge loaders program container program container
    /// </summary>
    /// <param name="loaderUid">The cartridge loader uid</param>
    /// <param name="cartridgeUid">The uid of the cartridge to be installed</param>
    /// <param name="loader">The cartridge loader component</param>
    /// <returns>Whether installing the cartridge was successful</returns>
    public bool InstallCartridge(EntityUid loaderUid, EntityUid cartridgeUid, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader) || loader.InstalledPrograms.Count >= loader.DiskSpace)
            return false;

        //This will eventually be replaced by serializing and deserializing the cartridge to copy it when something needs
        //the data on the cartridge to carry over when installing
        var prototypeId = Prototype(cartridgeUid)?.ID;
        return prototypeId != null && InstallProgram(loaderUid, prototypeId, loader: loader);
    }

    /// <summary>
    /// Installs a program by its prototype
    /// </summary>
    /// <param name="loaderUid">The cartridge loader uid</param>
    /// <param name="prototype">The prototype name</param>
    /// <param name="deinstallable">Whether the program can be deinstalled or not</param>
    /// <param name="loader">The cartridge loader component</param>
    /// <returns>Whether installing the cartridge was successful</returns>
    public bool InstallProgram(EntityUid loaderUid, string prototype, bool deinstallable = true, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader) || loader.InstalledPrograms.Count >= loader.DiskSpace)
            return false;

        if (!_containerSystem.TryGetContainer(loaderUid, ContainerName, out var container))
            return false;

        //Prevent installing cartridges that have already been installed
        if (IsInstalled(prototype, loader))
            return false;

        var installedProgram = Spawn(prototype, new EntityCoordinates(loaderUid, 0, 0));
        container?.Insert(installedProgram);

        UpdateCartridgeInstallationStatus(installedProgram, deinstallable ? InstallationStatus.Installed : InstallationStatus.Readonly);
        loader.InstalledPrograms.Add(installedProgram);

        RaiseLocalEvent(installedProgram, new CartridgeAddedEvent(loaderUid));
        UpdateUserInterfaceState(loaderUid, loader);
        return true;
    }

    /// <summary>
    /// Uninstalls a program using its uid
    /// </summary>
    /// <param name="loaderUid">The cartridge loader uid</param>
    /// <param name="programUid">The uid of the program to be uninstalled</param>
    /// <param name="loader">The cartridge loader component</param>
    /// <returns>Whether uninstalling the program was successful</returns>
    public bool UninstallProgram(EntityUid loaderUid, EntityUid programUid, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader) || !ContainsCartridge(programUid, loader, true))
            return false;

        if (loader.ActiveProgram == programUid)
            loader.ActiveProgram = null;

        loader.BackgroundPrograms.Remove(programUid);
        loader.InstalledPrograms.Remove(programUid);
        EntityManager.QueueDeleteEntity(programUid);
        UpdateUserInterfaceState(loaderUid, loader);
        return true;
    }

    /// <summary>
    /// Activates a program or cartridge and displays its ui fragment. Deactivates any previously active program.
    /// </summary>
    public void ActivateProgram(EntityUid loaderUid, EntityUid programUid, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!ContainsCartridge(programUid, loader))
            return;

        if (loader.ActiveProgram.HasValue)
            DeactivateProgram(loaderUid, programUid, loader);

        if (!loader.BackgroundPrograms.Contains(programUid))
            RaiseLocalEvent(programUid, new CartridgeActivatedEvent(loaderUid));

        loader.ActiveProgram = programUid;
        UpdateUserInterfaceState(loaderUid, loader);
    }

    /// <summary>
    /// Deactivates the currently active program or cartridge.
    /// </summary>
    public void DeactivateProgram(EntityUid loaderUid, EntityUid programUid, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!ContainsCartridge(programUid, loader) || loader.ActiveProgram != programUid)
            return;

        if (!loader.BackgroundPrograms.Contains(programUid))
            RaiseLocalEvent(programUid, new CartridgeDeactivatedEvent(programUid));

        loader.ActiveProgram = default;
        UpdateUserInterfaceState(loaderUid, loader);
    }

    /// <summary>
    /// Registers the given program as a running in the background. Programs running in the background will receive certain events like device net packets but not ui messages
    /// </summary>
    /// <remarks>
    /// Programs wanting to use this functionality will have to provide a way to register and unregister themselves as background programs through their ui fragment.
    /// </remarks>
    public void RegisterBackgroundProgram(EntityUid loaderUid, EntityUid cartridgeUid, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!ContainsCartridge(cartridgeUid, loader))
            return;

        if (loader.ActiveProgram != cartridgeUid)
            RaiseLocalEvent(cartridgeUid, new CartridgeActivatedEvent(loaderUid));

        loader.BackgroundPrograms.Add(cartridgeUid);
    }

    /// <summary>
    /// Unregisters the given program as running in the background
    /// </summary>
    public void UnregisterBackgroundProgram(EntityUid loaderUid, EntityUid cartridgeUid, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!ContainsCartridge(cartridgeUid, loader))
            return;

        if (loader.ActiveProgram != cartridgeUid)
            RaiseLocalEvent(cartridgeUid, new CartridgeDeactivatedEvent(loaderUid));

        loader.BackgroundPrograms.Remove(cartridgeUid);
    }

    protected override void OnItemInserted(EntityUid uid, CartridgeLoaderComponent loader, EntInsertedIntoContainerMessage args)
    {
        RaiseLocalEvent(args.Entity, new CartridgeAddedEvent(uid));
        base.OnItemInserted(uid, loader, args);
    }

    protected override void OnItemRemoved(EntityUid uid, CartridgeLoaderComponent loader, EntRemovedFromContainerMessage args)
    {
        var deactivate = loader.BackgroundPrograms.Remove(args.Entity);

        if (loader.ActiveProgram == args.Entity)
        {
            loader.ActiveProgram = default;
            deactivate = true;
        }

        if (deactivate)
            RaiseLocalEvent(args.Entity, new CartridgeDeactivatedEvent(uid));

        RaiseLocalEvent(args.Entity, new CartridgeRemovedEvent(uid));
        base.OnItemRemoved(uid, loader, args);
    }

    /// <summary>
    /// Installs programs from the list of preinstalled programs
    /// </summary>
    private void OnMapInit(EntityUid uid, CartridgeLoaderComponent component, MapInitEvent args)
    {
        foreach (var prototype in component.PreinstalledPrograms)
        {
            InstallProgram(uid, prototype, deinstallable: false);
        }
    }

    private void OnUsed(EntityUid uid, CartridgeLoaderComponent component, AfterInteractEvent args)
    {
        RelayEvent(component, new CartridgeAfterInteractEvent(uid, args));
    }

    private void OnPacketReceived(EntityUid uid, CartridgeLoaderComponent component, DeviceNetworkPacketEvent args)
    {
        RelayEvent(component, new CartridgeDeviceNetPacketEvent(uid, args));
    }

    private void OnLoaderUiMessage(EntityUid loaderUid, CartridgeLoaderComponent component, CartridgeLoaderUiMessage message)
    {
        switch (message.Action)
        {
            case CartridgeUiMessageAction.Activate:
                ActivateProgram(loaderUid, message.CartridgeUid, component);
                break;
            case CartridgeUiMessageAction.Deactivate:
                DeactivateProgram(loaderUid, message.CartridgeUid, component);
                break;
            case CartridgeUiMessageAction.Install:
                InstallCartridge(loaderUid, message.CartridgeUid, component);
                break;
            case CartridgeUiMessageAction.Uninstall:
                UninstallProgram(loaderUid, message.CartridgeUid, component);
                break;
            case CartridgeUiMessageAction.UIReady:
                if (component.ActiveProgram.HasValue)
                    RaiseLocalEvent(component.ActiveProgram.Value, new CartridgeUiReadyEvent(loaderUid));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Relays ui messages meant for cartridges to the currently active cartridge
    /// </summary>
    private void OnUiMessage(EntityUid uid, CartridgeLoaderComponent component, CartridgeUiMessage args)
    {
        var cartridgeEvent = args.MessageEvent;
        cartridgeEvent.LoaderUid = uid;

        RelayEvent(component, cartridgeEvent, true);
    }

    /// <summary>
    /// Relays events to the currently active program and and programs running in the background.
    /// Skips background programs if "skipBackgroundPrograms" is set to true
    /// </summary>
    /// <param name="loader">The cartritge loader component</param>
    /// <param name="args">The event to be relayed</param>
    /// <param name="skipBackgroundPrograms">Whether to skip relaying the event to programs running in the background</param>
    private void RelayEvent<TEvent>(CartridgeLoaderComponent loader, TEvent args, bool skipBackgroundPrograms = false) where TEvent : notnull
    {
        if (loader.ActiveProgram.HasValue)
            RaiseLocalEvent(loader.ActiveProgram.Value, args);

        if (skipBackgroundPrograms)
            return;

        foreach (var program in loader.BackgroundPrograms)
        {
            //Prevent programs registered as running in the background receiving events twice if they are active
            if (loader.ActiveProgram.HasValue && loader.ActiveProgram.Value.Equals(program))
                continue;

            RaiseLocalEvent(program, args);
        }
    }

    /// <summary>
    /// Checks if a program is already installed by searching for its prototype name in the list of installed programs
    /// </summary>
    private bool IsInstalled(string? prototype, CartridgeLoaderComponent loader)
    {
        foreach (var program in loader.InstalledPrograms)
        {
            if (Prototype(program)?.ID == prototype)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Shortcut for updating the loaders user interface state without passing in a subtype of <see cref="CartridgeLoaderUiState"/>
    /// like the <see cref="PDA.PDASystem"/> does when updating its ui state
    /// </summary>
    /// <seealso cref="PDA.PDASystem.UpdatePDAUserInterface"/>
    private void UpdateUserInterfaceState(EntityUid loaderUid, CartridgeLoaderComponent loader)
    {
        UpdateUiState(loaderUid, new CartridgeLoaderUiState(), null, loader);
    }

    private void UpdateCartridgeInstallationStatus(EntityUid cartridgeUid, InstallationStatus installationStatus, CartridgeComponent? cartridgeComponent = default!)
    {
        if (Resolve(cartridgeUid, ref cartridgeComponent))
        {
            cartridgeComponent.InstallationStatus = installationStatus;
            Dirty(cartridgeComponent);
        }
    }

    private bool ContainsCartridge(EntityUid cartridgeUid, CartridgeLoaderComponent loader , bool onlyInstalled = false)
    {
        return !onlyInstalled && loader.CartridgeSlot.Item?.Equals(cartridgeUid) == true || loader.InstalledPrograms.Contains(cartridgeUid);
    }
}

/// <summary>
/// Gets sent to running programs when the cartridge loader receives a device net package
/// </summary>
/// <seealso cref="DeviceNetworkPacketEvent"/>
public sealed class CartridgeDeviceNetPacketEvent : EntityEventArgs
{
    public readonly EntityUid Loader;
    public readonly DeviceNetworkPacketEvent PacketEvent;

    public CartridgeDeviceNetPacketEvent(EntityUid loader, DeviceNetworkPacketEvent packetEvent)
    {
        Loader = loader;
        PacketEvent = packetEvent;
    }
}

/// <summary>
/// Gets sent to running programs when the cartridge loader receives an after interact event
/// </summary>
/// <seealso cref="AfterInteractEvent"/>
public sealed class CartridgeAfterInteractEvent : EntityEventArgs
{
    public readonly EntityUid Loader;
    public readonly AfterInteractEvent InteractEvent;

    public CartridgeAfterInteractEvent(EntityUid loader, AfterInteractEvent interactEvent)
    {
        Loader = loader;
        InteractEvent = interactEvent;
    }
}
