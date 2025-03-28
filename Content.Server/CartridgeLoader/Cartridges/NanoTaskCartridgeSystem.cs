using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Paper;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Content.Server.DeviceNetwork;
using Content.Shared.NanoTask;
using Content.Server.NanoTask.Components;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Station.Systems;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonDeviceNetServer = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private Dictionary<EntityUid, EntityUid> _loaderMap = [];

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
        if (station.HasValue && _singletonDeviceNetServer.TryGetActiveServerAddress<NanoTaskServerComponent>(station.Value, out var addr))
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

        var device = Comp<DeviceNetworkComponent>(ent);

        switch (message.Payload)
        {
            case NanoTaskAddTask task:
                if (!task.Item.Validate())
                    return;

                var newPayload = new NetworkPayload
                {
                    [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdSetState,
                    [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_NEW_TASK,
                    [NanoTaskConstants.NET_CATEGORY_TASK] = task.Category.Category,
                    [NanoTaskConstants.NET_TASK_DESCRIPTION] = task.Item.Description,
                    [NanoTaskConstants.NET_TASK_REQUESTER] = task.Item.TaskIsFor,
                    [NanoTaskConstants.NET_TASK_PRIORITY] = task.Item.Priority,
                    [NanoTaskConstants.NET_TASK_STATUS] = task.Item.Status,
                };

                if (task.Category.Department is { } department)
                    newPayload[NanoTaskConstants.NET_DEPARTAMENT_TASK] = department;

                _deviceNetwork.QueuePacket(ent, address, newPayload, device.TransmitFrequency, device.DeviceNetId, device);

                break;
            case NanoTaskUpdateTask task:
                {
                    if (!task.Item.Data.Validate())
                        return;

                    var updatePayload = new NetworkPayload
                    {
                        [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdSetState,
                        [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_UPDATE_TASK,
                        [NanoTaskConstants.NET_TASK_ID] = task.Item.Id,
                        [NanoTaskConstants.NET_TASK_DESCRIPTION] = task.Item.Data.Description,
                        [NanoTaskConstants.NET_TASK_REQUESTER] = task.Item.Data.TaskIsFor,
                        [NanoTaskConstants.NET_TASK_PRIORITY] = task.Item.Data.Priority,
                        [NanoTaskConstants.NET_TASK_STATUS] = task.Item.Data.Status,
                    };

                    _deviceNetwork.QueuePacket(ent, address, updatePayload, device.TransmitFrequency, device.DeviceNetId, device);

                    break;
                }
            case NanoTaskDeleteTask task:
                var deletePayload = new NetworkPayload
                {
                    [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdSetState,
                    [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_DELETE_TASK,
                    [NanoTaskConstants.NET_TASK_ID] = task.Id,
                };

                _deviceNetwork.QueuePacket(ent, address, deletePayload, device.TransmitFrequency, device.DeviceNetId, device);

                break;
        }

        var loader = GetEntity(args.LoaderUid);
        _loaderMap.TryAdd(ent, loader);

        UpdateUiState(ent, loader, args.Actor);
    }

    private void OnDeviceNetworkPacket(Entity<NanoTaskCartridgeComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? com)
            || com != DeviceNetworkConstants.CmdSetState)
            return;

        if (args.Data.TryGetValue(NanoTaskConstants.NET_ALL_TASKS, out (List<NanoTaskItemAndId> Station, Dictionary<string, List<NanoTaskItemAndId>> Departament) tasks))
        {
            ent.Comp.StationTasks = tasks.Station;
            ent.Comp.DepartmentTasks = tasks.Departament;

            return;
        }

        if (!args.Data.TryGetValue(NanoTaskConstants.NET_COMMAND, out string? netCommand)
            || !args.Data.TryGetValue(NanoTaskConstants.NET_CATEGORY_TASK, out string? category)
            || !args.Data.TryGetValue(NanoTaskConstants.NET_TASK_ID, out uint taskId))
            return;

        HandleMessage(ent.Comp, category, netCommand, args.Data, taskId);

        if (_loaderMap.TryGetValue(ent, out var loader))
        {
            UpdateUiState(ent, loader);
            _loaderMap.Remove(ent);
        }
    }

    private void HandleMessage(NanoTaskCartridgeComponent comp, string category, string netCommand, NetworkPayload data, uint taskId)
    {
        if (category == NanoTaskConstants.NET_CATEGORY_STATION_TASK)
        {
            switch (netCommand)
            {
                case NanoTaskConstants.NET_NEW_TASK:
                    comp.StationTasks.Add(ShapeNanoTaskItemAndId(data));
                    break;
                case NanoTaskConstants.NET_UPDATE_TASK:
                    var index = comp.StationTasks.FindIndex(x => x.Id == taskId);
                    if (index == -1)
                        return;
                    else
                        comp.StationTasks[index] = ShapeNanoTaskItemAndId(data);
                    break;
                case NanoTaskConstants.NET_DELETE_TASK:
                    comp.StationTasks.RemoveAll(x => x.Id == taskId);
                    break;
                default:
                    throw new UnreachableException();
            }
        }
        else if (category == NanoTaskConstants.NET_CATEGORY_DEPARTAMENT_TASK)
        {
            if (!data.TryGetValue(NanoTaskConstants.NET_DEPARTAMENT_TASK, out string? departament))
                return;

            switch (netCommand)
            {
                case NanoTaskConstants.NET_NEW_TASK:
                    comp.DepartmentTasks.GetOrNew(departament).Add(ShapeNanoTaskItemAndId(data));
                    break;
                case NanoTaskConstants.NET_UPDATE_TASK:
                    var depart = comp.DepartmentTasks.GetOrNew(departament);
                    var index = depart.FindIndex(x => x.Id == taskId);
                    if (index == -1)
                        return;
                    else
                        depart[index] = ShapeNanoTaskItemAndId(data);
                    break;
                case NanoTaskConstants.NET_DELETE_TASK:
                    comp.DepartmentTasks.GetOrNew(departament).RemoveAll(x => x.Id == taskId);
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    private static NanoTaskItemAndId ShapeNanoTaskItemAndId(NetworkPayload payload)
    {
        if (!payload.TryGetValue(NanoTaskConstants.NET_TASK_ID, out uint taskId)
            || !payload.TryGetValue(NanoTaskConstants.NET_TASK_DESCRIPTION, out string? description)
            || !payload.TryGetValue(NanoTaskConstants.NET_TASK_REQUESTER, out string? requester)
            || !payload.TryGetValue(NanoTaskConstants.NET_TASK_PRIORITY, out NanoTaskPriority priority)
            || !payload.TryGetValue(NanoTaskConstants.NET_TASK_STATUS, out NanoTaskItemStatus status))
            throw new ArgumentNullException();

        return new(taskId, new(description, requester, status, priority));
    }

    private void OnConnect(Entity<NanoTaskCartridgeComponent> ent, ref DeviceNetServerConnectedEvent args)
    {
        if (!CheckServerAvailable(ent.Owner, out var address))
        {
            UpdateUiStateNoServers(ent.Owner);
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
        ent.Comp.StationTasks.Clear();
        ent.Comp.DepartmentTasks.Clear();
    }

    private void UpdateUiStateNoServers(EntityUid loaderUid)
    {
        var state = new NanoTaskServerOfflineUiState();
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    private void UpdateUiState(Entity<NanoTaskCartridgeComponent> ent, EntityUid loaderUid, EntityUid? actor = null)
    {
        var query = EntityQueryEnumerator<NanoTaskServerComponent, SingletonDeviceNetServerComponent>();
        while (query.MoveNext(out var uid, out var server, out var singletonServer))
        {
            if (!_singletonDeviceNetServer.IsActiveServer(uid, singletonServer))
                continue;

            if (actor.HasValue)
            {
                var departments = _prototypeManager.EnumeratePrototypes<NanoTaskDepartmentPrototype>();

                if (ent.Comp.DepartmentTasks.Count == 0)
                    foreach (var department in departments)
                        if (_accessReader.IsAllowed(actor.Value, department))
                            ent.Comp.DepartmentTasks.Add(department.Name, []);

                var departmentTasks = ent.Comp.DepartmentTasks.Where(task => departments.Any(department => department.Name == task.Key && _accessReader.IsAllowed(actor.Value, department))).ToDictionary();

                var filteredState = new NanoTaskUiState(departmentTasks, ent.Comp.StationTasks);
                _cartridgeLoader.UpdateCartridgeUiState(loaderUid, filteredState);

                return;
            }

            var state = new NanoTaskUiState(ent.Comp.DepartmentTasks, ent.Comp.StationTasks);
            _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
        }
    }
}
