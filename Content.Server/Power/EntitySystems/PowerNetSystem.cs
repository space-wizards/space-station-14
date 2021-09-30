using System.Collections.Generic;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Power.EntitySystems
{
    /// <summary>
    ///     Manages power networks, power state, and all power components.
    /// </summary>
    [UsedImplicitly]
    public class PowerNetSystem : EntitySystem
    {
        private readonly PowerState _powerState = new();
        private readonly HashSet<PowerNet> _powerNetReconnectQueue = new();
        private readonly HashSet<ApcNet> _apcNetReconnectQueue = new();

        private readonly Dictionary<PowerNetworkBatteryComponent, float> _lastSupply = new();

        private readonly BatteryRampPegSolver _solver = new();

        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(NodeGroupSystem));

            SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentInit>(ApcPowerReceiverInit);
            SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentShutdown>(ApcPowerReceiverShutdown);
            SubscribeLocalEvent<ApcPowerReceiverComponent, EntityPausedEvent>(ApcPowerReceiverPaused);
            SubscribeLocalEvent<PowerNetworkBatteryComponent, ComponentInit>(BatteryInit);
            SubscribeLocalEvent<PowerNetworkBatteryComponent, ComponentShutdown>(BatteryShutdown);
            SubscribeLocalEvent<PowerNetworkBatteryComponent, EntityPausedEvent>(BatteryPaused);
            SubscribeLocalEvent<PowerConsumerComponent, ComponentInit>(PowerConsumerInit);
            SubscribeLocalEvent<PowerConsumerComponent, ComponentShutdown>(PowerConsumerShutdown);
            SubscribeLocalEvent<PowerConsumerComponent, EntityPausedEvent>(PowerConsumerPaused);
            SubscribeLocalEvent<PowerSupplierComponent, ComponentInit>(PowerSupplierInit);
            SubscribeLocalEvent<PowerSupplierComponent, ComponentShutdown>(PowerSupplierShutdown);
            SubscribeLocalEvent<PowerSupplierComponent, EntityPausedEvent>(PowerSupplierPaused);
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

        private static void ApcPowerReceiverPaused(
            EntityUid uid,
            ApcPowerReceiverComponent component,
            EntityPausedEvent args)
        {
            component.NetworkLoad.Paused = args.Paused;
        }

        private void BatteryInit(EntityUid uid, PowerNetworkBatteryComponent component, ComponentInit args)
        {
            AllocBattery(component.NetworkBattery);
        }

        private void BatteryShutdown(EntityUid uid, PowerNetworkBatteryComponent component, ComponentShutdown args)
        {
            _powerState.Batteries.Free(component.NetworkBattery.Id);
        }

        private static void BatteryPaused(EntityUid uid, PowerNetworkBatteryComponent component, EntityPausedEvent args)
        {
            component.NetworkBattery.Paused = args.Paused;
        }

        private void PowerConsumerInit(EntityUid uid, PowerConsumerComponent component, ComponentInit args)
        {
            AllocLoad(component.NetworkLoad);
        }

        private void PowerConsumerShutdown(EntityUid uid, PowerConsumerComponent component, ComponentShutdown args)
        {
            _powerState.Loads.Free(component.NetworkLoad.Id);
        }

        private static void PowerConsumerPaused(EntityUid uid, PowerConsumerComponent component, EntityPausedEvent args)
        {
            component.NetworkLoad.Paused = args.Paused;
        }

        private void PowerSupplierInit(EntityUid uid, PowerSupplierComponent component, ComponentInit args)
        {
            AllocSupply(component.NetworkSupply);
        }

        private void PowerSupplierShutdown(EntityUid uid, PowerSupplierComponent component, ComponentShutdown args)
        {
            _powerState.Supplies.Free(component.NetworkSupply.Id);
        }

        private static void PowerSupplierPaused(EntityUid uid, PowerSupplierComponent component, EntityPausedEvent args)
        {
            component.NetworkSupply.Paused = args.Paused;
        }

        public void InitPowerNet(PowerNet powerNet)
        {
            AllocNetwork(powerNet.NetworkNode);
        }

        public void DestroyPowerNet(PowerNet powerNet)
        {
            _powerState.Networks.Free(powerNet.NetworkNode.Id);
        }

        public void QueueReconnectPowerNet(PowerNet powerNet)
        {
            _powerNetReconnectQueue.Add(powerNet);
        }

        public void InitApcNet(ApcNet apcNet)
        {
            AllocNetwork(apcNet.NetworkNode);
        }

        public void DestroyApcNet(ApcNet apcNet)
        {
            _powerState.Networks.Free(apcNet.NetworkNode.Id);
        }

        public void QueueReconnectApcNet(ApcNet apcNet)
        {
            _apcNetReconnectQueue.Add(apcNet);
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

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Setup for events.
            {
                foreach (var powerNetBattery in EntityManager.EntityQuery<PowerNetworkBatteryComponent>())
                {
                    _lastSupply[powerNetBattery] = powerNetBattery.CurrentSupply;
                }
            }

            // Reconnect networks.
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

            // Synchronize batteries
            RaiseLocalEvent(new NetworkBatteryPreSync());

            // Run power solver.
            _solver.Tick(frameTime, _powerState);

            // Synchronize batteries, the other way around.
            RaiseLocalEvent(new NetworkBatteryPostSync());

            // Send events where necessary.
            {
                foreach (var apcReceiver in EntityManager.EntityQuery<ApcPowerReceiverComponent>())
                {
                    var recv = apcReceiver.NetworkLoad.ReceivingPower;
                    ref var last = ref apcReceiver.LastPowerReceived;

                    if (!MathHelper.CloseToPercent(recv, last))
                    {
                        last = recv;
                        apcReceiver.ApcPowerChanged();
                    }
                }

                foreach (var consumer in EntityManager.EntityQuery<PowerConsumerComponent>())
                {
                    var newRecv = consumer.NetworkLoad.ReceivingPower;
                    ref var lastRecv = ref consumer.LastReceived;
                    if (!MathHelper.CloseToPercent(lastRecv, newRecv))
                    {
                        lastRecv = newRecv;
                        var msg = new PowerConsumerReceivedChanged(newRecv, consumer.DrawRate);
                        RaiseLocalEvent(consumer.Owner.Uid, msg);
                    }
                }

                foreach (var powerNetBattery in EntityManager.EntityQuery<PowerNetworkBatteryComponent>())
                {
                    if (!_lastSupply.TryGetValue(powerNetBattery, out var lastPowerSupply))
                    {
                        lastPowerSupply = 0f;
                    }

                    var currentSupply = powerNetBattery.CurrentSupply;

                    if (lastPowerSupply == 0f && currentSupply != 0f)
                    {
                        RaiseLocalEvent(powerNetBattery.Owner.Uid, new PowerNetBatterySupplyEvent {Supply = true});
                    }
                    else if (lastPowerSupply > 0f && currentSupply == 0f)
                    {
                        RaiseLocalEvent(powerNetBattery.Owner.Uid, new PowerNetBatterySupplyEvent {Supply = false});
                    }
                }

                _lastSupply.Clear();
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

        private static void DoReconnectApcNet(ApcNet net)
        {
            var netNode = net.NetworkNode;

            netNode.Loads.Clear();
            netNode.BatteriesDischarging.Clear();
            netNode.BatteriesCharging.Clear();
            netNode.Supplies.Clear();

            foreach (var provider in net.Providers)
            {
                foreach (var receiver in provider.LinkedReceivers)
                {
                    netNode.Loads.Add(receiver.NetworkLoad.Id);
                    receiver.NetworkLoad.LinkedNetwork = netNode.Id;
                }
            }

            foreach (var apc in net.Apcs)
            {
                var netBattery = apc.Owner.GetComponent<PowerNetworkBatteryComponent>();
                netNode.BatteriesDischarging.Add(netBattery.NetworkBattery.Id);
                netBattery.NetworkBattery.LinkedNetworkDischarging = netNode.Id;
            }
        }

        private static void DoReconnectPowerNet(PowerNet net)
        {
            var netNode = net.NetworkNode;

            netNode.Loads.Clear();
            netNode.Supplies.Clear();
            netNode.BatteriesCharging.Clear();
            netNode.BatteriesDischarging.Clear();

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

            foreach (var charger in net.Chargers)
            {
                var battery = charger.Owner.GetComponent<PowerNetworkBatteryComponent>();
                netNode.BatteriesCharging.Add(battery.NetworkBattery.Id);
                battery.NetworkBattery.LinkedNetworkCharging = netNode.Id;
            }

            foreach (var discharger in net.Dischargers)
            {
                var battery = discharger.Owner.GetComponent<PowerNetworkBatteryComponent>();
                netNode.BatteriesDischarging.Add(battery.NetworkBattery.Id);
                battery.NetworkBattery.LinkedNetworkDischarging = netNode.Id;
            }
        }
    }

    /// <summary>
    ///     Raised before power network simulation happens, to synchronize battery state from
    ///     components like <see cref="BatteryComponent"/> into <see cref="PowerNetworkBatteryComponent"/>.
    /// </summary>
    public struct NetworkBatteryPreSync
    {
    }

    /// <summary>
    ///     Raised after power network simulation happens, to synchronize battery charge changes from
    ///     <see cref="PowerNetworkBatteryComponent"/> to components like <see cref="BatteryComponent"/>.
    /// </summary>
    public struct NetworkBatteryPostSync
    {
    }

    /// <summary>
    ///     Raised when the amount of receiving power on a <see cref="PowerConsumerComponent"/> changes.
    /// </summary>
    public sealed class PowerConsumerReceivedChanged : EntityEventArgs
    {
        public float ReceivedPower { get; }
        public float DrawRate { get; }

        public PowerConsumerReceivedChanged(float receivedPower, float drawRate)
        {
            ReceivedPower = receivedPower;
            DrawRate = drawRate;
        }
    }

    /// <summary>
    /// Raised whenever a <see cref="PowerNetworkBatteryComponent"/> changes from / to 0 CurrentSupply.
    /// </summary>
    public sealed class PowerNetBatterySupplyEvent : EntityEventArgs
    {
        public bool Supply { get; init;  }
    }

    public struct PowerStatistics
    {
        public int CountNetworks;
        public int CountLoads;
        public int CountSupplies;
        public int CountBatteries;
    }
}
