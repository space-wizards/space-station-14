using Content.Shared.Containers.ItemSlots;
using Content.Server.Shuttle.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Server.Station.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;

namespace Content.Server.Shuttle;

public sealed class ShuttleDestinationSlotSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShuttleDestinationSlotComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShuttleDestinationSlotComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ShuttleDestinationSlotComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<ShuttleDestinationSlotComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
    }

    private void OnInit(EntityUid uid, ShuttleDestinationSlotComponent component, ComponentInit args)
    {
        _itemSlots.AddItemSlot(uid, ShuttleDestinationSlotComponent.DiskSlotId, component.DiskSlot);
    }


    private void OnRemove(EntityUid uid, ShuttleDestinationSlotComponent component, ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(uid, component.DiskSlot);
    }

    private void OnItemSlotChanged(EntityUid uid, ShuttleDestinationSlotComponent component, ContainerModifiedMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.DiskSlot.ID)
            return;

        if (component.DiskSlot.HasItem)
        {
            Console.WriteLine("Item HATH");
            if (component.DiskSlot.Item is { Valid: true } disk)
            {
                EntityUid? diskCoords = GetDiskDestination(disk);

                if (diskCoords != null)
                {
                    FTLDestinationComponent destination;

                    if (!EnsureComp(diskCoords.Value, out destination))
                    {
                        destination.Enabled = false;
                    }

                    EnableDestination(uid, destination);
                    _console.RefreshShuttleConsoles();
                }
            }
        } else
        {
            Console.WriteLine("Item HATHNT");
            if (args.Entity is { Valid: true } disk)
            {
                EntityUid? diskCoords = GetDiskDestination(disk);

                if (diskCoords != null)
                {
                    FTLDestinationComponent destination = EnsureComp<FTLDestinationComponent>(diskCoords.Value);
                    DisableDestination(uid, destination);
                    _console.RefreshShuttleConsoles();
                }
            }
        }
    }
    private EntityUid? GetDiskDestination(EntityUid disk)
    {
        ShuttleDestinationCoordinatesComponent? diskCoordinates = null;

        if (!Resolve(disk, ref diskCoordinates))
        {
            return null;
        }

        return diskCoordinates.GetDestinationEntityUid();
    }

    private void EnableDestination(EntityUid uid, FTLDestinationComponent destination)
    {
        if (destination.WhitelistSpecific == null)
        {
            destination.WhitelistSpecific = new List<EntityUid>();
        }

        if (TryComp(uid, out DroneConsoleComponent? consoleId))
        {
            _console.RefreshDroneConsoles();

            if (consoleId != null && consoleId.Entity != null)
            {
                Console.WriteLine("WEH! ADDED DRONE!");
                destination.WhitelistSpecific.Add(consoleId.Entity.Value);
                return;
            }
        }

        Console.WriteLine("WEH! ADDED!");
        destination.WhitelistSpecific.Add(uid);

    }

    private void DisableDestination(EntityUid uid, FTLDestinationComponent destination)
    {
        if (destination.WhitelistSpecific != null)
        {
            if (TryComp(uid, out DroneConsoleComponent? consoleId))
            {
                _console.RefreshDroneConsoles();

                if (consoleId != null && consoleId.Entity != null)
                {
                    Console.WriteLine("WEH! REMOVED DRONE!");
                    destination.WhitelistSpecific.Remove(consoleId.Entity.Value);
                    return;
                }
            }

            Console.WriteLine("WEH! REMOVED!");
            destination.WhitelistSpecific.Remove(uid);

        }
    }
}


