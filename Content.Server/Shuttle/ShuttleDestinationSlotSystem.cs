using Content.Shared.Containers.ItemSlots;
using Content.Server.Shuttle.Components;
using Content.Server.Shuttles;
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
using Content.Shared.Shuttles.Components;

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
        _itemSlots.AddItemSlot(uid, component.DiskSlotId, component.DiskSlot);
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
                ShuttleDestinationCoordinatesComponent? diskCoordinates = null;
                if (!Resolve(disk, ref diskCoordinates))
                {
                    return;
                }

                EntityUid? diskCoords = diskCoordinates.Destination;

                if (diskCoords != null)
                {
                    if (HasComp<FTLDestinationComponent>(diskCoords.Value))
                    {
                        EnableDestination(uid, diskCoords.Value);
                        _console.RefreshShuttleConsoles();
                    }
                }
            }
        } else
        {
            if (args.Entity is { Valid: true } disk)
            {
                ShuttleDestinationCoordinatesComponent? diskCoordinates = null;
                if (!Resolve(disk, ref diskCoordinates))
                {
                    return;
                }

                EntityUid? diskCoords = diskCoordinates.Destination;

                if (diskCoords != null)
                {
                    if (HasComp<FTLDestinationComponent>(diskCoords.Value))
                    {
                        DisableDestination(uid, diskCoords.Value);
                        _console.RefreshShuttleConsoles();
                    }
                }
            }
        }
    }

    private void EnableDestination(EntityUid uid, EntityUid destination)
    {
        // Drone consoles adds the destination to the shuttle's console component, to allow control from both consoles
        if (TryComp(uid, out DroneConsoleComponent? consoleId))
        {
            _console.RefreshDroneConsoles();

            if (consoleId.Entity != null)
            {
                if (TryComp(consoleId.Entity.Value, out ShuttleConsoleComponent? remoteConsoleComp))
                {
                    remoteConsoleComp.FTLWhitelist.Add(destination);
                    return;
                }
            }
        }

        if (TryComp(uid, out ShuttleConsoleComponent? consoleComp))
        {
            consoleComp.FTLWhitelist.Add(destination);
        }
    }

    private void DisableDestination(EntityUid uid, EntityUid destination)
    {
        if (TryComp(uid, out DroneConsoleComponent? consoleId))
        {
            _console.RefreshDroneConsoles();

            if (consoleId.Entity != null)
            {
                if (TryComp(consoleId.Entity.Value, out ShuttleConsoleComponent? remoteConsoleComp))
                {
                    remoteConsoleComp.FTLWhitelist.Remove(destination);
                    return;
                }
            }
        }

        if (TryComp(uid, out ShuttleConsoleComponent? consoleComp))
        {
            consoleComp.FTLWhitelist.Remove(destination);
        }
    }
}


