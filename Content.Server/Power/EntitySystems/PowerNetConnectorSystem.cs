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

        // TODO please end my life
        SubscribeLocalEvent<ApcComponent, ComponentRemove>(OnRemove<ApcComponent, IApcNet>);
        SubscribeLocalEvent<ApcPowerProviderComponent, ComponentRemove>(OnRemove<ApcPowerProviderComponent, IApcNet>);
        SubscribeLocalEvent<BatteryChargerComponent, ComponentRemove>(OnRemove<BatteryChargerComponent, IPowerNet>);
        SubscribeLocalEvent<BatteryDischargerComponent, ComponentRemove>(OnRemove<BatteryDischargerComponent, IPowerNet>);
        SubscribeLocalEvent<PowerConsumerComponent, ComponentRemove>(OnRemove<PowerConsumerComponent, IBasePowerNet>);
        SubscribeLocalEvent<PowerSupplierComponent, ComponentRemove>(OnRemove<PowerSupplierComponent, IBasePowerNet>);
    }

    private void OnRemove<TComp, TNet>(EntityUid uid, TComp component, ComponentRemove args)
        where TComp : BaseNetConnectorComponent<TNet>
        where TNet : class
    {
        component.ClearNet();
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

    public void BaseNetConnectorInit<T>(BaseNetConnectorComponent<T> component) where T : class
    {
        if (component.NeedsNet)
        {
            component.TryFindAndSetNet();
        }
    }
}
