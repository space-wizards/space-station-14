using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;

namespace Content.Server.Power.EntitySystems;

public sealed class PowerNetConnectorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ApcComponent, ComponentInit>(OnApcInit);
        SubscribeLocalEvent<ApcPowerProviderComponent, ComponentInit>(OnApcPowerProviderInit);
        SubscribeLocalEvent<BatteryChargerComponent, ComponentInit>(OnBatteryChargerInit);
        SubscribeLocalEvent<BatteryDischargerComponent, ComponentInit>(OnBatteryDischargerInit);
    }

    private void OnPowerSupplierInit(EntityUid uid, PowerSupplierComponent component, ComponentInit args)
    {
        BaseNetConnectorInit(component);
    }

    private void OnBatteryDischargerInit(EntityUid uid, BatteryDischargerComponent component, ComponentInit args)
    {
        BaseNetConnectorInit(component);
    }

    private void OnBatteryChargerInit(EntityUid uid, BatteryChargerComponent component, ComponentInit args)
    {
        BaseNetConnectorInit(component);
    }

    private void OnApcPowerProviderInit(EntityUid uid, ApcPowerProviderComponent component, ComponentInit args)
    {
        BaseNetConnectorInit(component);
    }

    private void OnApcInit(EntityUid uid, ApcComponent component, ComponentInit args)
    {
        BaseNetConnectorInit(component);
    }

    public void BaseNetConnectorInit<T>(BaseNetConnectorComponent<T> component)
    {
        if (component.NeedsNet)
        {
            component.TryFindAndSetNet();
        }
    }
}
