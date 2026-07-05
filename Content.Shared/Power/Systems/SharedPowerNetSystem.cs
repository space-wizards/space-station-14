using Content.Shared.Power.Components;
using Content.Shared.Power.Events;

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

        SubscribeLocalEvent<PowerReceiverComponent, ComponentRemove>(PowerReceiverRemove);
        SubscribeLocalEvent<PowerReceiverComponent, EntityPausedEvent>(PowerReceiverPaused);
        SubscribeLocalEvent<PowerReceiverComponent, EntityUnpausedEvent>(PowerReceiverUnpaused);

        SubscribeLocalEvent<PowerNetworkBatteryComponent, EntityPausedEvent>(BatteryPaused);
        SubscribeLocalEvent<PowerNetworkBatteryComponent, EntityUnpausedEvent>(BatteryUnpaused);

        SubscribeLocalEvent<PowerConsumerComponent, EntityPausedEvent>(PowerConsumerPaused);
        SubscribeLocalEvent<PowerConsumerComponent, EntityUnpausedEvent>(PowerConsumerUnpaused);

        SubscribeLocalEvent<PowerSupplierComponent, EntityPausedEvent>(PowerSupplierPaused);
        SubscribeLocalEvent<PowerSupplierComponent, EntityUnpausedEvent>(PowerSupplierUnpaused);
    }

    private void OnPowerAppearance(Entity<AppearanceComponent> ent, ref PowerChangedEvent args)
    {
        _appearance.SetData(ent, PowerDeviceVisuals.Powered, args.Powered, ent.Comp);
    }

    public virtual void InitPowerNet(Entity<PowerNetComponent> powerNet) { }

    public virtual void DestroyPowerNet(Entity<PowerNetComponent> powerNet) { }

    public virtual void QueueReconnectPowerNet(Entity<PowerNetComponent> powerNet) { }

    private void PowerReceiverMapInit(Entity<PowerReceiverComponent> ent, ref MapInitEvent args)
    {
        _appearance.SetData(ent, PowerDeviceVisuals.Powered, ent.Comp.Powered);
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

    private void BatteryPaused(Entity<PowerNetworkBatteryComponent> ent, ref EntityPausedEvent args)
    {
        ent.Comp.Paused = true;
    }

    private void BatteryUnpaused(Entity<PowerNetworkBatteryComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.Paused = false;
    }

    private void PowerConsumerPaused(Entity<PowerConsumerComponent> ent, ref EntityPausedEvent args)
    {
        ent.Comp.Paused = true;
    }

    private void PowerConsumerUnpaused(Entity<PowerConsumerComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.Paused = false;
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
