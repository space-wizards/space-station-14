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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NanoTaskServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnPacketReceived(Entity<NanoTaskServerComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_COMMAND, out string? cmd))
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
        }
    }

    private void HandleAllTasks(Entity<NanoTaskServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        var payload = new NetworkPayload
        {
            [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_ALL_TASKS,
            [NanoTaskConstants.NET_TASKS] = ent.Comp.Tasks,
        };

        var device = Comp<DeviceNetworkComponent>(ent);
        _deviceNetworkSystem.QueuePacket(ent, args.Address, payload, device: device);
    }

    private void HandleDeleteTask(Entity<NanoTaskServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_ITEM_TASK, out NanoTaskItemAndDepartment? item))
            return;

        var index = ent.Comp.Tasks.FindIndex(x => x.Item.Id == item.Item.Id);
        ent.Comp.Tasks.RemoveAt(index);

        var payload = new NetworkPayload
        {
            [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_DELETE_TASK,
            [NanoTaskConstants.NET_ITEM_TASK] = item,
        };

        var device = Comp<DeviceNetworkComponent>(ent);
        _deviceNetworkSystem.QueuePacket(ent, null, payload, device: device);
    }

    private void HandleUpdateTask(Entity<NanoTaskServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_ITEM_TASK, out NanoTaskItemAndDepartment? item))
            return;

        if (item.Item.Data.Status is NanoTaskItemStatus.Completed)
        {
            HandleDeleteTask(ent, args);
            return;
        }

        var index = ent.Comp.Tasks.FindIndex(x => x.Item.Id == item.Item.Id);
        ent.Comp.Tasks[index] = item;

        var payload = new NetworkPayload
        {
            [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_UPDATE_TASK,
            [NanoTaskConstants.NET_ITEM_TASK] = item,
        };

        var device = Comp<DeviceNetworkComponent>(ent);
        _deviceNetworkSystem.QueuePacket(ent, null, payload, device: device);
    }

    private void HandleNewTask(Entity<NanoTaskServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(NanoTaskConstants.NET_NEW_ITEM_TASK, out NanoTaskItem? item)
            || !args.Data.TryGetValue(NanoTaskConstants.NET_ITEM_TASK_CATEGORY,
                out NanoTaskCategoryAndDepartment? category))
            return;

        var id = ent.Comp.Counter++;
        var newItem = new NanoTaskItemAndDepartment(new NanoTaskItemAndId(id, item), category);
        ent.Comp.Tasks.Add(newItem);

        var payload = new NetworkPayload
        {
            [NanoTaskConstants.NET_COMMAND] = NanoTaskConstants.NET_NEW_TASK,
            [NanoTaskConstants.NET_ITEM_TASK] = newItem,
        };

        var device = Comp<DeviceNetworkComponent>(ent);
        _deviceNetworkSystem.QueuePacket(ent, null, payload, device: device);
    }
}
