using System.Diagnostics;
using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NanoTask.Components;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.DeviceNetwork;
using Content.Shared.NanoTask;
using Content.Shared.NanoTask.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.NanoTask.Systems;

public sealed class NanoTaskServerSystem : EntitySystem
{
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NanoTaskServerComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<NanoTaskServerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<NanoTaskServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnComponentInit(Entity<NanoTaskServerComponent> ent, ref ComponentInit args)
    {
        var departmentProtos = _prototypeManager.EnumeratePrototypes<NanoTaskDepartmentPrototype>();
        foreach (var department in departmentProtos)
            ent.Comp.DepartamentTasks.Add(department.Name, []);
    }

    private void OnPacketReceived(Entity<NanoTaskServerComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? _)
            || !args.Data.TryGetValue(NanoTaskConstants.NET_COMMAND, out string? cmd))
            return;

        switch (cmd)
        {
            case NanoTaskConstants.NET_NEW_TASK:
                HandleNewTask(ent, args);
                return;
            case NanoTaskConstants.NET_UPDATE_TASK:
                HandleUpdateTask(ent, args);
                return;
            case NanoTaskConstants.NET_DELETE_TASK:
                HandleDeleteTask(ent, args);
                return;
            case NanoTaskConstants.NET_ALL_TASKS:
                HandleAllTasks(ent, args);
                return;
            default:
                throw new UnreachableException();
        }
    }

    private void HandleAllTasks(Entity<NanoTaskServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        var payload = new NetworkPayload
        {
            [NanoTaskConstants.NET_ALL_TASKS] = (ent.Comp.StationTasks, ent.Comp.DepartamentTasks.ToDictionary(x => x.Key, x => x.Value))
        };

        var device = Comp<DeviceNetworkComponent>(ent);
        _deviceNetworkSystem.QueuePacket(ent, args.Address, payload, device: device);
    }

    private void HandleDeleteTask(Entity<NanoTaskServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_CATEGORY_TASK, out string? category))
            return;

        if (category == NanoTaskConstants.NET_CATEGORY_STATION_TASK)
        {
            var itemId = GetNanoTaskItemId(args.Data);
            ent.Comp.StationTasks.RemoveAll(x => x.Id == itemId);
        }
        else if (category == NanoTaskConstants.NET_CATEGORY_DEPARTAMENT_TASK
                && args.Data.TryGetValue(NanoTaskConstants.NET_DEPARTAMENT_TASK, out string? departamentName))
        {
            var departament = _prototypeManager
                .EnumeratePrototypes<NanoTaskDepartmentPrototype>()
                .First(x => x.Name == departamentName);

            var itemId = GetNanoTaskItemId(args.Data);
            departament.Tasks.RemoveAll(x => x.Id == itemId);
        }
    }

    private void HandleUpdateTask(Entity<NanoTaskServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_CATEGORY_TASK, out string? category))
            return;

        if (category == NanoTaskConstants.NET_CATEGORY_STATION_TASK)
        {
            var id = GetNanoTaskItemId(args.Data);
            var index = ent.Comp.StationTasks.FindIndex(x => x.Id == id);
            if (index == -1)
                return;

            ent.Comp.StationTasks[index] = new(id, GetNanoTaskItem(args.Data));
        }
        else if (category == NanoTaskConstants.NET_CATEGORY_DEPARTAMENT_TASK
                && args.Data.TryGetValue(NanoTaskConstants.NET_DEPARTAMENT_TASK, out string? departamentName))
        {
            var departament = _prototypeManager
                .EnumeratePrototypes<NanoTaskDepartmentPrototype>()
                .First(x => x.Name == departamentName);

            var itemId = GetNanoTaskItemId(args.Data);
            var itemIndex = departament.Tasks.FindIndex(x => x.Id == itemId);
            if (itemIndex == -1)
                return;

            departament.Tasks[itemIndex] = new(itemId, GetNanoTaskItem(args.Data));
        }
    }

    private void HandleNewTask(Entity<NanoTaskServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_CATEGORY_TASK, out string? category))
            return;

        var id = ent.Comp.Counter++;
        if (category == NanoTaskConstants.NET_CATEGORY_STATION_TASK)
        {
            var item = GetNanoTaskItem(args.Data);
            ent.Comp.StationTasks.Add(new(ent.Comp.Counter++, item));

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdSetState,
                [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_NEW_TASK,
                [NanoTaskConstants.NET_TASK_ID] = id,
                [NanoTaskConstants.NET_CATEGORY_TASK] = NanoTaskConstants.NET_CATEGORY_STATION_TASK,
                [NanoTaskConstants.NET_TASK_DESCRIPTION] = item.Description,
                [NanoTaskConstants.NET_TASK_REQUESTER] = item.TaskIsFor,
                [NanoTaskConstants.NET_TASK_PRIORITY] = item.Priority,
                [NanoTaskConstants.NET_TASK_STATUS] = item.Status,
            };

            var device = Comp<DeviceNetworkComponent>(ent);
            _deviceNetworkSystem.QueuePacket(ent, null, payload, device: device);
        }
        else if (category == NanoTaskConstants.NET_CATEGORY_DEPARTAMENT_TASK
                && args.Data.TryGetValue(NanoTaskConstants.NET_DEPARTAMENT_TASK, out string? departamentName))
        {
            var departament = _prototypeManager
                .EnumeratePrototypes<NanoTaskDepartmentPrototype>()
                .First(x => x.Name == departamentName);

            var item = GetNanoTaskItem(args.Data);
            departament.Tasks.Add(new(ent.Comp.Counter++, item));

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdSetState,
                [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_NEW_TASK,
                [NanoTaskConstants.NET_TASK_ID] = id,
                [NanoTaskConstants.NET_CATEGORY_TASK] = NanoTaskConstants.NET_CATEGORY_DEPARTAMENT_TASK,
                [NanoTaskConstants.NET_DEPARTAMENT_TASK] = departamentName,
                [NanoTaskConstants.NET_TASK_DESCRIPTION] = item.Description,
                [NanoTaskConstants.NET_TASK_REQUESTER] = item.TaskIsFor,
                [NanoTaskConstants.NET_TASK_PRIORITY] = item.Priority,
                [NanoTaskConstants.NET_TASK_STATUS] = item.Status,
            };

            var device = Comp<DeviceNetworkComponent>(ent);
            _deviceNetworkSystem.QueuePacket(ent, null, payload, device: device);
        }
    }

    private static uint GetNanoTaskItemId(NetworkPayload payload)
    {
        if (!payload.TryGetValue(NanoTaskConstants.NET_TASK_ID, out uint id))
            throw new ArgumentNullException();

        return id;
    }

    private static NanoTaskItem GetNanoTaskItem(NetworkPayload payload)
    {
        if (!payload.TryGetValue(NanoTaskConstants.NET_TASK_DESCRIPTION, out string? description)
            || !payload.TryGetValue(NanoTaskConstants.NET_TASK_REQUESTER, out string? requester)
            || !payload.TryGetValue(NanoTaskConstants.NET_TASK_PRIORITY, out NanoTaskPriority priority)
            || !payload.TryGetValue(NanoTaskConstants.NET_TASK_STATUS, out NanoTaskItemStatus status))
            throw new ArgumentNullException();

        return new(description, requester, status, priority);
    }

    private void OnRemove(Entity<NanoTaskServerComponent> ent, ref ComponentRemove args)
    {
        ent.Comp.DepartamentTasks.Clear();
        ent.Comp.StationTasks.Clear();
    }
}
