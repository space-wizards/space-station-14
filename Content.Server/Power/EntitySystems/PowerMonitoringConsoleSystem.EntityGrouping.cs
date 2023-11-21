using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Power.Components;
using Content.Shared.Pinpointer;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Power.EntitySystems;

internal sealed partial class PowerMonitoringConsoleSystem
{
    // Groups an entity based on its prototype
    private void AssignEntityToMasterGroup(EntityUid uid, PowerMonitoringDeviceComponent component)
    {
        if (!component.JoinAlikeEntities)
            return;

        var protoId = MetaData(uid).EntityPrototype?.ID;

        if (protoId == null)
            return;

        var entProtoId = (EntProtoId) protoId;

        if (!_groupableEntityCoords.ContainsKey(entProtoId))
            _groupableEntityCoords[entProtoId] = new();

        _groupableEntityCoords[entProtoId].Add(uid, Transform(uid).Coordinates);
    }

    // Remove an entity from consideration for master assignment
    private void RemoveEntityFromMasterGroup(EntityUid uid, PowerMonitoringDeviceComponent component)
    {
        if (!component.JoinAlikeEntities)
            return;

        _masterDevices.Remove(uid);

        component.MasterUid = new EntityUid();
        component.ChildEntities.Clear();

        if (TryComp<NavMapTrackableComponent>(uid, out var trackable))
        {
            trackable.ChildOffsets.Clear();
            Dirty(uid, trackable);
        }

        if (!TryGetEntProtoId(uid, out var entProtoId) || !_groupableEntityCoords.ContainsKey(entProtoId.Value))
            return;

        _groupableEntityCoords[entProtoId.Value].Remove(uid);
    }

    // Designates entities as 'masters' on a per prototype and per load network basis
    // Entities which share this prototype and sit on the same load network are assigned
    // to the master that represents this device for this network. In this way you
    // can have one device represent multiple identical, connected devices
    private void AssignMastersToEntities(EntProtoId entProtoId)
    {
        // Retrieve all devices of the specified prototype
        if (!_groupableEntityCoords.TryGetValue(entProtoId, out var devices) || !devices.Any())
            return;

        var currentMaster = devices.Last().Key;

        // Note: the first device found on a given network is dubbed its master
        foreach ((var ent, var coords) in devices)
        {
            if (!TryComp<PowerMonitoringDeviceComponent>(ent, out var device))
                continue;

            // Clear outdated component data
            device.ChildEntities.Clear();

            if (TryComp<NavMapTrackableComponent>(ent, out var trackable))
            {
                trackable.ChildOffsets.Clear();
                Dirty(ent, trackable);
            }

            // If the device is not attached to a network, continue on
            if (!TryComp<NodeContainerComponent>(ent, out var container) ||
                !container.Nodes.TryGetValue(device.LoadNode, out var loadNode) ||
                !loadNode.ReachableNodes.Any())
            {
                device.MasterUid = ent;
                _masterDevices.TryAdd(ent, device);

                continue;
            }

            // If the device has been assigned to the current master, continue on
            if (device.MasterUid == currentMaster)
                continue;

            // Dub this device an master
            currentMaster = ent;
            device.MasterUid = ent;
            _masterDevices.TryAdd(ent, device);

            // Check all other devices to see if this master should represent them
            foreach ((var otherEnt, var otherCoords) in devices)
            {
                if (ent == otherEnt)
                    continue;

                if (!TryComp<PowerMonitoringDeviceComponent>(otherEnt, out var otherDevice))
                    continue;

                if (!TryComp<NodeContainerComponent>(otherEnt, out var otherContainer) ||
                    !otherContainer.Nodes.TryGetValue(otherDevice.LoadNode, out var otherLoadNode) ||
                    !otherLoadNode.ReachableNodes.Any())
                    continue;

                // Matching netIds - this device should be represented by the master
                if ((loadNode.NodeGroup as BaseNodeGroup)?.NetId == (otherLoadNode.NodeGroup as BaseNodeGroup)?.NetId)
                {
                    _masterDevices.Remove(otherEnt);

                    device.ChildEntities.Add(otherEnt);
                    otherDevice.MasterUid = ent;

                    // Update the master and device NavMapTrackableComponent
                    if (trackable != null)
                    {
                        trackable.ParentUid = null;
                        trackable.ChildOffsets.Add(otherCoords - coords);
                        Dirty(ent, trackable);

                        if (TryComp<NavMapTrackableComponent>(otherEnt, out var otherTrackable))
                        {
                            otherTrackable.ParentUid = ent;
                            Dirty(otherEnt, otherTrackable);
                        }
                    }
                }
            }
        }
    }
}
