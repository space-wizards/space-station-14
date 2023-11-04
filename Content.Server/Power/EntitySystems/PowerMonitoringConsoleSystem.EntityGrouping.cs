using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Pinpointer.UI;
using Content.Server.Power.Components;
using Content.Server.StationEvents.Components;
using Content.Server.UserInterface;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Robust.Server.GameStates;
using Robust.Shared.Map.Components;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;
using Content.Server.NodeContainer;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Power.EntitySystems;

internal sealed partial class PowerMonitoringConsoleSystem
{
    private Dictionary<EntProtoId, Dictionary<EntityUid, EntityCoordinates>> _groupableEntityCoords = new();
    private Dictionary<EntityUid, PowerMonitoringDeviceComponent> _exemplarDevices = new();
    private HashSet<EntProtoId> _exemplarTypesToUpdate = new();

    private void AssignEntityToTrackingGroup(EntityUid uid)
    {
        var protoId = MetaData(uid).EntityPrototype?.ID;

        if (protoId == null)
            return;

        var entProtoId = (EntProtoId) protoId;

        if (!_groupableEntityCoords.ContainsKey(entProtoId))
            _groupableEntityCoords[entProtoId] = new();

        _groupableEntityCoords[entProtoId].Add(uid, Transform(uid).Coordinates);
    }

    private void RemoveEntityFromTrackingGroup(EntityUid uid)
    {
        _exemplarDevices.Remove(uid);

        if (TryComp<PowerMonitoringDeviceComponent>(uid, out var device))
        {
            device.ExemplarUid = new EntityUid();
            device.ChildEntities.Clear();
        }

        if (!TryGetEntProtoId(uid, out var entProtoId) || !_groupableEntityCoords.ContainsKey(entProtoId.Value))
            return;

        _groupableEntityCoords[entProtoId.Value].Remove(uid);
    }

    private void AssignExemplarsToEntities(EntProtoId entProtoId)
    {
        if (!_groupableEntityCoords.TryGetValue(entProtoId, out var devices) || !devices.Any())
            return;

        var currentEntity = devices.Last().Key;

        foreach ((var ent, var coords) in devices)
        {
            if (!TryComp<PowerMonitoringDeviceComponent>(ent, out var device))
                continue;

            device.ChildEntities.Clear();

            if (TryComp<NavMapTrackableComponent>(ent, out var trackable))
            {
                trackable.Coordinates = EntityManager.GetNetCoordinates(coords);
                trackable.ChildCoordinates.Clear();
                Dirty(ent, trackable);
            }

            if (!TryComp<NodeContainerComponent>(ent, out var container) ||
                !container.Nodes.TryGetValue(device.LoadNode, out var loadNode) ||
                !loadNode.ReachableNodes.Any())
            {
                device.ExemplarUid = ent;
                _exemplarDevices.TryAdd(ent, device);

                continue;
            }

            if (device.ExemplarUid == currentEntity)
                continue;

            currentEntity = ent;
            device.ExemplarUid = ent;
            _exemplarDevices.TryAdd(ent, device);

            var reachableNodes = FloodFillNode(loadNode);

            foreach (var node in reachableNodes)
            {
                var foundCoords = Transform(node.Owner).Coordinates;
                var foundEnt = devices.FirstOrNull(x => x.Value == foundCoords)?.Key;

                if (foundEnt != null)
                {
                    if (!TryComp<PowerMonitoringDeviceComponent>(foundEnt, out var foundDevice))
                        continue;

                    _exemplarDevices.Remove(foundEnt.Value);

                    device.ChildEntities.Add(foundEnt.Value);
                    foundDevice.ExemplarUid = currentEntity;

                    if (trackable != null)
                        trackable.ChildCoordinates.Add(EntityManager.GetNetCoordinates(foundCoords));
                }
            }
        }
    }

    private bool TryGetEntProtoId(EntityUid uid, [NotNullWhen(true)] out EntProtoId? entProtoId)
    {
        entProtoId = null;
        var protoId = MetaData(uid)?.EntityPrototype?.ID;

        if (protoId == null)
            return false;

        entProtoId = (EntProtoId) protoId;
        return true;
    }
}
