using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.PDA;
using Content.Shared.CartridgeLoader;
using Content.Shared.Interaction;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.CartridgeLoader;

public sealed class CartridgeLoaderSystem : SharedCartridgeLoaderSystem
{
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly PdaSystem _pda = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartridgeLoaderComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<CartridgeLoaderComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CartridgeLoaderComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<CartridgeLoaderComponent, CartridgeLoaderUiMessage>(OnLoaderUiMessage);
        SubscribeLocalEvent<CartridgeLoaderComponent, CartridgeUiMessage>(OnUiMessage);
    }

    public IReadOnlyList<EntityUid> GetInstalled(EntityUid uid, ContainerManagerComponent? comp = null)
    {
        if (_containerSystem.TryGetContainer(uid, InstalledContainerId, out var container, comp))
            return container.ContainedEntities;

        return Array.Empty<EntityUid>();
    }

    public bool TryGetProgram<T>(
        EntityUid uid,
        [NotNullWhen(true)] out EntityUid? programUid,
        [NotNullWhen(true)] out T? program,
        bool installedOnly = false,
        CartridgeLoaderComponent? loader = null,
        ContainerManagerComponent? containerManager = null) where T : IComponent
    {
        program = default;
        programUid = null;

        if (!_containerSystem.TryGetContainer(uid, InstalledContainerId, out var container, containerManager))
            return false;

        foreach (var prog in container.ContainedEntities)
        {
            if (!TryComp(prog, out program))
                continue;

            programUid = prog;
            return true;
        }

        if (installedOnly)
            return false;

        if (!Resolve(uid, ref loader) || !TryComp(loader.CartridgeSlot.Item, out program))
            return false;

        programUid = loader.CartridgeSlot.Item;
        return true;
    }

    public bool TryGetProgram<T>(
        EntityUid uid,
        [NotNullWhen(true)] out EntityUid? programUid,
        bool installedOnly = false,
        CartridgeLoaderComponent? loader = null,
        ContainerManagerComponent? containerManager = null) where T : IComponent
    {
        return TryGetProgram<T>(uid, out programUid, out _, installedOnly, loader, containerManager);
    }

    public bool HasProgram<T>(
        EntityUid uid,
        bool installedOnly = false,
        CartridgeLoaderComponent? loader = null,
        ContainerManagerComponent? containerManager = null) where T : IComponent
    {
        return TryGetProgram<T>(uid, out _, out _, installedOnly, loader, containerManager);
    }

    /// <summary>
    /// Updates the cartridge loaders ui state.
    /// </summary>
    /// <remarks>
    /// Because the cartridge loader integrates with the ui of the entity using it, the entities ui state needs to inherit from <see cref="CartridgeLoaderUiState"/>
    /// and use this method to update its state so the cartridge loaders state can be added to it.
    /// </remarks>
    /// <seealso cref="PDA.PdaSystem.UpdatePdaUserInterface"/>
    public void UpdateUiState(EntityUid loaderUid, ICommonSession? session, CartridgeLoaderComponent? loader)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!_userInterfaceSystem.TryGetUi(loaderUid, loader.UiKey, out var ui))
            return;

        var programs = GetAvailablePrograms(loaderUid, loader);
        var state = new CartridgeLoaderUiState(programs, GetNetEntity(loader.ActiveProgram));
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
    public void UpdateCartridgeUiState(EntityUid loaderUid, BoundUserInterfaceState state, ICommonSession? session = default!, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (_userInterfaceSystem.TryGetUi(loaderUid, loader.UiKey, out var ui))
            _userInterfaceSystem.SetUiState(ui, state, session);
    }

    /// <summary>
    /// Returns a list of all installed programs and the inserted cartridge if it isn't already installed
    /// </summary>
    /// <param name="uid">The cartridge loaders uid</param>
    /// <param name="loader">The cartridge loader component</param>
    /// <returns>A list of all the available program entity ids</returns>
    public List<NetEntity> GetAvailablePrograms(EntityUid uid, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(uid, ref loader))
            return new List<NetEntity>();

        var available = GetNetEntityList(GetInstalled(uid));

        if (loader.CartridgeSlot.Item is not { } cartridge)
            return available;

        // TODO exclude duplicate programs. Or something I dunno I CBF fixing this mess.
        available.Add(GetNetEntity(cartridge));
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
        if (!Resolve(loaderUid, ref loader))
            return false;

        //This will eventually be replaced by serializing and deserializing the cartridge to copy it when something needs
        //the data on the cartridge to carry over when installing

        // For anyone stumbling onto this: Do not do this or I will cut you.
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
    public bool InstallProgram(EntityUid loaderUid, string prototype, bool deinstallable = true, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return false;

        if (!_containerSystem.TryGetContainer(loaderUid, InstalledContainerId, out var container))
            return false;

        if (container.Count >= loader.DiskSpace)
            return false;

        // TODO cancel duplicate program installations
        var ev = new ProgramInstallationAttempt(loaderUid, prototype);
        RaiseLocalEvent(ref ev);

        if (ev.Cancelled)
            return false;

        var installedProgram = Spawn(prototype, new EntityCoordinates(loaderUid, 0, 0));
        _containerSystem.Insert(installedProgram, container);

        UpdateCartridgeInstallationStatus(installedProgram, deinstallable ? InstallationStatus.Installed : InstallationStatus.Readonly);

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
    public bool UninstallProgram(EntityUid loaderUid, EntityUid programUid, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return false;

        if (!GetInstalled(loaderUid).Contains(programUid))
            return false;

        if (loader.ActiveProgram == programUid)
            loader.ActiveProgram = null;

        loader.BackgroundPrograms.Remove(programUid);
        EntityManager.QueueDeleteEntity(programUid);
        UpdateUserInterfaceState(loaderUid, loader);
        return true;
    }

    /// <summary>
    /// Activates a program or cartridge and displays its ui fragment. Deactivates any previously active program.
    /// </summary>
    public void ActivateProgram(EntityUid loaderUid, EntityUid programUid, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!HasProgram(loaderUid, programUid, loader))
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
    public void DeactivateProgram(EntityUid loaderUid, EntityUid programUid, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!HasProgram(loaderUid, programUid, loader) || loader.ActiveProgram != programUid)
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
    public void RegisterBackgroundProgram(EntityUid loaderUid, EntityUid cartridgeUid, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!HasProgram(loaderUid, cartridgeUid, loader))
            return;

        if (loader.ActiveProgram != cartridgeUid)
            RaiseLocalEvent(cartridgeUid, new CartridgeActivatedEvent(loaderUid));

        loader.BackgroundPrograms.Add(cartridgeUid);
    }

    /// <summary>
    /// Unregisters the given program as running in the background
    /// </summary>
    public void UnregisterBackgroundProgram(EntityUid loaderUid, EntityUid cartridgeUid, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!HasProgram(loaderUid, cartridgeUid, loader))
            return;

        if (loader.ActiveProgram != cartridgeUid)
            RaiseLocalEvent(cartridgeUid, new CartridgeDeactivatedEvent(loaderUid));

        loader.BackgroundPrograms.Remove(cartridgeUid);
    }

    protected override void OnItemInserted(EntityUid uid, CartridgeLoaderComponent loader, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != InstalledContainerId && args.Container.ID != loader.CartridgeSlot.ID)
            return;

        RaiseLocalEvent(args.Entity, new CartridgeAddedEvent(uid));
        base.OnItemInserted(uid, loader, args);
    }

    protected override void OnItemRemoved(EntityUid uid, CartridgeLoaderComponent loader, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != InstalledContainerId && args.Container.ID != loader.CartridgeSlot.ID)
            return;

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

        _pda.UpdatePdaUi(uid);
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
        var cartridge = GetEntity(message.CartridgeUid);

        switch (message.Action)
        {
            case CartridgeUiMessageAction.Activate:
                ActivateProgram(loaderUid, cartridge, component);
                break;
            case CartridgeUiMessageAction.Deactivate:
                DeactivateProgram(loaderUid, cartridge, component);
                break;
            case CartridgeUiMessageAction.Install:
                InstallCartridge(loaderUid, cartridge, component);
                break;
            case CartridgeUiMessageAction.Uninstall:
                UninstallProgram(loaderUid, cartridge, component);
                break;
            case CartridgeUiMessageAction.UIReady:
                if (component.ActiveProgram.HasValue)
                    RaiseLocalEvent(component.ActiveProgram.Value, new CartridgeUiReadyEvent(loaderUid));
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unrecognized UI action passed from cartridge loader ui {message.Action}.");
        }
    }

    /// <summary>
    /// Relays ui messages meant for cartridges to the currently active cartridge
    /// </summary>
    private void OnUiMessage(EntityUid uid, CartridgeLoaderComponent component, CartridgeUiMessage args)
    {
        var cartridgeEvent = args.MessageEvent;
        cartridgeEvent.LoaderUid = GetNetEntity(uid);

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
    /// Shortcut for updating the loaders user interface state without passing in a subtype of <see cref="CartridgeLoaderUiState"/>
    /// like the <see cref="PDA.PdaSystem"/> does when updating its ui state
    /// </summary>
    /// <seealso cref="PDA.PdaSystem.UpdatePdaUserInterface"/>
    private void UpdateUserInterfaceState(EntityUid loaderUid, CartridgeLoaderComponent loader)
    {
        UpdateUiState(loaderUid, null, loader);
    }

    private void UpdateCartridgeInstallationStatus(EntityUid cartridgeUid, InstallationStatus installationStatus, CartridgeComponent? cartridgeComponent = default!)
    {
        if (Resolve(cartridgeUid, ref cartridgeComponent))
        {
            cartridgeComponent.InstallationStatus = installationStatus;
            Dirty(cartridgeUid, cartridgeComponent);
        }
    }

    private bool HasProgram(EntityUid loader, EntityUid program, CartridgeLoaderComponent component)
    {
        return component.CartridgeSlot.Item == program || GetInstalled(loader).Contains(program);
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

/// <summary>
/// Raised on an attempt of program installation.
/// </summary>
[ByRefEvent]
public record struct ProgramInstallationAttempt(EntityUid LoaderUid, string Prototype, bool Cancelled = false);
