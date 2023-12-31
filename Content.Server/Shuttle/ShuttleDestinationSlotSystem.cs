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

        SubscribeLocalEvent<ShuttleDestinationSlotComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ShuttleDestinationSlotComponent, ComponentShutdown>(OnRemove);
        SubscribeLocalEvent<ShuttleDestinationSlotComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<ShuttleDestinationSlotComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
    }

    private void OnInit(EntityUid uid, ShuttleDestinationSlotComponent component, MapInitEvent args)
    {
        _itemSlots.AddItemSlot(uid, ShuttleDestinationSlotComponent.DiskSlotId, component.DiskSlot);
    }


    private void OnRemove(EntityUid uid, ShuttleDestinationSlotComponent component, ComponentShutdown args)
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
            if (component.DiskSlot.Item is { Valid: true } disk)
            {
                EntityUid? diskCoords = GetDiskDestination(disk);

                if (diskCoords != null)
                {

                    // Emergency Pod Disk Consoles

                    if (EntityManager.TryGetComponent(uid, out TransformComponent? xform) && EntityManager.TryGetComponent(xform.GridUid, out EscapePodComponent? escapePodComponent))
                    {
                        escapePodComponent.Destination = diskCoords.Value;
                        return;
                    }

                    // Shuttle Consoles

                    if (!EnsureComp(diskCoords.Value, out FTLDestinationComponent destination))
                    {
                        destination.Enabled = false;
                    }

                    EnableDestination(uid, destination);
                    _console.RefreshShuttleConsoles();
                }
            }
        } else
        {
            if (args.Entity is { Valid: true } disk)
            {

                EntityUid? diskCoords = GetDiskDestination(disk);

                if (diskCoords != null)
                {

                    // Emergency Pod Disk Consoles

                    if (EntityManager.TryGetComponent(uid, out TransformComponent? xform) && EntityManager.TryGetComponent(xform.GridUid, out EscapePodComponent? escapePodComponent))
                    {
                        escapePodComponent.Destination = null;
                        return;
                    }

                    // Shuttle Consoles

                    EnsureComp(diskCoords.Value, out FTLDestinationComponent destination);
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

        // Drone consoles adds the shuttle's console uid, to allow control from both consoles

        if (TryComp(uid, out DroneConsoleComponent? consoleId))
        {
            _console.RefreshDroneConsoles();

            if (consoleId != null && consoleId.Entity != null)
            {
                destination.WhitelistSpecific.Add(consoleId.Entity.Value);
                return;
            }
        }

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
                    destination.WhitelistSpecific.Remove(consoleId.Entity.Value);
                    return;
                }
            }

            destination.WhitelistSpecific.Remove(uid);
        }
    }
}


