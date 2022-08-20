using Content.Server.DeviceNetwork.Systems;
using Content.Server.UserInterface;
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

    private const string ContainerName = "program_container";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartridgeLoaderComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CartridgeLoaderComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<CartridgeLoaderComponent, CartridgeLoaderUiMessage>(OnUiMessage);
    }

    public void UpdateUiState(EntityUid loaderUid, CartridgeLoaderUiState state, IPlayerSession? session = default!, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        state.ActiveUI = loader.ActiveProgram;
        state.Programs = GetAvailablePrograms(loaderUid, loader);

        loader.Owner.GetUIOrNull(loader.UiKey)?.SetState(state, session);
    }

    public void UpdateCartridgeUiState(EntityUid loaderUid, BoundUserInterfaceState state, IPlayerSession? session = default!, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        loader.Owner.GetUIOrNull(loader.UiKey)?.SetState(state, session);
    }

    public List<EntityUid> GetAvailablePrograms(EntityUid uid, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(uid, ref loader))
            return new List<EntityUid>();

        //Don't count a cartridge that has been installed as available to avoid confusion
        if (loader.CartridgeSlot.HasItem && IsInstalled(Prototype(loader.CartridgeSlot.Item!.Value)?.ID, loader))
            return loader.InstalledPrograms;

        var available = new List<EntityUid>();
        available.AddRange(loader.InstalledPrograms);

        if (loader.CartridgeSlot.HasItem)
            available.Add(loader.CartridgeSlot.Item!.Value);

        return available;
    }

    public bool InstallCartridge(EntityUid loaderUid, EntityUid cartridgeUid, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader) || loader.InstalledPrograms.Count >= loader.DiskSpace)
            return false;

        //This will eventually be replaced by serializing and deserializing the cartridge to copy it when something needs
        //the data on the cartridge to carry over when installing
        var prototypeId = Prototype(cartridgeUid)?.ID;
        return prototypeId != null && InstallProgram(loaderUid, prototypeId, loader);
    }

    public bool InstallProgram(EntityUid loaderUid, string prototype, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader) || loader.InstalledPrograms.Count >= loader.DiskSpace)
            return false;

        if (_containerSystem?.TryGetContainer(loaderUid, ContainerName, out var container) != true)
            return false;

        //Prevent installing cartridges that have already been installed
        if (IsInstalled(prototype, loader))
            return false;

        var installedProgram = Spawn(prototype, new EntityCoordinates(loaderUid, 0, 0));
        container?.Insert(installedProgram);

        UpdateCartridgeInstallationStatus(installedProgram, InstallationStatus.Installed);
        loader.InstalledPrograms.Add(installedProgram);

        //FIXME: Installing a cartridge does not update the available cartridges on the UI
        RaiseLocalEvent(installedProgram, new CartridgeAddedEvent(loaderUid));
        UpdateUserInterfaceState(loaderUid, loader);
        return true;
    }

    public bool UninstallProgram(EntityUid loaderUid, EntityUid cartridgeUid, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader) || !ContainsCartridge(cartridgeUid, loader, true))
            return false;

        loader.InstalledPrograms.Remove(cartridgeUid);
        EntityManager.QueueDeleteEntity(cartridgeUid);
        UpdateUserInterfaceState(loaderUid, loader);
        return true;
    }

    public void ActivateProgram(EntityUid loaderUid, EntityUid cartridgeUid, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!ContainsCartridge(cartridgeUid, loader))
            return;

        if (loader.ActiveProgram.HasValue)
            DeactivateProgram(loaderUid, cartridgeUid, loader);

        if (!loader.BackgroundPrograms.Contains(cartridgeUid))
            RaiseLocalEvent(cartridgeUid, new CartridgeActivatedEvent(loaderUid));

        loader.ActiveProgram = cartridgeUid;
        UpdateUserInterfaceState(loaderUid, loader);
    }

    public void DeactivateProgram(EntityUid loaderUid, EntityUid cartridgeUid, CartridgeLoaderComponent? loader  = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (!ContainsCartridge(cartridgeUid, loader) || loader.ActiveProgram != cartridgeUid)
            return;

        if (!loader.BackgroundPrograms.Contains(cartridgeUid))
            RaiseLocalEvent(cartridgeUid, new CartridgeDeactivatedEvent(cartridgeUid));

        loader.ActiveProgram = default;
        UpdateUserInterfaceState(loaderUid, loader);
    }

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

    protected override void OnItemInserted(EntityUid uid, SharedCartridgeLoaderComponent loader, EntInsertedIntoContainerMessage args)
    {
        RaiseLocalEvent(args.Entity, new CartridgeAddedEvent(uid));
        base.OnItemInserted(uid, loader, args);
    }

    protected override void OnItemRemoved(EntityUid uid, SharedCartridgeLoaderComponent loader, EntRemovedFromContainerMessage args)
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

    private void OnUsed(EntityUid uid, CartridgeLoaderComponent component, AfterInteractEvent args)
    {
        RelayEvent(component, new CartridgeAfterInteractEvent(uid, args));
    }

    private void OnPacketReceived(EntityUid uid, CartridgeLoaderComponent component, DeviceNetworkPacketEvent args)
    {
        RelayEvent(component, new CartridgeDeviceNetPacketEvent(uid, args));
    }

    private void OnUiMessage(EntityUid loaderUid, CartridgeLoaderComponent component, CartridgeLoaderUiMessage args)
    {
        switch (args.Action)
        {
            case CartridgeUiMessageAction.Activate:
                ActivateProgram(loaderUid, args.CartridgeUid, component);
                break;
            case CartridgeUiMessageAction.Deactivate:
                DeactivateProgram(loaderUid, args.CartridgeUid, component);
                break;
            case CartridgeUiMessageAction.Install:
                InstallCartridge(loaderUid, args.CartridgeUid, component);
                break;
            case CartridgeUiMessageAction.Uninstall:
                UninstallProgram(loaderUid, args.CartridgeUid, component);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void RelayEvent<TEvent>(CartridgeLoaderComponent loader, TEvent args) where TEvent : notnull
    {
        if (loader.ActiveProgram.HasValue)
            RaiseLocalEvent(loader.ActiveProgram.Value, args);

        foreach (var program in loader.BackgroundPrograms)
        {
            RaiseLocalEvent(program, args);
        }
    }

    private bool IsInstalled(string? prototype, CartridgeLoaderComponent loader)
    {
        foreach (var program in loader.InstalledPrograms)
        {
            if (Prototype(program)?.ID == prototype)
                return true;
        }

        return false;
    }

    private void UpdateUserInterfaceState(EntityUid loaderUid, CartridgeLoaderComponent loader)
    {
        UpdateUiState(loaderUid, new CartridgeLoaderUiState(), null, loader);
    }

    private void UpdateCartridgeInstallationStatus(EntityUid cartridgeUid, InstallationStatus installationStatus)
    {
        CartridgeComponent? cartridgeComponent = default!;
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
