using Content.Shared.Power.Components;
using Content.Shared.Power.Events;
using Content.Shared.Power.Pow3r;
using Content.Shared.Power.Pow3r.Nodes;

namespace Content.Shared.Power.Systems;

public abstract partial class SharedPowerNetSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private PowerNetHandler _handler = default!;
    [Dependency] private EntityQuery<PowerNetworkConnectorComponent> _connectorQuery = default!;

    public abstract bool IsPoweredCalculate(PowerReceiverComponent comp);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AppearanceComponent, PowerChangedEvent>(OnPowerAppearance);

        SubscribeLocalEvent<PowerReceiverComponent, MapInitEvent>(PowerReceiverMapInit);
        SubscribeLocalEvent<PowerReceiverComponent, ComponentInit>(PowerReceiverInit);
        SubscribeLocalEvent<PowerReceiverComponent, ComponentRemove>(PowerReceiverRemove);
        SubscribeLocalEvent<PowerReceiverComponent, EntityPausedEvent>(PowerReceiverPaused);
        SubscribeLocalEvent<PowerReceiverComponent, EntityUnpausedEvent>(PowerReceiverUnpaused);

        SubscribeLocalEvent<PowerNetworkBatteryComponent, ComponentInit>(BatteryInit);
        SubscribeLocalEvent<PowerNetworkBatteryComponent, EntityPausedEvent>(BatteryPaused);
        SubscribeLocalEvent<PowerNetworkBatteryComponent, EntityUnpausedEvent>(BatteryUnpaused);

        SubscribeLocalEvent<PowerConsumerComponent, ComponentInit>(PowerConsumerInit);
        SubscribeLocalEvent<PowerConsumerComponent, EntityPausedEvent>(PowerConsumerPaused);
        SubscribeLocalEvent<PowerConsumerComponent, EntityUnpausedEvent>(PowerConsumerUnpaused);

        SubscribeLocalEvent<PowerSupplierComponent, ComponentInit>(PowerSupplierInit);
        SubscribeLocalEvent<PowerSupplierComponent, EntityPausedEvent>(PowerSupplierPaused);
        SubscribeLocalEvent<PowerSupplierComponent, EntityUnpausedEvent>(PowerSupplierUnpaused);
    }

    private void OnPowerAppearance(Entity<AppearanceComponent> ent, ref PowerChangedEvent args)
    {
        _appearance.SetData(ent, PowerDeviceVisuals.Powered, args.Powered, ent.Comp);
    }

    public virtual void InitPowerNet(Entity<PowerNetComponent> powerNet)
    {
        AllocNetwork(powerNet);
    }

    public virtual void DestroyPowerNet(Entity<PowerNetComponent> powerNet) { }

    public virtual void QueueReconnectPowerNet(Entity<PowerNetComponent> powerNet) { }

    protected virtual void AllocLoad(Entity<PowerConsumerComponent> load)
    {
        load.Comp.Load = new PowerLoad();
    }

    protected virtual void AllocLoad(Entity<PowerReceiverComponent> load)
    {
        load.Comp.Load = new PowerLoad();
    }

    protected virtual void AllocSupply(Entity<PowerSupplierComponent> supply)
    {
        supply.Comp.Supply = new PowerSupply();
    }

    protected virtual void AllocBattery(Entity<PowerNetworkBatteryComponent> battery)
    {
        battery.Comp.Battery = new PowerBattery();
    }

    protected virtual void AllocNetwork(Entity<PowerNetComponent> network)
    {
        network.Comp.Network = new PowerNetwork();
    }

    private void PowerReceiverMapInit(Entity<PowerReceiverComponent> ent, ref MapInitEvent args)
    {
        _appearance.SetData(ent, PowerDeviceVisuals.Powered, ent.Comp.Powered);
    }

    private void PowerReceiverInit(Entity<PowerReceiverComponent> ent, ref ComponentInit args)
    {
        AllocLoad(ent);
    }

    private void PowerReceiverRemove(Entity<PowerReceiverComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.Provider != null
            && _connectorQuery.TryComp(ent.Owner, out var connector)
            && connector.Net != null)
            _handler.RemoveReceiver(connector.Net.Value, ent.AsNullable(), ent.Comp.Provider.Value);
    }

    private void PowerReceiverPaused(Entity<PowerReceiverComponent> ent, ref EntityPausedEvent args)
    {
        ent.Comp.Paused = true;
    }

    private void PowerReceiverUnpaused(Entity<PowerReceiverComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.Paused = false;
    }

    private void BatteryInit(Entity<PowerNetworkBatteryComponent> ent, ref ComponentInit args)
    {
        AllocBattery(ent);
    }

    private void BatteryPaused(Entity<PowerNetworkBatteryComponent> ent, ref EntityPausedEvent args)
    {
        ent.Comp.Paused = true;
    }

    private void BatteryUnpaused(Entity<PowerNetworkBatteryComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.Paused = false;
    }

    private void PowerConsumerInit(Entity<PowerConsumerComponent> ent, ref ComponentInit args)
    {
        AllocLoad(ent);
    }

    private void PowerConsumerPaused(Entity<PowerConsumerComponent> ent, ref EntityPausedEvent args)
    {
        ent.Comp.Paused = true;
    }

    private void PowerConsumerUnpaused(Entity<PowerConsumerComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.Paused = false;
    }

    private void PowerSupplierInit(Entity<PowerSupplierComponent> ent, ref ComponentInit args)
    {
        AllocSupply(ent);
    }

    private void PowerSupplierPaused(Entity<PowerSupplierComponent> ent, ref EntityPausedEvent args)
    {
        ent.Comp.Paused = true;
    }

    private void PowerSupplierUnpaused(Entity<PowerSupplierComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.Paused = false;
    }
}
