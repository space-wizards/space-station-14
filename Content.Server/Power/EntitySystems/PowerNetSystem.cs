using System.Linq;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Content.Shared.CCVar;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Threading;

namespace Content.Server.Power.EntitySystems
{
    /// <summary>
    ///     Manages power networks, power state, and all power components.
    /// </summary>
    [UsedImplicitly]
    public sealed class PowerNetSystem : SharedPowerNetSystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly PowerNetConnectorSystem _powerNetConnector = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IParallelManager _parMan = default!;
        [Dependency] private readonly BatterySystem _battery = default!;

        private readonly PowerState _powerState = new();
        private readonly HashSet<PowerNet> _powerNetReconnectQueue = new();
        private readonly HashSet<ApcNet> _apcNetReconnectQueue = new();

        private EntityQuery<ApcPowerReceiverBatteryComponent> _apcBatteryQuery;
        private EntityQuery<AppearanceComponent> _appearanceQuery;
        private EntityQuery<BatteryComponent> _batteryQuery;

        private BatteryRampPegSolver _solver = new();

        public override void Initialize()
        {
            base.Initialize();

            _apcBatteryQuery = GetEntityQuery<ApcPowerReceiverBatteryComponent>();
            _appearanceQuery = GetEntityQuery<AppearanceComponent>();
            _batteryQuery = GetEntityQuery<BatteryComponent>();

            UpdatesAfter.Add(typeof(NodeGroupSystem));
            _solver = new(_cfg.GetCVar(CCVars.DebugPow3rDisableParallel));

            SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentInit>(ApcPowerReceiverInit);
            SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentShutdown>(ApcPowerReceiverShutdown);
            SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentRemove>(ApcPowerReceiverRemove);
            SubscribeLocalEvent<ApcPowerReceiverComponent, EntityPausedEvent>(ApcPowerReceiverPaused);
            SubscribeLocalEvent<ApcPowerReceiverComponent, EntityUnpausedEvent>(ApcPowerReceiverUnpaused);

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

        private void ApcPowerReceiverInit(EntityUid uid, ApcPowerReceiverComponent component, ComponentInit args)
        {
            AllocLoad(component.NetworkLoad);
        }

        private void ApcPowerReceiverShutdown(EntityUid uid, ApcPowerReceiverComponent component,
            ComponentShutdown args)
        {
            _powerState.Loads.Free(component.NetworkLoad.Id);
        }

        private void ApcPowerReceiverRemove(EntityUid uid, ApcPowerReceiverComponent component, ComponentRemove args)
        {
            component.Provider?.RemoveReceiver(component);
        }

        private static void ApcPowerReceiverPaused(
            EntityUid uid,
            ApcPowerReceiverComponent component,
            ref EntityPausedEvent args)
        {
            component.NetworkLoad.Paused = true;
        }

        private static void ApcPowerReceiverUnpaused(
            EntityUid uid,
            ApcPowerReceiverComponent component,
            ref EntityUnpausedEvent args)
        {
            component.NetworkLoad.Paused = false;
        }

        private void BatteryInit(EntityUid uid, PowerNetworkBatteryComponent component, ComponentInit args)
        {
            AllocBattery(component.NetworkBattery);
        }

        private void BatteryShutdown(EntityUid uid, PowerNetworkBatteryComponent component, ComponentShutdown args)
        {
            _powerState.Batteries.Free(component.NetworkBattery.Id);
        }

        private static void BatteryPaused(EntityUid uid, PowerNetworkBatteryComponent component, ref EntityPausedEvent args)
        {
            component.NetworkBattery.Paused = true;
        }

        private static void BatteryUnpaused(EntityUid uid, PowerNetworkBatteryComponent component, ref EntityUnpausedEvent args)
        {
            component.NetworkBattery.Paused = false;
        }

        private void PowerConsumerInit(EntityUid uid, PowerConsumerComponent component, ComponentInit args)
        {
            _powerNetConnector.BaseNetConnectorInit(component);
            AllocLoad(component.NetworkLoad);
        }

        private void PowerConsumerShutdown(EntityUid uid, PowerConsumerComponent component, ComponentShutdown args)
        {
            _powerState.Loads.Free(component.NetworkLoad.Id);
        }

        private static void PowerConsumerPaused(EntityUid uid, PowerConsumerComponent component, ref EntityPausedEvent args)
        {
            component.NetworkLoad.Paused = true;
        }

        private static void PowerConsumerUnpaused(EntityUid uid, PowerConsumerComponent component, ref EntityUnpausedEvent args)
        {
            component.NetworkLoad.Paused = false;
        }

        private void PowerSupplierInit(EntityUid uid, PowerSupplierComponent component, ComponentInit args)
        {
            _powerNetConnector.BaseNetConnectorInit(component);
            AllocSupply(component.NetworkSupply);
        }

        private void PowerSupplierShutdown(EntityUid uid, PowerSupplierComponent component, ComponentShutdown args)
        {
            _powerState.Supplies.Free(component.NetworkSupply.Id);
        }

        private static void PowerSupplierPaused(EntityUid uid, PowerSupplierComponent component, ref EntityPausedEvent args)
        {
            component.NetworkSupply.Paused = true;
        }

        private static void PowerSupplierUnpaused(EntityUid uid, PowerSupplierComponent component, ref EntityUnpausedEvent args)
        {
            component.NetworkSupply.Paused = false;
        }

        public void InitPowerNet(PowerNet powerNet)
        {
            AllocNetwork(powerNet.NetworkNode);
            _powerState.GroupedNets = null;
        }

        public void DestroyPowerNet(PowerNet powerNet)
        {
            _powerState.Networks.Free(powerNet.NetworkNode.Id);
            _powerState.GroupedNets = null;
        }

        public void QueueReconnectPowerNet(PowerNet powerNet)
        {
            _powerNetReconnectQueue.Add(powerNet);
            _powerState.GroupedNets = null;
        }

        public void InitApcNet(ApcNet apcNet)
        {
            AllocNetwork(apcNet.NetworkNode);
            _powerState.GroupedNets = null;
        }

        public void DestroyApcNet(ApcNet apcNet)
        {
            _powerState.Networks.Free(apcNet.NetworkNode.Id);
            _powerState.GroupedNets = null;
        }

        public void QueueReconnectApcNet(ApcNet apcNet)
        {
            _apcNetReconnectQueue.Add(apcNet);
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

        public NetworkPowerStatistics GetNetworkStatistics(PowerState.Network network)
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
            UpdateApcPowerReceiver(frameTime);
            UpdatePowerConsumer();
            UpdateNetworkBattery();
        }

        private void ReconnectNetworks()
        {
            foreach (var apcNet in _apcNetReconnectQueue)
            {
                if (apcNet.Removed)
                    continue;

                DoReconnectApcNet(apcNet);
            }

            _apcNetReconnectQueue.Clear();

            foreach (var powerNet in _powerNetReconnectQueue)
            {
                if (powerNet.Removed)
                    continue;

                DoReconnectPowerNet(powerNet);
            }

            _powerNetReconnectQueue.Clear();
        }

        private void UpdateApcPowerReceiver(float frameTime)
        {
            var enumerator = AllEntityQuery<ApcPowerReceiverComponent>();
            while (enumerator.MoveNext(out var uid, out var apcReceiver))
            {
                var powered = !apcReceiver.PowerDisabled
                              && (!apcReceiver.NeedsPower
                                  || MathHelper.CloseToPercent(apcReceiver.NetworkLoad.ReceivingPower,
                                      apcReceiver.Load));

                MetaDataComponent? metadata = null;

                // TODO: If we get archetypes would be better to split this out.
                // Check if the entity has an internal battery
                if (_apcBatteryQuery.TryComp(uid, out var apcBattery) && _batteryQuery.TryComp(uid, out var battery))
                {
                    apcReceiver.Load = apcBattery.IdleLoad;

                    // Try to draw power from the battery if there isn't sufficient external power
                    var requireBattery = !powered && !apcReceiver.PowerDisabled;

                    if (requireBattery)
                    {
                        _battery.SetCharge(uid, battery.CurrentCharge - apcBattery.IdleLoad * frameTime, battery);
                    }
                    // Otherwise try to charge the battery
                    else if (powered && !_battery.IsFull(uid, battery))
                    {
                        apcReceiver.Load += apcBattery.BatteryRechargeRate * apcBattery.BatteryRechargeEfficiency;
                        _battery.SetCharge(uid, battery.CurrentCharge + apcBattery.BatteryRechargeRate * frameTime, battery);
                    }

                    // Enable / disable the battery if the state changed
                    var enableBattery = requireBattery && battery.CurrentCharge > 0;

                    if (apcBattery.Enabled != enableBattery)
                    {
                        apcBattery.Enabled = enableBattery;
                        metadata = MetaData(uid);
                        Dirty(uid, apcBattery, metadata);

                        var apcBatteryEv = new ApcPowerReceiverBatteryChangedEvent(enableBattery);
                        RaiseLocalEvent(uid, ref apcBatteryEv);

                        _appearance.SetData(uid, PowerDeviceVisuals.BatteryPowered, enableBattery);
                    }

                    powered |= enableBattery;
                }

                // If new value is the same as the old, then exit
                if (!apcReceiver.Recalculate && apcReceiver.Powered == powered)
                    continue;

                metadata ??= MetaData(uid);
                if (Paused(uid, metadata))
                    continue;

                apcReceiver.Recalculate = false;
                apcReceiver.Powered = powered;
                Dirty(uid, apcReceiver, metadata);

                var ev = new PowerChangedEvent(powered, apcReceiver.NetworkLoad.ReceivingPower);
                RaiseLocalEvent(uid, ref ev);

                if (_appearanceQuery.TryComp(uid, out var appearance))
                    _appearance.SetData(uid, PowerDeviceVisuals.Powered, powered, appearance);
            }
        }

        private void UpdatePowerConsumer()
        {
            var enumerator = EntityQueryEnumerator<PowerConsumerComponent>();
            while (enumerator.MoveNext(out var uid, out var consumer))
            {
                var newRecv = consumer.NetworkLoad.ReceivingPower;
                ref var lastRecv = ref consumer.LastReceived;
                if (MathHelper.CloseToPercent(lastRecv, newRecv))
                    continue;

                lastRecv = newRecv;
                var msg = new PowerConsumerReceivedChanged(newRecv, consumer.DrawRate);
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

        private void AllocLoad(PowerState.Load load)
        {
            _powerState.Loads.Allocate(out load.Id) = load;
        }

        private void AllocSupply(PowerState.Supply supply)
        {
            _powerState.Supplies.Allocate(out supply.Id) = supply;
        }

        private void AllocBattery(PowerState.Battery battery)
        {
            _powerState.Batteries.Allocate(out battery.Id) = battery;
        }

        private void AllocNetwork(PowerState.Network network)
        {
            _powerState.Networks.Allocate(out network.Id) = network;
        }

        private void DoReconnectApcNet(ApcNet net)
        {
            var netNode = net.NetworkNode;

            netNode.Loads.Clear();
            netNode.BatterySupplies.Clear();
            netNode.BatteryLoads.Clear();
            netNode.Supplies.Clear();

            foreach (var provider in net.Providers)
            {
                foreach (var receiver in provider.LinkedReceivers)
                {
                    netNode.Loads.Add(receiver.NetworkLoad.Id);
                    receiver.NetworkLoad.LinkedNetwork = netNode.Id;
                }
            }

            DoReconnectBasePowerNet(net, netNode);

            var batteryQuery = GetEntityQuery<PowerNetworkBatteryComponent>();

            foreach (var apc in net.Apcs)
            {
                var netBattery = batteryQuery.GetComponent(apc.Owner);
                netNode.BatterySupplies.Add(netBattery.NetworkBattery.Id);
                netBattery.NetworkBattery.LinkedNetworkDischarging = netNode.Id;
            }
        }

        private void DoReconnectPowerNet(PowerNet net)
        {
            var netNode = net.NetworkNode;

            netNode.Loads.Clear();
            netNode.Supplies.Clear();
            netNode.BatteryLoads.Clear();
            netNode.BatterySupplies.Clear();

            DoReconnectBasePowerNet(net, netNode);

            var batteryQuery = GetEntityQuery<PowerNetworkBatteryComponent>();

            foreach (var charger in net.Chargers)
            {
                var battery = batteryQuery.GetComponent(charger.Owner);
                netNode.BatteryLoads.Add(battery.NetworkBattery.Id);
                battery.NetworkBattery.LinkedNetworkCharging = netNode.Id;
            }

            foreach (var discharger in net.Dischargers)
            {
                var battery = batteryQuery.GetComponent(discharger.Owner);
                netNode.BatterySupplies.Add(battery.NetworkBattery.Id);
                battery.NetworkBattery.LinkedNetworkDischarging = netNode.Id;
            }
        }

        private void DoReconnectBasePowerNet<TNetType>(BasePowerNet<TNetType> net, PowerState.Network netNode)
            where TNetType : IBasePowerNet
        {
            foreach (var consumer in net.Consumers)
            {
                netNode.Loads.Add(consumer.NetworkLoad.Id);
                consumer.NetworkLoad.LinkedNetwork = netNode.Id;
            }

            foreach (var supplier in net.Suppliers)
            {
                netNode.Supplies.Add(supplier.NetworkSupply.Id);
                supplier.NetworkSupply.LinkedNetwork = netNode.Id;
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
    public readonly record struct PowerConsumerReceivedChanged(float ReceivedPower, float DrawRate)
    {
        public readonly float ReceivedPower = ReceivedPower;
        public readonly float DrawRate = DrawRate;
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

}
