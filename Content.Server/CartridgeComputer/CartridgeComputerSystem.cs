using System.Collections.Immutable;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.UserInterface;
using Content.Shared.CartridgeComputer;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.CartridgeComputer;

public sealed class CartridgeComputerSystem : SharedCartridgeComputerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartridgeComputerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CartridgeComputerComponent, AfterInteractEvent>(OnUsed);
    }

    public void UpdateUIState<T>(EntityUid computerUid, CartridgeComputerUIState state, CartridgeComputerComponent? computer  = default!) where T : CartridgeComputerUIState
    {
        if (!Resolve(computerUid, ref computer))
            return;

        state.ActiveUI = computer.ActiveProgram;
        state.InstalledPrograms = computer.InstalledPrograms;
        computer.Owner.GetUIOrNull(CartridgeComputerUiKey.Key)?.SetState(state);
    }

    public IReadOnlyList<EntityUid> GetAvailablePrograms(EntityUid uid, CartridgeComputerComponent? computer  = default!)
    {
        if (!Resolve(uid, ref computer))
            return ImmutableList<EntityUid>.Empty;

        var available = new List<EntityUid>();
        available.AddRange(computer.InstalledPrograms);

        if (computer.CartridgeSlot.HasItem)
            available.Add(computer.CartridgeSlot.Item!.Value);

        return available.ToImmutableList();
    }

    public bool InstallCartridge(EntityUid computerUid, EntityUid cartridgeUid, CartridgeComputerComponent? computer = default!)
    {
        if (!Resolve(computerUid, ref computer) || computer.InstalledPrograms.Count >= computer.DiskSpace)
            return false;

        //This will eventually be replaced by serializing and deserializing the cartridge to copy it when something needs
        //the data on the cartridge to carry over when installing
        var prototypeId = Prototype(cartridgeUid)?.ID;
        return prototypeId != null && InstallProgram(computerUid, prototypeId, computer);
    }

    public bool InstallProgram(EntityUid computerUid, string prototype, CartridgeComputerComponent? computer  = default!)
    {
        if (!Resolve(computerUid, ref computer) || computer.InstalledPrograms.Count >= computer.DiskSpace)
            return false;

        var installedProgram = Spawn(prototype, new EntityCoordinates(computerUid, 0, 0));
        RaiseLocalEvent(installedProgram, new CartridgeAddedEvent(computerUid));

        computer.InstalledPrograms.Add(installedProgram);
        return true;
    }

    public void ActivateProgram(EntityUid computerUid, EntityUid cartridgeUid, CartridgeComputerComponent? computer  = default!)
    {
        if (!Resolve(computerUid, ref computer))
            return;

        if (!ContainsCartridge(computerUid, cartridgeUid, computer))
            return;

        if (computer.ActiveProgram.HasValue)
            DeactivateProgram(computerUid, cartridgeUid, computer);

        if (!computer.BackgroundPrograms.Contains(cartridgeUid))
            RaiseLocalEvent(cartridgeUid, new CartridgeActivatedEvent(computerUid));

        computer.ActiveProgram = cartridgeUid;
    }

    public void DeactivateProgram(EntityUid computerUid, EntityUid cartridgeUid, CartridgeComputerComponent? computer  = default!)
    {
        if (!Resolve(computerUid, ref computer))
            return;

        if (!ContainsCartridge(computerUid, cartridgeUid, computer) || computer.ActiveProgram != cartridgeUid)
            return;

        if (!computer.BackgroundPrograms.Contains(cartridgeUid))
            RaiseLocalEvent(cartridgeUid, new CartridgeDeactivatedEvent(cartridgeUid));

        computer.ActiveProgram = default;
    }

    public void RegisterBackgroundProgram(EntityUid computerUid, EntityUid cartridgeUid, CartridgeComputerComponent? computer  = default!)
    {
        if (!Resolve(computerUid, ref computer))
            return;

        if (!ContainsCartridge(computerUid, cartridgeUid, computer))
            return;

        if (computer.ActiveProgram != cartridgeUid)
            RaiseLocalEvent(cartridgeUid, new CartridgeActivatedEvent(computerUid));

        computer.BackgroundPrograms.Add(cartridgeUid);
    }

    public void UnregisterBackgroundProgram(EntityUid computerUid, EntityUid cartridgeUid, CartridgeComputerComponent? computer  = default!)
    {
        if (!Resolve(computerUid, ref computer))
            return;

        if (!ContainsCartridge(computerUid, cartridgeUid, computer))
            return;

        if (computer.ActiveProgram != cartridgeUid)
            RaiseLocalEvent(cartridgeUid, new CartridgeDeactivatedEvent(computerUid));

        computer.BackgroundPrograms.Remove(cartridgeUid);
    }

    protected override void OnItemRemoved(EntityUid uid, SharedCartridgeComputerComponent computer, EntRemovedFromContainerMessage args)
    {
        var deactivate = computer.BackgroundPrograms.Remove(args.Entity);

        if (computer.ActiveProgram == args.Entity)
        {
            computer.ActiveProgram = default;
            deactivate = true;
        }

        if (deactivate)
            RaiseLocalEvent(args.Entity, new CartridgeDeactivatedEvent(uid));

        base.OnItemRemoved(uid, computer, args);
    }

    private void OnUsed(EntityUid uid, CartridgeComputerComponent component, AfterInteractEvent args)
    {
        RelayEvent(component, new CartridgeAfterInteractEvent(uid, args));
    }

    private void OnPacketReceived(EntityUid uid, CartridgeComputerComponent component, DeviceNetworkPacketEvent args)
    {
        RelayEvent(component, new CartridgeDeviceNetPacketEvent(uid, args));
    }

    private void RelayEvent<TEvent>(CartridgeComputerComponent computer, TEvent args) where TEvent : notnull
    {
        if (computer.ActiveProgram.HasValue)
            RaiseLocalEvent(computer.ActiveProgram.Value, args);

        foreach (var program in computer.BackgroundPrograms)
        {
            RaiseLocalEvent(program, args);
        }
    }

    private bool ContainsCartridge(EntityUid computerUid, EntityUid cartridgeUid, CartridgeComputerComponent computer)
    {
        return computer.CartridgeSlot.Item?.Equals(cartridgeUid) == true || computer.InstalledPrograms.Contains(cartridgeUid);
    }
}

public sealed class CartridgeDeviceNetPacketEvent : EntityEventArgs
{
    public readonly EntityUid Computer;
    public readonly DeviceNetworkPacketEvent PacketEvent;

    public CartridgeDeviceNetPacketEvent(EntityUid computer, DeviceNetworkPacketEvent packetEvent)
    {
        Computer = computer;
        PacketEvent = packetEvent;
    }
}

public sealed class CartridgeAfterInteractEvent : EntityEventArgs
{
    public readonly EntityUid Computer;
    public readonly AfterInteractEvent InteractEvent;

    public CartridgeAfterInteractEvent(EntityUid computer, AfterInteractEvent interactEvent)
    {
        Computer = computer;
        InteractEvent = interactEvent;
    }
}
