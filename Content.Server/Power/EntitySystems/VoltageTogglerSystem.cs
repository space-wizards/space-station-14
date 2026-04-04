using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Verbs;

namespace Content.Server.Power.EntitySystems;

public sealed class VoltageTogglerSystem : SharedVoltageTogglerSystem
{
    [Dependency] private readonly NodeGroupSystem _nodeGroupSystem = null!;

    public override void ChangeVoltage(Entity<VoltageTogglerComponent> entity, VoltageSetting setting)
    {
        if (TryComp<NodeContainerComponent>(entity, out var nodeContainerComp))
        {
            var newNodeGroupId = setting.Voltage switch
            {
                Voltage.Apc => NodeGroupID.Apc,
                Voltage.Medium => NodeGroupID.MVPower,
                Voltage.High => NodeGroupID.HVPower,
                _ => NodeGroupID.Default,
            };

            var inputNode = nodeContainerComp.Nodes["input"];
            _nodeGroupSystem.QueueNodeRemove(inputNode);
            inputNode.SetNodeGroupId(newNodeGroupId);
            _nodeGroupSystem.QueueReflood(inputNode);
        }

        if (TryComp<PowerConsumerComponent>(entity, out var powerConsumerComp))
        {
            powerConsumerComp.Voltage = setting.Voltage;
            powerConsumerComp.DrawRate = setting.Wattage;
        }
    }
}
