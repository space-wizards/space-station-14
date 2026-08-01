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
        // DS14-start
        private readonly Dictionary<PowerState.NodeId, EntityUid> _apcReceiverLoads = new();
        private readonly Dictionary<PowerState.NodeId, EntityUid> _powerConsumerLoads = new();
        private readonly Dictionary<PowerState.NodeId, EntityUid> _networkBatteries = new();
        private readonly HashSet<EntityUid> _batteryBackedApcReceivers = new();
        private readonly HashSet<EntityUid> _forceApcReceiverUpdate = new();
        private readonly List<EntityUid> _apcReceiverUpdateBuffer = new();
        private readonly HashSet<EntityUid> _processedApcReceivers = new();
        private readonly List<EntityUid> _changedBatteryStorage = new();
        private readonly HashSet<EntityUid> _changedBatteryStorageSet = new();
        // DS14-end

        private EntityQuery<ApcPowerReceiverBatteryComponent> _apcBatteryQuery;
        private EntityQuery<BatteryComponent> _batteryQuery;

        private BatteryRampPegSolver _solver = new();

        public override void Initialize()
        {
            base.Initialize();

            _apcBatteryQuery = GetEntityQuery<ApcPowerReceiverBatteryComponent>();
            _batteryQuery = GetEntityQuery<BatteryComponent>();

            UpdatesAfter.Add(typeof(NodeGroupSystem));
            _solver = new(_cfg.GetCVar(CCVars.DebugPow3rDisableParallel));

            SubscribeLocalEvent<ApcPowerReceiverComponent, MapInitEvent>(ApcPowerReceiverMapInit);
            SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentInit>(ApcPowerReceiverInit);
            SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentShutdown>(ApcPowerReceiverShutdown);
            SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentRemove>(ApcPowerReceiverRemove);
            SubscribeLocalEvent<ApcPowerReceiverComponent, EntityPausedEvent>(ApcPowerReceiverPaused);
            SubscribeLocalEvent<ApcPowerReceiverComponent, EntityUnpausedEvent>(ApcPowerReceiverUnpaused);
            // DS14-start
            SubscribeLocalEvent<ApcPowerReceiverBatteryComponent, ComponentInit>(ApcPowerReceiverBatteryInit);
            SubscribeLocalEvent<ApcPowerReceiverBatteryComponent, ComponentShutdown>(ApcPowerReceiverBatteryShutdown);
            // DS14-end

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

        private void ApcPowerReceiverMapInit(Entity<ApcPowerReceiverComponent> ent, ref MapInitEvent args)
        {
            _appearance.SetData(ent, PowerDeviceVisuals.Powered, ent.Comp.Powered);
            QueueApcReceiverUpdate(ent.Owner); // DS14
        }

        private void ApcPowerReceiverInit(EntityUid uid, ApcPowerReceiverComponent component, ComponentInit args)
        {
            AllocLoad(component.NetworkLoad);
            // DS14-start
            _apcReceiverLoads[component.NetworkLoad.Id] = uid;
            QueueApcReceiverUpdate(uid);

            if (_apcBatteryQuery.HasComp(uid))
                _batteryBackedApcReceivers.Add(uid);
            // DS14-end
        }

        private void ApcPowerReceiverShutdown(EntityUid uid, ApcPowerReceiverComponent component,
            ComponentShutdown args)
        {
            // DS14-start
            _apcReceiverLoads.Remove(component.NetworkLoad.Id);
            _forceApcReceiverUpdate.Remove(uid);
            _batteryBackedApcReceivers.Remove(uid);
            _powerState.DetachLoad(component.NetworkLoad);
            // DS14-end
            _powerState.Loads.Free(component.NetworkLoad.Id);
        }

        private void ApcPowerReceiverRemove(EntityUid uid, ApcPowerReceiverComponent component, ComponentRemove args)
        {
            component.Provider?.RemoveReceiver(component);
        }

        private void ApcPowerReceiverPaused(
            EntityUid uid,
            ApcPowerReceiverComponent component,
            ref EntityPausedEvent args) // DS14
        {
            component.NetworkLoad.Paused = true;
            QueueApcReceiverUpdate(uid); // DS14
        }

        private void ApcPowerReceiverUnpaused(
            EntityUid uid,
            ApcPowerReceiverComponent component,
            ref EntityUnpausedEvent args) // DS14
        {
            component.NetworkLoad.Paused = false;
            QueueApcReceiverUpdate(uid); // DS14
        }

        private void ApcPowerReceiverBatteryInit(EntityUid uid, ApcPowerReceiverBatteryComponent component, ComponentInit args) // DS14
        {
            if (HasComp<ApcPowerReceiverComponent>(uid))
                _batteryBackedApcReceivers.Add(uid); // DS14
        }

        private void ApcPowerReceiverBatteryShutdown(EntityUid uid, ApcPowerReceiverBatteryComponent component, ComponentShutdown args) // DS14
        {
            _batteryBackedApcReceivers.Remove(uid); // DS14
        }

        private void BatteryInit(EntityUid uid, PowerNetworkBatteryComponent component, ComponentInit args)
        {
            AllocBattery(component.NetworkBattery);
            _networkBatteries[component.NetworkBattery.Id] = uid; // DS14
        }

        private void BatteryShutdown(EntityUid uid, PowerNetworkBatteryComponent component, ComponentShutdown args)
        {
            // DS14-start
            _networkBatteries.Remove(component.NetworkBattery.Id);
            _powerState.DetachBattery(component.NetworkBattery);
            // DS14-end
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
            _powerConsumerLoads[component.NetworkLoad.Id] = uid; // DS14
        }

        private void PowerConsumerShutdown(EntityUid uid, PowerConsumerComponent component, ComponentShutdown args)
        {
            // DS14-start
            _powerConsumerLoads.Remove(component.NetworkLoad.Id);
            _powerState.DetachLoad(component.NetworkLoad);
            // DS14-end
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
            _powerState.DetachSupply(component.NetworkSupply); // DS14
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
                CountSupplies = _powerState.Supplies.Count,
                // DS14-start
                CapacityBatteries = _powerState.Batteries.Capacity,
                CapacityLoads = _powerState.Loads.Capacity,
                CapacityNetworks = _powerState.Networks.Capacity,
                CapacitySupplies = _powerState.Supplies.Capacity
                // DS14-end
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

            if (_apcNetReconnectQueue.Count != 0 || _powerNetReconnectQueue.Count != 0)
                ReconnectNetworks();

            // Synchronize batteries
            RaiseLocalEvent(new NetworkBatteryPreSync());

            // Run power solver.
            _solver.Tick(frameTime, _powerState, _parMan);

            // DS14-start
            CollectChangedBatteryStorage();
            RaiseLocalEvent(new NetworkBatteryPostSync(_changedBatteryStorage));
            // DS14-end

            UpdateApcPowerReceiver(frameTime);
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

        private bool IsPoweredCalculate(ApcPowerReceiverComponent comp)
        {
            return !comp.PowerDisabled
                   && (!comp.NeedsPower
                       || MathHelper.CloseToPercent(comp.NetworkLoad.ReceivingPower,
                           comp.Load));
        }

        public override bool IsPoweredCalculate(SharedApcPowerReceiverComponent comp)
        {
            return IsPoweredCalculate((ApcPowerReceiverComponent)comp);
        }

        private void UpdateApcPowerReceiver(float frameTime)
        {
            _processedApcReceivers.Clear(); // DS14

            while (_powerState.TryDequeueChangedLoad(out var loadId)) // DS14
            {
                // DS14-start
                if (_apcReceiverLoads.TryGetValue(loadId, out var uid))
                    UpdateApcPowerReceiver(uid, frameTime);
                else if (_powerConsumerLoads.TryGetValue(loadId, out uid))
                    UpdatePowerConsumer(uid);
                // DS14-end
            }

            // DS14-start
            _apcReceiverUpdateBuffer.Clear();
            _apcReceiverUpdateBuffer.AddRange(_forceApcReceiverUpdate);
            _forceApcReceiverUpdate.Clear();
            // DS14-end

            foreach (var uid in _apcReceiverUpdateBuffer) // DS14
            {
                UpdateApcPowerReceiver(uid, frameTime); // DS14
            }

            // DS14-start
            _apcReceiverUpdateBuffer.Clear();
            _apcReceiverUpdateBuffer.AddRange(_batteryBackedApcReceivers);
            // DS14-end

            foreach (var uid in _apcReceiverUpdateBuffer) // DS14
            {
                UpdateApcPowerReceiver(uid, frameTime); // DS14
            }
        }

        private void UpdateApcPowerReceiver(EntityUid uid, float frameTime) // DS14
        {
            if (!_processedApcReceivers.Add(uid))
                return;

            if (!TryComp<ApcPowerReceiverComponent>(uid, out var apcReceiver))
                return;

            if (apcReceiver.NetworkLoad.LinkedNetwork == default)
                apcReceiver.NetworkLoad.SetReceivingPower(0f);

            if (apcReceiver.NetworkLoad.Paused)
                return;

            var powered = IsPoweredCalculate(apcReceiver);

            MetaDataComponent? metadata = null;

            if (_apcBatteryQuery.TryComp(uid, out var apcBattery) && _batteryQuery.TryComp(uid, out var battery))
            {
                metadata = MetaData(uid);
                if (Paused(uid, metadata))
                    return;

                apcReceiver.Load = apcBattery.IdleLoad;

                var requireBattery = !powered && !apcReceiver.PowerDisabled;

                if (requireBattery)
                {
                    _battery.ChangeCharge((uid, battery), -apcBattery.IdleLoad * frameTime);
                }
                else if (powered && !_battery.IsFull((uid, battery)))
                {
                    apcReceiver.Load += apcBattery.BatteryRechargeRate * apcBattery.BatteryRechargeEfficiency;
                    _battery.ChangeCharge((uid, battery), apcBattery.BatteryRechargeRate * frameTime);
                }

                var currentCharge = _battery.GetCharge((uid, battery));
                var enableBattery = requireBattery && currentCharge > 0;

                if (apcBattery.Enabled != enableBattery)
                {
                    apcBattery.Enabled = enableBattery;
                    Dirty(uid, apcBattery, metadata);

                    var apcBatteryEv = new ApcPowerReceiverBatteryChangedEvent(enableBattery);
                    RaiseLocalEvent(uid, ref apcBatteryEv);

                    _appearance.SetData(uid, PowerDeviceVisuals.BatteryPowered, enableBattery);
                }

                powered |= enableBattery;
            }

            if (apcReceiver.Powered == powered)
                return;

            metadata ??= MetaData(uid);
            if (Paused(uid, metadata))
                return;

            apcReceiver.Powered = powered;
            Dirty(uid, apcReceiver, metadata);

            var ev = new PowerChangedEvent(powered, apcReceiver.NetworkLoad.ReceivingPower);
            RaiseLocalEvent(uid, ref ev);
        }

        private void UpdatePowerConsumer(EntityUid uid) // DS14
        {
            if (!TryComp<PowerConsumerComponent>(uid, out var consumer))
                return;

            var newRecv = consumer.NetworkLoad.ReceivingPower;
            ref var lastRecv = ref consumer.LastReceived;
            if (MathHelper.CloseToPercent(lastRecv, newRecv))
                return;

            lastRecv = newRecv;
            var msg = new PowerConsumerReceivedChanged(newRecv, consumer.DrawRate);
            RaiseLocalEvent(uid, ref msg);
        }

        private void UpdateNetworkBattery()
        {
            while (_powerState.TryDequeueChangedBatterySupply(out var batteryId)) // DS14
            {
                // DS14-start
                if (_networkBatteries.TryGetValue(batteryId, out var uid))
                    UpdateNetworkBattery(uid);
                // DS14-end
            }
        }

        private void UpdateNetworkBattery(EntityUid uid) // DS14
        {
            if (!TryComp<PowerNetworkBatteryComponent>(uid, out var powerNetBattery))
                return;

            var lastSupply = powerNetBattery.LastSupply;
            var currentSupply = powerNetBattery.CurrentSupply;

            if (lastSupply == currentSupply)
                return;

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

        private void CollectChangedBatteryStorage() // DS14
        {
            // DS14-start
            _changedBatteryStorage.Clear();
            _changedBatteryStorageSet.Clear();
            // DS14-end

            while (_powerState.TryDequeueChangedBatteryStorage(out var batteryId)) // DS14
            {
                if (!_networkBatteries.TryGetValue(batteryId, out var uid)) // DS14
                {
                    continue; // DS14
                }

                if (_changedBatteryStorageSet.Add(uid)) // DS14
                {
                    _changedBatteryStorage.Add(uid); // DS14
                }
            }
        }

        public void QueueApcReceiverUpdate(EntityUid uid) // DS14
        {
            _forceApcReceiverUpdate.Add(uid); // DS14
        }

        private void AllocLoad(PowerState.Load load)
        {
            _powerState.Loads.Allocate(out load.Id) = load;
            _powerState.AttachLoad(load); // DS14
        }

        private void AllocSupply(PowerState.Supply supply)
        {
            _powerState.Supplies.Allocate(out supply.Id) = supply;
            _powerState.AttachSupply(supply); // DS14
        }

        private void AllocBattery(PowerState.Battery battery)
        {
            _powerState.Batteries.Allocate(out battery.Id) = battery;
            _powerState.AttachBattery(battery); // DS14
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
            // DS14-start
            netNode.LoadDemandDirty = true;
            netNode.BatteryLoadDirty = true;
            netNode.BatterySupplyDirty = true;
            netNode.SupplyDirty = true;
            netNode.LastLoadSupplyRatio = float.NaN;
            // DS14-end

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
            // DS14-start
            netNode.LoadDemandDirty = true;
            netNode.BatteryLoadDirty = true;
            netNode.BatterySupplyDirty = true;
            netNode.SupplyDirty = true;
            netNode.LastLoadSupplyRatio = float.NaN;
            // DS14-end

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
        // DS14-start
        public readonly IReadOnlyList<EntityUid>? ChangedBatteries;

        public NetworkBatteryPostSync(IReadOnlyList<EntityUid>? changedBatteries)
        {
            ChangedBatteries = changedBatteries;
        }
        // DS14-end
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
        // DS14-start
        public int CapacityNetworks;
        public int CapacityLoads;
        public int CapacitySupplies;
        public int CapacityBatteries;
        // DS14-end
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
