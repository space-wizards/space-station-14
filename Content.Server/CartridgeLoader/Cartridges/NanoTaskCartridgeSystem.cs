using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Content.Server.DeviceNetwork;
using Content.Shared.NanoTask;
using Content.Server.NanoTask.Components;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Station.Systems;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;
using Content.Shared.NanoTask.Prototypes;
using System.Linq;
using Content.Shared.Access.Systems;

namespace Content.Server.CartridgeLoader.Cartridges;

/// <summary>
///     Server-side class implementing the core UI logic of NanoTask
/// </summary>
public sealed class NanoTaskCartridgeSystem : SharedNanoTaskCartridgeSystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonDeviceNetServer = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NanoTaskCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<NanoTaskCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);

        SubscribeLocalEvent<NanoTaskCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);

        SubscribeLocalEvent<NanoTaskCartridgeComponent, DeviceNetworkPacketEvent>(OnDeviceNetworkPacket);
        SubscribeLocalEvent<NanoTaskCartridgeComponent, DeviceNetServerDisconnectedEvent>(OnDisconnect);
        SubscribeLocalEvent<NanoTaskCartridgeComponent, DeviceNetServerConnectedEvent>(OnConnect);
    }

    private void OnCartridgeRemoved(Entity<NanoTaskCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        if (!_cartridgeLoader.HasProgram<NanoTaskCartridgeComponent>(args.Loader))
        {
            RemComp<NanoTaskInteractionComponent>(args.Loader);
        }
    }

    private bool CheckServerAvailable(EntityUid ent, [NotNullWhen(true)] out string? address)
    {
        var station = _station.GetOwningStation(ent);
        if (station.HasValue &&
            _singletonDeviceNetServer.TryGetActiveServerAddress<NanoTaskServerComponent>(station.Value, out var addr))
        {
            address = addr;
            return true;
        }

        address = null;
        return false;
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(Entity<NanoTaskCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        if (!CheckServerAvailable(ent.Owner, out var address))
        {
            UpdateUiStateNoServers(args.Loader);
            return;
        }

        UpdateUiState(ent, args.Loader, args.Actor);
    }

    /// <summary>
    /// The ui messages received here get wrapped by a CartridgeMessageEvent and are relayed from the <see cref="CartridgeLoaderSystem"/>
    /// </summary>
    /// <remarks>
    /// The cartridge specific ui message event needs to inherit from the CartridgeMessageEvent
    /// </remarks>
    private void OnUiMessage(Entity<NanoTaskCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not NanoTaskUiMessageEvent message)
            return;

        if (!CheckServerAvailable(ent.Owner, out var address))
        {
            UpdateUiStateNoServers(GetEntity(args.LoaderUid));
            return;
        }

        var isAllowed = message.Payload switch
        {
            NanoTaskAddTask task => task.Category.Department,
            NanoTaskUpdateTask task => task.Item.Category.Department,
            NanoTaskDeleteTask task => ent.Comp.Tasks.FirstOrDefault(x => x.Item.Id == task.Id)?.Category.Department,
            _ => null
        } is { } department && _accessReader.IsAllowed(args.Actor, _prototypeManager.Index(department));

        if (!isAllowed)
            return;

        var device = Comp<DeviceNetworkComponent>(ent);

        switch (message.Payload)
        {
            case NanoTaskAddTask task:
                if (!task.Item.Validate())
                    return;

                var newPayload = new NetworkPayload
                {
                    [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_NEW_TASK,
                    [NanoTaskConstants.NET_NEW_ITEM_TASK] = task.Item,
                    [NanoTaskConstants.NET_ITEM_TASK_CATEGORY] = task.Category,
                };

                _deviceNetwork.QueuePacket(ent,
                    address,
                    newPayload,
                    device.TransmitFrequency,
                    device.DeviceNetId,
                    device);

                break;
            case NanoTaskUpdateTask task:
                {
                    if (!task.Item.Item.Data.Validate())
                        return;

                    var updatePayload = new NetworkPayload
                    {
                        [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_UPDATE_TASK,
                        [NanoTaskConstants.NET_ITEM_TASK] =
                            new NanoTaskItemAndDepartment(task.Item.Item, task.Item.Category),
                    };

                    _deviceNetwork.QueuePacket(ent,
                        address,
                        updatePayload,
                        device.TransmitFrequency,
                        device.DeviceNetId,
                        device);

                    break;
                }
            case NanoTaskDeleteTask task:
                var deletePayload = new NetworkPayload
                {
                    [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_DELETE_TASK,
                    [NanoTaskConstants.NET_ITEM_TASK_ID] = task.Id,
                };

                _deviceNetwork.QueuePacket(ent,
                    address,
                    deletePayload,
                    device.TransmitFrequency,
                    device.DeviceNetId,
                    device);

                break;
        }

        ent.Comp.ActorUid = args.Actor;

        var loader = GetEntity(args.LoaderUid);
        UpdateUiState(ent, loader, args.Actor);
    }

    private void OnDeviceNetworkPacket(Entity<NanoTaskCartridgeComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_COMMAND, out string? command))
            return;

        switch (command)
        {
            case NanoTaskConstants.NET_ALL_TASKS:
                HandleAllTasks(ent, args);
                break;
            case NanoTaskConstants.NET_NEW_TASK:
                HandleNewTask(ent, args);
                break;
            case NanoTaskConstants.NET_UPDATE_TASK:
                HandleUpdateTask(ent, args);
                break;
            case NanoTaskConstants.NET_DELETE_TASK:
                HandleDeleteTask(ent, args);
                break;
        }

        if (Comp<CartridgeComponent>(ent).LoaderUid is not { } loaderUid)
            return;

        var description =
            (args.Data[NanoTaskConstants.NET_ITEM_TASK] as NanoTaskItemAndDepartment)?.Item.Data.Description ??
            (args.Data[NanoTaskConstants.NET_NEW_ITEM_TASK] as NanoTaskItem)?.Description;

        if (description is null)
            return;

        switch (command)
        {
            case NanoTaskConstants.NET_NEW_TASK:
                _cartridgeLoader.SendNotification(loaderUid,
                    Loc.GetString("nano-task-ui-new-task-title"),
                    description);
                break;
            case NanoTaskConstants.NET_DELETE_TASK:
                _cartridgeLoader.SendNotification(loaderUid,
                    Loc.GetString("nano-task-ui-task-completed-title"),
                    description);
                break;
        }

        var actor = ent.Comp.ActorUid;
        ent.Comp.ActorUid = null;

        UpdateUiState(ent, loaderUid, actor);
    }

    private static void HandleAllTasks(Entity<NanoTaskCartridgeComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_TASKS, out List<NanoTaskItemAndDepartment>? tasks))
            return;

        ent.Comp.Tasks = tasks;
    }

    private static void HandleDeleteTask(Entity<NanoTaskCartridgeComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_ITEM_TASK, out NanoTaskItemAndDepartment? item))
            return;

        var index = ent.Comp.Tasks.FindIndex(x => x.Item.Id == item.Item.Id);
        if (index == -1)
            return;

        ent.Comp.Tasks.RemoveAt(index);
    }

    private static void HandleUpdateTask(Entity<NanoTaskCartridgeComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_ITEM_TASK, out NanoTaskItemAndDepartment? item))
            return;

        var index = ent.Comp.Tasks.FindIndex(x => x.Item.Id == item.Item.Id);
        if (index == -1)
            return;

        ent.Comp.Tasks[index] = item;
    }

    private static void HandleNewTask(Entity<NanoTaskCartridgeComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_ITEM_TASK, out NanoTaskItemAndDepartment? item))
            return;

        ent.Comp.Tasks.Add(item);
    }

    private void OnConnect(Entity<NanoTaskCartridgeComponent> ent, ref DeviceNetServerConnectedEvent args)
    {
        var loaderUid = Comp<CartridgeComponent>(ent).LoaderUid;
        if (!loaderUid.HasValue)
            return;

        if (!CheckServerAvailable(ent, out var address))
        {
            UpdateUiStateNoServers(loaderUid.Value);
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdSetState,
            [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_ALL_TASKS,
        };

        var device = Comp<DeviceNetworkComponent>(ent);
        _deviceNetwork.QueuePacket(ent, address, payload, device.TransmitFrequency, device.DeviceNetId, device);
    }

    private void OnDisconnect(Entity<NanoTaskCartridgeComponent> ent, ref DeviceNetServerDisconnectedEvent args)
    {
        ent.Comp.Tasks.Clear();

        if (CheckServerAvailable(ent, out var address))
            return;

        var loaderUid = Comp<CartridgeComponent>(ent).LoaderUid;
        if (!loaderUid.HasValue)
            return;

        UpdateUiStateNoServers(loaderUid.Value);
    }

    private void UpdateUiStateNoServers(EntityUid loaderUid)
    {
        var state = new NanoTaskServerOfflineUiState();
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    private void UpdateUiState(Entity<NanoTaskCartridgeComponent> ent, EntityUid loaderUid, EntityUid? actor = null)
    {
        var tasks = ent.Comp.Tasks.Where(t =>
            {
                if (t.Category.Category is NanoTaskCategory.Station)
                    return true;

                if (!actor.HasValue)
                    return false;

                var proto = _prototypeManager.Index<NanoTaskDepartmentPrototype>(t.Category.Department!);
                var hasPermissions = _accessReader.IsAllowed(actor.Value, proto);

                return hasPermissions;
            })
            .ToList();

        var departments = actor.HasValue ? _prototypeManager.EnumeratePrototypes<NanoTaskDepartmentPrototype>()
            .Where(x => _accessReader.IsAllowed(actor.Value, x))
            .Select(x => new ProtoId<NanoTaskDepartmentPrototype>(x.ID))
            .ToList() : [];

        var filteredState = new NanoTaskUiState(tasks, departments);
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, filteredState);
    }
}
