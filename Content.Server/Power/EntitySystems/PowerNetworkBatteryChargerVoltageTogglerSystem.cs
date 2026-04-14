using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Verbs;

namespace Content.Server.Power.EntitySystems;

public sealed class PowerNetworkBatteryChargerVoltageTogglerSystem : SharedPowerNetworkBatteryChargerVoltageTogglerSystem
{
    [Dependency] private readonly NodeGroupSystem _nodeGroupSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerNetworkBatteryChargerVoltageTogglerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<PowerNetworkBatteryChargerVoltageTogglerComponent> entity, ref MapInitEvent args)
    {
        ChangeVoltage(entity, entity.Comp.Settings[entity.Comp.SelectedVoltageLevel]);
    }

    protected override void ChangeVoltage(Entity<PowerNetworkBatteryChargerVoltageTogglerComponent> entity, VoltageSetting setting)
    {
        if (!TryComp<NodeContainerComponent>(entity, out var nodeContainerComp) ||
            !TryComp<PowerNetworkBatteryComponent>(entity, out var powerNetworkBatteryComp) ||
            !TryComp<BatteryChargerComponent>(entity, out var batteryChargerComp))
            return;

        foreach (var settingAlt in entity.Comp.Settings)
        {
            var node = (CableDeviceNode) nodeContainerComp.Nodes[settingAlt.Node];
            node.Enabled = settingAlt.Voltage == setting.Voltage;
            _nodeGroupSystem.QueueReflood(node);
        }

        batteryChargerComp.Voltage = setting.Voltage;
        powerNetworkBatteryComp.MaxChargeRate = setting.Wattage;
    }
}
