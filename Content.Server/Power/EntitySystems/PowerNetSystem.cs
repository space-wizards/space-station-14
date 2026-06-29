using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Content.Server.Power.Pow3r.Solvers;
using Content.Shared.CCVar;
using Content.Shared.NodeContainer.Systems;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power.Pow3r.Nodes;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Threading;

namespace Content.Server.Power.EntitySystems;

/// <summary>
///     Manages power networks, power state, and all power components.
/// </summary>
public sealed partial class PowerNetSystem : SharedPowerNetSystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private PowerNetConnectorSystem _powerNetConnector = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IParallelManager _parMan = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private PowerNetHandler _handler = default!;

    [Dependency] private EntityQuery<PowerReceiverBatteryComponent> _apcBatteryQuery = default!;
    [Dependency] private EntityQuery<PowerNetworkBatteryComponent> _powerNetworkBatteryQuery = default!;
    [Dependency] private EntityQuery<BatteryComponent> _batteryQuery = default!;
    [Dependency] private EntityQuery<PowerNetworkConnectorComponent> _connectorQuery = default!;
    [Dependency] private EntityQuery<PowerConsumerComponent> _consumerQuery = default!;
    [Dependency] private EntityQuery<PowerSupplierComponent> _supplierQuery = default!;

    private readonly PowerState _powerState = new();
    private readonly HashSet<PowerNet> _powerNetReconnectQueue = new();

    private BatteryRampPegSolver _solver = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(NodeGroupSystem));
        _solver = new(_cfg.GetCVar(CCVars.DebugPow3rDisableParallel));

        SubscribeLocalEvent<PowerReceiverComponent, MapInitEvent>(PowerReceiverMapInit);
        SubscribeLocalEvent<PowerReceiverComponent, ComponentInit>(PowerReceiverInit);
        SubscribeLocalEvent<PowerReceiverComponent, ComponentShutdown>(PowerReceiverShutdown);
        SubscribeLocalEvent<PowerReceiverComponent, ComponentRemove>(PowerReceiverRemove);
        SubscribeLocalEvent<PowerReceiverComponent, EntityPausedEvent>(PowerReceiverPaused);
        SubscribeLocalEvent<PowerReceiverComponent, EntityUnpausedEvent>(PowerReceiverUnpaused);

        SubscribeLocalEvent<PowerNetworkBatteryComponent, ComponentInit>(BatteryInit);
        SubscribeLocalEvent<PowerNetworkBatteryComponent, ComponentShutdown>(BatteryShutdown);
        SubscribeLocalEvent<PowerNetworkBatteryComponent, EntityPausedEvent>(BatteryPaused);
        SubscribeLocalEvent<PowerNetworkBatteryComponent, EntityUnpausedEvent>(BatteryUnpaused);

        SubscribeLocalEvent<PowerConsumerComponent, ComponentInit>(PowerConsumerInit);
        SubscribeLocalEvent<PowerConsumerComponent, ComponentShutdown>(PowerConsumerShutdown);
        SubscribeLocalEvent<PowerConsumerComponent, EntityPausedEvent>(PowerConsumerPaused);
        SubscribeLocalEvent<PowerConsumerComponent, EntityUnpausedEvent>(PowerConsumerUnpaused);

        SubscribeLocalEvent<PowerSupplierComponent, ComponentInit>(PowerSupplierInit);
        SubscribeLocalEvent<PowerSupplierComponent, ComponentShutdown>(PowerSupplierShutdown);
        SubscribeLocalEvent<PowerSupplierComponent, EntityPausedEvent>(PowerSupplierPaused);
        SubscribeLocalEvent<PowerSupplierComponent, EntityUnpausedEvent>(PowerSupplierUnpaused);

        Subs.CVar(_cfg, CCVars.DebugPow3rDisableParallel, DebugPow3rDisableParallelChanged);
    }

    private void DebugPow3rDisableParallelChanged(bool val)
    {
        _solver = new(val);
    }

    private void PowerReceiverMapInit(Entity<PowerReceiverComponent> ent, ref MapInitEvent args)
    {
        _appearance.SetData(ent, PowerDeviceVisuals.Powered, ent.Comp.Powered);
    }

    private void PowerReceiverInit(EntityUid uid, PowerReceiverComponent component, ComponentInit args)
    {
        AllocLoad(component);
    }

    private void PowerReceiverShutdown(EntityUid uid, PowerReceiverComponent component,
        ComponentShutdown args)
    {
        _powerState.Loads.Free(component.Id);
    }

    private void PowerReceiverRemove(Entity<PowerReceiverComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.Provider != null
            && _connectorQuery.TryComp(ent.Owner, out var connector)
            && connector.Net != null)
            _handler.RemoveReceiver(connector.Net, ent.AsNullable(), ent.Comp.Provider.Value);
    }

    private static void PowerReceiverPaused(
        EntityUid uid,
        PowerReceiverComponent component,
        ref EntityPausedEvent args)
    {
        component.Paused = true;
    }

    private static void PowerReceiverUnpaused(
        EntityUid uid,
        PowerReceiverComponent component,
        ref EntityUnpausedEvent args)
    {
        component.Paused = false;
    }

    private void BatteryInit(EntityUid uid, PowerNetworkBatteryComponent component, ComponentInit args)
    {
        AllocBattery(component);
    }

    private void BatteryShutdown(EntityUid uid, PowerNetworkBatteryComponent component, ComponentShutdown args)
    {
        _powerState.Batteries.Free(component.Id);
    }

    private static void BatteryPaused(EntityUid uid, PowerNetworkBatteryComponent component, ref EntityPausedEvent args)
    {
        component.Paused = true;
    }

    private static void BatteryUnpaused(EntityUid uid, PowerNetworkBatteryComponent component, ref EntityUnpausedEvent args)
    {
        component.Paused = false;
    }

    private void PowerConsumerInit(EntityUid uid, PowerConsumerComponent component, ComponentInit args)
    {
        AllocLoad(component);
    }

    private void PowerConsumerShutdown(EntityUid uid, PowerConsumerComponent component, ComponentShutdown args)
    {
        _powerState.Loads.Free(component.Id);
    }

    private static void PowerConsumerPaused(EntityUid uid, PowerConsumerComponent component, ref EntityPausedEvent args)
    {
        component.Paused = true;
    }

    private static void PowerConsumerUnpaused(EntityUid uid, PowerConsumerComponent component, ref EntityUnpausedEvent args)
    {
        component.Paused = false;
    }

    private void PowerSupplierInit(EntityUid uid, PowerSupplierComponent component, ComponentInit args)
    {
        AllocSupply(component);
    }

    private void PowerSupplierShutdown(EntityUid uid, PowerSupplierComponent component, ComponentShutdown args)
    {
        _powerState.Supplies.Free(component.Id);
    }

    private static void PowerSupplierPaused(EntityUid uid, PowerSupplierComponent component, ref EntityPausedEvent args)
    {
        component.Paused = true;
    }

    private static void PowerSupplierUnpaused(EntityUid uid, PowerSupplierComponent component, ref EntityUnpausedEvent args)
    {
        component.Paused = false;
    }

    public void InitPowerNet(PowerNet powerNet)
    {
        AllocNetwork(powerNet);
        _powerState.GroupedNets = null;
    }

    public void DestroyPowerNet(PowerNet powerNet)
    {
        _powerState.Networks.Free(powerNet.Id);
        _powerState.GroupedNets = null;
    }

    public void QueueReconnectPowerNet(PowerNet powerNet)
    {
        _powerNetReconnectQueue.Add(powerNet);
        _powerState.GroupedNets = null;
    }

    public PowerStatistics GetStatistics()
    {
        return new()
        {
            CountBatteries = _powerState.Batteries.Count,
            CountLoads = _powerState.Loads.Count,
            CountNetworks = _powerState.Networks.Count,
            CountSupplies = _powerState.Supplies.Count
        };
    }

    public NetworkPowerStatistics GetNetworkStatistics(IPowerNetwork network)
    {
        // Right, consumption. Now this is a big mess.
        // Start by summing up consumer draw rates.
        // Then deal with batteries.
        // While for consumers we want to use their max draw rates,
        //  for batteries we ought to use their current draw rates,
        //  because there's all sorts of weirdness with them.
        // A full battery will still have the same max draw rate,
        //  but will likely have deliberately limited current draw rate.
        float consumptionW = network.Loads.Sum(s => _powerState.Loads[s].DesiredPower);
        consumptionW += network.BatteryLoads.Sum(s => _powerState.Batteries[s].CurrentReceiving);

        // This is interesting because LastMaxSupplySum seems to match LastAvailableSupplySum for some reason.
        // I suspect it's accounting for current supply rather than theoretical supply.
        float maxSupplyW = network.Supplies.Sum(s => _powerState.Supplies[s].MaxSupply);

        // Battery stuff is more complex.
        // Without stealing PowerState, the most efficient way
        //  to grab the necessary discharge data is from
        //  PowerNetworkBatteryComponent (has Pow3r reference).
        float supplyBatteriesW = 0.0f;
        float storageCurrentJ = 0.0f;
        float storageMaxJ = 0.0f;
        foreach (var discharger in network.BatterySupplies)
        {
            var nb = _powerState.Batteries[discharger];
            supplyBatteriesW += nb.CurrentSupply;
            storageCurrentJ += nb.CurrentStorage;
            storageMaxJ += nb.Capacity;
            maxSupplyW += nb.MaxSupply;
        }
        // And charging
        float outStorageCurrentJ = 0.0f;
        float outStorageMaxJ = 0.0f;
        foreach (var charger in network.BatteryLoads)
        {
            var nb = _powerState.Batteries[charger];
            outStorageCurrentJ += nb.CurrentStorage;
            outStorageMaxJ += nb.Capacity;
        }
        return new()
        {
            SupplyCurrent = network.LastCombinedMaxSupply,
            SupplyBatteries = supplyBatteriesW,
            SupplyTheoretical = maxSupplyW,
            Consumption = consumptionW,
            InStorageCurrent = storageCurrentJ,
            InStorageMax = storageMaxJ,
            OutStorageCurrent = outStorageCurrentJ,
            OutStorageMax = outStorageMaxJ
        };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ReconnectNetworks();

        // Synchronize batteries
        RaiseLocalEvent(new NetworkBatteryPreSync());

        // Run power solver.
        _solver.Tick(frameTime, _powerState, _parMan);

        // Synchronize batteries, the other way around.
        RaiseLocalEvent(new NetworkBatteryPostSync());

        // Send events where necessary.
        // TODO: Instead of querying ALL power components every tick, and then checking if an event needs to be
        // raised, should probably assemble a list of entity Uids during the actual solver steps.
        UpdatePowerReceiver(frameTime);
        UpdatePowerConsumer();
        UpdateNetworkBattery();
    }

    private void ReconnectNetworks()
    {
        foreach (var powerNet in _powerNetReconnectQueue)
        {
            if (powerNet.Removed)
                continue;

            DoReconnectPowerNet(powerNet);
        }

        _powerNetReconnectQueue.Clear();
    }

    public override bool IsPoweredCalculate(PowerReceiverComponent comp)
    {
        // Power is disabled, so unpowered
        if (!comp.Enabled)
            return false;

        // Doesn't need power, so always powered
        if (!comp.NeedsPower)
            return true;

        return comp.DesiredPower > 0 && MathHelper.CloseToPercent(comp.ReceivingPower, comp.DesiredPower);
    }

    private void UpdatePowerReceiver(float frameTime)
    {
        var enumerator = AllEntityQuery<PowerReceiverComponent>();
        while (enumerator.MoveNext(out var uid, out var apcReceiver))
        {
            var powered = IsPoweredCalculate(apcReceiver);

            MetaDataComponent? metadata = null;

            // TODO: If we get archetypes would be better to split this out.
            // Check if the entity has an internal battery
            if (_apcBatteryQuery.TryComp(uid, out var apcBattery) && _batteryQuery.TryComp(uid, out var battery))
            {
                metadata = MetaData(uid);
                if (Paused(uid, metadata))
                    continue;

                apcReceiver.DesiredPower = apcBattery.IdleLoad;

                // Try to draw power from the battery if there isn't sufficient external power
                var requireBattery = !powered && apcReceiver.Enabled;

                if (requireBattery)
                {
                    _battery.ChangeCharge((uid, battery), -apcBattery.IdleLoad * frameTime);
                }
                // Otherwise try to charge the battery
                else if (powered && !_battery.IsFull((uid, battery)))
                {
                    apcReceiver.DesiredPower += apcBattery.BatteryRechargeRate * apcBattery.BatteryRechargeEfficiency;
                    _battery.ChangeCharge((uid, battery), apcBattery.BatteryRechargeRate * frameTime);
                }

                // Enable / disable the battery if the state changed
                var currentCharge = _battery.GetCharge((uid, battery));
                var enableBattery = requireBattery && currentCharge > 0;

                if (apcBattery.Enabled != enableBattery)
                {
                    apcBattery.Enabled = enableBattery;
                    Dirty(uid, apcBattery, metadata);

                    var apcBatteryEv = new PowerReceiverBatteryChangedEvent(enableBattery);
                    RaiseLocalEvent(uid, ref apcBatteryEv);

                    _appearance.SetData(uid, PowerDeviceVisuals.BatteryPowered, enableBattery);
                }

                powered |= enableBattery;
            }

            // If new value is the same as the old, then exit
            if (apcReceiver.Powered == powered)
                continue;

            metadata ??= MetaData(uid);
            if (Paused(uid, metadata))
                continue;

            apcReceiver.Powered = powered;
            Dirty(uid, apcReceiver, metadata);

            var ev = new PowerChangedEvent(powered, apcReceiver.ReceivingPower);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void UpdatePowerConsumer()
    {
        var enumerator = EntityQueryEnumerator<PowerConsumerComponent>();
        while (enumerator.MoveNext(out var uid, out var consumer))
        {
            var newRecv = consumer.ReceivingPower;
            ref var lastRecv = ref consumer.LastReceived;
            if (MathHelper.CloseToPercent(lastRecv, newRecv))
                continue;

            lastRecv = newRecv;
            var msg = new PowerConsumerReceivedChanged(newRecv, consumer.DesiredPower);
            RaiseLocalEvent(uid, ref msg);
        }
    }

    private void UpdateNetworkBattery()
    {
        var enumerator = EntityQueryEnumerator<PowerNetworkBatteryComponent>();
        while (enumerator.MoveNext(out var uid, out var powerNetBattery))
        {
            var lastSupply = powerNetBattery.LastSupply;
            var currentSupply = powerNetBattery.CurrentSupply;

            if (lastSupply == 0f && currentSupply != 0f)
            {
                var ev = new PowerNetBatterySupplyEvent(true);
                RaiseLocalEvent(uid, ref ev);
            }
            else if (lastSupply > 0f && currentSupply == 0f)
            {
                var ev = new PowerNetBatterySupplyEvent(false);
                RaiseLocalEvent(uid, ref ev);
            }

            powerNetBattery.LastSupply = currentSupply;
        }
    }

    private void AllocLoad<T>(T load) where T : class, IPowerLoad
    {
        _powerState.Loads.Allocate(out var loadId) = load;
        load.Id = loadId;
    }

    private void AllocSupply<T>(T supply) where T : class, IPowerSupply
    {
        _powerState.Supplies.Allocate(out var supplyId) = supply;
        supply.Id = supplyId;
    }

    private void AllocBattery<T>(T battery) where T : class, IPowerBattery
    {
        _powerState.Batteries.Allocate(out var batteryId) = battery;
        battery.Id = batteryId;
    }

    private void AllocNetwork<T>(T network) where T : class, IPowerNetwork
    {
        _powerState.Networks.Allocate(out var networkId) = network;
        network.Id = networkId;
    }

    private void DoReconnectPowerNet(PowerNet net)
    {
        net.Loads.Clear();
        net.Supplies.Clear();
        net.BatteryLoads.Clear();
        net.BatterySupplies.Clear();

        foreach (var consumer in net.Consumers)
        {
            if (!_consumerQuery.TryComp(consumer, out var consumerComp))
                continue;

            net.Loads.Add(consumerComp.Id);
            consumerComp.LinkedNetwork = net.Id;
        }

        foreach (var supplier in net.Suppliers)
        {
            if (!_supplierQuery.TryComp(supplier, out var supplierComp))
                continue;

            net.Supplies.Add(supplierComp.Id);
            supplierComp.LinkedNetwork = net.Id;
        }

        foreach (var charger in net.Chargers)
        {
            if (!_powerNetworkBatteryQuery.TryComp(charger, out var battery))
                continue;

            net.BatteryLoads.Add(battery.Id);
            battery.LinkedNetworkCharging = net.Id;
        }

        foreach (var discharger in net.Dischargers)
        {
            if (!_powerNetworkBatteryQuery.TryComp(discharger, out var battery))
                continue;

            net.BatterySupplies.Add(battery.Id);
            battery.LinkedNetworkDischarging = net.Id;
        }
    }

    /// <summary>
    /// Validate integrity of the power state data. Throws if an error is found.
    /// </summary>
    public void Validate()
    {
        _solver.Validate(_powerState);
    }
}

/// <summary>
///     Raised before power network simulation happens, to synchronize battery state from
///     components like <see cref="BatteryComponent"/> into <see cref="PowerNetworkBatteryComponent"/>.
/// </summary>
public readonly struct NetworkBatteryPreSync
{
}

/// <summary>
///     Raised after power network simulation happens, to synchronize battery charge changes from
///     <see cref="PowerNetworkBatteryComponent"/> to components like <see cref="BatteryComponent"/>.
/// </summary>
public readonly struct NetworkBatteryPostSync
{
}

/// <summary>
///     Raised when the amount of receiving power on a <see cref="PowerConsumerComponent"/> changes.
/// </summary>
[ByRefEvent]
public readonly record struct PowerConsumerReceivedChanged(float ReceivedPower, float DesiredPower)
{
    public readonly float ReceivedPower = ReceivedPower;
    public readonly float DesiredPower = DesiredPower;
}

/// <summary>
/// Raised whenever a <see cref="PowerNetworkBatteryComponent"/> changes from / to 0 CurrentSupply.
/// </summary>
[ByRefEvent]
public readonly record struct PowerNetBatterySupplyEvent(bool Supply)
{
    public readonly bool Supply = Supply;
}

public struct PowerStatistics
{
    public int CountNetworks;
    public int CountLoads;
    public int CountSupplies;
    public int CountBatteries;
}

public struct NetworkPowerStatistics
{
    public float SupplyCurrent;
    public float SupplyBatteries;
    public float SupplyTheoretical;
    public float Consumption;
    public float InStorageCurrent;
    public float InStorageMax;
    public float OutStorageCurrent;
    public float OutStorageMax;
}
