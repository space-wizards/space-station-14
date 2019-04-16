using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using System;
using System.Collections.Generic;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Master class for group of <see cref="PowerTransferComponent"/>, takes in and distributes power via nodes
    /// </summary>
    public class Powernet
    {
        public Powernet()
        {
            var EntitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
            var powerSystem = EntitySystemManager.GetEntitySystem<PowerSystem>();
            powerSystem.Powernets.Add(this);
            Uid = powerSystem.NewUid();
        }

        /// <summary>
        ///     Unique identifier per powernet, used for debugging mostly.
        /// </summary>
        [ViewVariables]
        public int Uid { get; }

        /// <summary>
        ///     The entities that make up the powernet's physical location and allow powernet connection
        /// </summary>
        public readonly List<PowerTransferComponent> WireList = new List<PowerTransferComponent>();

        /// <summary>
        ///     Entities that connect directly to the powernet through <see cref="PowerTransferComponent" /> above to add power or add power load
        /// </summary>
        public readonly List<PowerNodeComponent> NodeList = new List<PowerNodeComponent>();

        /// <summary>
        ///     Subset of nodelist that adds a continuous power supply to the network
        /// </summary>
        private readonly Dictionary<PowerGeneratorComponent, float> GeneratorList =
            new Dictionary<PowerGeneratorComponent, float>();

        [ViewVariables]
        public int GeneratorCount => GeneratorList.Count;

        /// <summary>
        ///     Subset of nodelist that draw power, stores information on current continuous powernet load
        /// </summary>
        private readonly SortedSet<PowerDeviceComponent> DeviceLoadList =
            new SortedSet<PowerDeviceComponent>(new DevicePriorityCompare());

        [ViewVariables]
        public int DeviceCount => DeviceLoadList.Count;

        /// <summary>
        ///     All the devices that have been depowered by this powernet or depowered prior to being absorted into this powernet
        /// </summary>
        private readonly List<PowerDeviceComponent> DepoweredDevices = new List<PowerDeviceComponent>();

        /// <summary>
        ///     A list of the energy storage components that will feed the powernet if necessary, and if there is enough power feed itself
        /// </summary>
        private readonly List<PowerStorageNetComponent> PowerStorageSupplierList = new List<PowerStorageNetComponent>();

        [ViewVariables]
        public int PowerStorageSupplierCount => PowerStorageSupplierList.Count;

        /// <summary>
        ///     A list of energy storage components that will never feed the powernet, will try to draw energy to feed themselves if possible
        /// </summary>
        private readonly List<PowerStorageNetComponent> PowerStorageConsumerList = new List<PowerStorageNetComponent>();

        [ViewVariables]
        public int PowerStorageConsumerCount => PowerStorageConsumerList.Count;

        /// <summary>
        ///     Static counter of all continuous load placed from devices on this power network.
        ///     In Watts.
        /// </summary>
        [ViewVariables]
        public float Load { get; private set; } = 0;

        /// <summary>
        ///     Static counter of all continuous supply from generators on this power network.
        ///     In Watts.
        /// </summary>
        [ViewVariables]
        public float Supply { get; private set; } = 0;

        /// <summary>
        ///     Variable that causes powernet to be regenerated from its wires during the next update cycle.
        /// </summary>
        [ViewVariables]
        public bool Dirty { get; set; } = false;

        // These are stats for power monitoring equipment such as APCs.

        /// <summary>
        ///     The total supply that was available to us last tick.
        ///     This does not mean it was used.
        /// </summary>
        [ViewVariables]
        public float LastTotalAvailable { get; private set; }

        /// <summary>
        ///     The total power drawn last tick.
        ///     This is how much power was actually, in practice, drawn.
        ///     Not how much SHOULD have been drawn.
        ///     If avail &lt; demand, this will be just &lt;= than the actual avail
        ///     (e.g. if all machines need 100 W but there's 20 W excess, the 20 W will be avail but not drawn.)
        /// </summary>
        [ViewVariables]
        public float LastTotalDraw { get; private set; }

        /// <summary>
        ///     The amount of power that was demanded last tick.
        ///     This does not mean it was full filled in practice.
        ///     This does not include the demand from storage suppliers until the suppliers are actually capable of drawing power.
        ///     As such, this will quite abruptly shoot up if available rises to cover supplier charge demand too.
        /// </summary>
        /// <seealso cref="LastTotalDemandWithSuppliers"/>
        [ViewVariables]
        public float LastTotalDemand { get; private set; }

        /// <summary>
        ///     The amount of power that was demanded last tick, ALWAYS including storage supplier draw.
        ///     This does not mean it was full filled in practice.
        ///     See <see cref="LastTotalDemand"/> for the difference.
        /// </summary>
        [ViewVariables]
        public float LastTotalDemandWithSuppliers { get; private set; }

        /// <summary>
        ///     The amount of power that we are lacking to properly power everything (excluding storage supplier charging).
        /// </summary>
        [ViewVariables]
        public float Lack => Math.Max(0, LastTotalDemand - LastTotalAvailable);

        /// <summary>
        ///     The total amount of power that wasn't used last tick.
        ///     This does not necessarily mean it went to waste, unused supply from storage is also counted.
        ///     It is ALSO not implied that if this is &gt; 0, that we have sufficient power for everything.
        ///     See the doc comment on <see cref="LastTotalDraw"/>.
        /// </summary>
        [ViewVariables]
        public float Excess => Math.Max(0, LastTotalAvailable - LastTotalDraw);

        public void Update(float frameTime)
        {
            // The amount of energy that is supplied from generators that do not care if it's used or not.
            var activeSupply = Supply * frameTime;
            // The total load we need to fill for machines.
            var activeLoad = Load * frameTime;

            // The total load from storage consumers (batteries that do not supply like an SMES)
            float storageConsumerDemand = 0;
            foreach (var supply in PowerStorageConsumerList)
            {
                storageConsumerDemand += supply.RequestCharge(frameTime);
            }

            // The total supply from storage suppliers.
            float storageSupply = 0;
            // The total load from storage suppliers (batteries that DO supply like an SMES)
            float storageSupplierDemand = 0;
            foreach (var supply in PowerStorageSupplierList)
            {
                storageSupply += supply.AvailableCharge(frameTime);
                storageSupplierDemand += supply.RequestCharge(frameTime);
            }

            LastTotalAvailable = (storageSupply + activeSupply) / frameTime;
            LastTotalDemandWithSuppliers = (activeLoad + storageConsumerDemand + storageSupplierDemand) / frameTime;

            // The happy case.
            // If we have enough power to feed all load and storage demand, then feed everything
            if (activeSupply > activeLoad + storageConsumerDemand + storageSupplierDemand)
            {
                PowerAllDevices();
                ChargeStorageConsumers(frameTime);
                ChargeStorageSuppliers(frameTime);
                LastTotalDraw = LastTotalDemand = LastTotalDemandWithSuppliers;
                return;
            }

            LastTotalDemand = (activeLoad + storageConsumerDemand) / frameTime;

            // We don't have enough power for the storage powernet suppliers, ignore powering them
            // TODO: This is technically incorrect, it's totally possible to power *some* suppliers here,
            // just not all.
            if (activeSupply > activeLoad + storageConsumerDemand)
            {
                PowerAllDevices();
                ChargeStorageConsumers(frameTime);
                LastTotalDraw = LastTotalDemand;
                return;
            }

            // The complex case: There is too little power to power everything without using storage suppliers (SMES).
            // We have to keep track of power draw as to not incorrectly detract too much from storage suppliers.

            // Calculate the total potential supply, then go through every normal load and detract.
            var totalRemaining = activeSupply + storageSupply;
            foreach (var device in DeviceLoadList)
            {
                var deviceLoad = device.Load * frameTime;
                if (deviceLoad > totalRemaining)
                {
                    device.ExternalPowered = false;
                    DepoweredDevices.Add(device);
                }
                else
                {
                    totalRemaining -= deviceLoad;
                    if (!device.ExternalPowered)
                    {
                        DepoweredDevices.Remove(device);
                        device.ExternalPowered = true;
                    }
                }
            }

            if (totalRemaining > 0)
            {
                // What we have left (if any) goes into storage consumers.
                foreach (var consumer in PowerStorageConsumerList)
                {
                    if (totalRemaining < 0)
                    {
                        break;
                    }

                    var demand = consumer.RequestCharge(frameTime);
                    if (demand == 0)
                    {
                        continue;
                    }

                    var taken = Math.Min(demand, totalRemaining);
                    totalRemaining -= taken;
                    consumer.AddCharge(taken);
                }
            }

            LastTotalDraw = (activeSupply + storageSupply - totalRemaining) / frameTime;

            // activeSupply is free to use, but storageSupply is not.
            // Calculate how much of storageSupply, and deduct it from the storage suppliers.
            var supplierUsed = storageSupply - totalRemaining;

            // And deduct!
            foreach (var supplier in PowerStorageSupplierList)
            {
                var load = supplier.AvailableCharge(frameTime);
                if (load == 0)
                {
                    continue;
                }

                var added = Math.Min(load, supplierUsed);
                supplierUsed -= added;
                supplier.DeductCharge(added);
                if (supplierUsed <= 0)
                {
                    return;
                }
            }
        }

        private void PowerAllDevices()
        {
            foreach (var device in DepoweredDevices)
            {
                device.ExternalPowered = true;
            }

            DepoweredDevices.Clear();
        }

        private void ChargeStorageConsumers(float frametime)
        {
            foreach (var storage in PowerStorageConsumerList)
            {
                storage.ChargePowerTick(frametime);
            }
        }

        private void ChargeStorageSuppliers(float frametime)
        {
            foreach (var storage in PowerStorageSupplierList)
            {
                storage.ChargePowerTick(frametime);
            }
        }

        /// <summary>
        /// Kills a powernet after it is marked dirty and its component have already been regenerated by the powernet system
        /// </summary>
        public void DirtyKill()
        {
            WireList.Clear();
            while (NodeList.Count != 0)
            {
                NodeList[0].DisconnectFromPowernet();
            }

            GeneratorList.Clear();
            DeviceLoadList.Clear();
            DepoweredDevices.Clear();
            PowerStorageSupplierList.Clear();
            PowerStorageConsumerList.Clear();

            RemoveFromSystem();
        }

        /// <summary>
        /// Combines two powernets when they connect via powertransfer components
        /// </summary>
        public void MergePowernets(Powernet toMerge)
        {
            //TODO: load balance reconciliation between powernets on merge tick here

            foreach (var wire in toMerge.WireList)
            {
                wire.Parent = this;
            }

            WireList.AddRange(toMerge.WireList);
            toMerge.WireList.Clear();

            foreach (var node in toMerge.NodeList)
            {
                node.Parent = this;
            }

            NodeList.AddRange(toMerge.NodeList);
            toMerge.NodeList.Clear();

            foreach (var generator in toMerge.GeneratorList)
            {
                GeneratorList.Add(generator.Key, generator.Value);
            }

            Supply += toMerge.Supply;
            toMerge.Supply = 0;
            toMerge.GeneratorList.Clear();

            foreach (var device in toMerge.DeviceLoadList)
            {
                DeviceLoadList.Add(device);
            }

            Load += toMerge.Load;
            toMerge.Load = 0;
            toMerge.DeviceLoadList.Clear();

            DepoweredDevices.AddRange(toMerge.DepoweredDevices);
            toMerge.DepoweredDevices.Clear();

            PowerStorageSupplierList.AddRange(toMerge.PowerStorageSupplierList);
            toMerge.PowerStorageSupplierList.Clear();

            PowerStorageConsumerList.AddRange(toMerge.PowerStorageConsumerList);
            toMerge.PowerStorageConsumerList.Clear();

            toMerge.RemoveFromSystem();
        }

        /// <summary>
        /// Removes reference from the powernets list on the powernet system
        /// </summary>
        private void RemoveFromSystem()
        {
            var EntitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
            EntitySystemManager.GetEntitySystem<PowerSystem>().Powernets.Remove(this);
        }

        #region Registration

        /// <summary>
        /// Register a continuous load from a device connected to the powernet
        /// </summary>
        public void AddDevice(PowerDeviceComponent device)
        {
            DeviceLoadList.Add(device);
            Load += device.Load;
            if (!device.Powered)
                DepoweredDevices.Add(device);
        }

        /// <summary>
        /// Update one of the loads from a deviceconnected to the powernet
        /// </summary>
        public void UpdateDevice(PowerDeviceComponent device, float oldLoad)
        {
            if (DeviceLoadList.Contains(device))
            {
                Load -= oldLoad;
                Load += device.Load;
            }
        }

        /// <summary>
        ///     Returns whether or not a power device is in this powernet's load list.
        /// </summary>
        /// <param name="device">The device to check for.</param>
        /// <returns>True if the device is in the load list, false otherwise.</returns>
        public bool HasDevice(PowerDeviceComponent device)
        {
            return DeviceLoadList.Contains(device);
        }

        /// <summary>
        ///     Remove a continuous load from a device connected to the powernet
        /// </summary>
        public void RemoveDevice(PowerDeviceComponent device)
        {
            if (DeviceLoadList.Contains(device))
            {
                Load -= device.Load;
                DeviceLoadList.Remove(device);
                if (DepoweredDevices.Contains(device))
                    DepoweredDevices.Remove(device);
            }
            else
            {
                Logger.WarningS("power", "We tried to remove device {0} twice from {1}, somehow.", device.Owner, this);
            }
        }

        /// <summary>
        /// Register a power supply from a generator connected to the powernet
        /// </summary>
        public void AddGenerator(PowerGeneratorComponent generator)
        {
            GeneratorList.Add(generator, generator.Supply);
            Supply += generator.Supply;
        }

        /// <summary>
        /// Update the value supplied from a generator connected to the powernet
        /// </summary>
        public void UpdateGenerator(PowerGeneratorComponent generator)
        {
            if (GeneratorList.ContainsKey(generator))
            {
                Supply -= GeneratorList[generator];
                GeneratorList[generator] = generator.Supply;
                Supply += generator.Supply;
            }
        }

        /// <summary>
        /// Remove a power supply from a generator connected to the powernet
        /// </summary>
        public void RemoveGenerator(PowerGeneratorComponent generator)
        {
            if (GeneratorList.ContainsKey(generator))
            {
                Supply -= GeneratorList[generator];
                GeneratorList.Remove(generator);
            }
            else
            {
                Logger.WarningS("power", "We tried to remove generator {0} twice from {1}, somehow.", generator.Owner,
                    this);
            }
        }

        /// <summary>
        /// Register a power supply from a generator connected to the powernet
        /// </summary>
        public void AddPowerStorage(PowerStorageNetComponent storage)
        {
            if (storage.ChargePowernet)
                PowerStorageSupplierList.Add(storage);
            else
                PowerStorageConsumerList.Add(storage);
        }

        //How do I even call this? TODO: fix
        public void UpdateStorageType(PowerStorageNetComponent storage)
        {
            //If our chargepowernet settings change we need to tell the powernet of this new setting and remove traces of our old setting
            if (PowerStorageSupplierList.Contains(storage))
                PowerStorageSupplierList.Remove(storage);
            if (PowerStorageConsumerList.Contains(storage))
                PowerStorageConsumerList.Remove(storage);

            //Apply new setting
            if (storage.ChargePowernet)
                PowerStorageSupplierList.Add(storage);
            else
                PowerStorageConsumerList.Add(storage);
        }

        /// <summary>
        /// Remove a power supply from a generator connected to the powernet
        /// </summary>
        public void RemovePowerStorage(PowerStorageNetComponent storage)
        {
            if (PowerStorageSupplierList.Contains(storage))
            {
                PowerStorageSupplierList.Remove(storage);
            }

            if (PowerStorageConsumerList.Contains(storage))
            {
                PowerStorageSupplierList.Remove(storage);
            }
        }

        #endregion Registration

        public override string ToString()
        {
            return $"Powernet {Uid}";
        }

        /// <summary>
        ///     Priority that a device will receive power if powernet cannot supply every device
        /// </summary>
        public enum Priority
        {
            Necessary = 0,
            High = 1,
            Medium = 2,
            Low = 3,
            Provider = 4,
            Unnecessary = 5
        }

        /// <summary>
        ///     Comparer that keeps the device dictionary sorted by powernet priority
        /// </summary>
        public class DevicePriorityCompare : IComparer<PowerDeviceComponent>
        {
            public int Compare(PowerDeviceComponent x, PowerDeviceComponent y)
            {
                int compare = y.Priority.CompareTo(x.Priority);

                //If the comparer returns 0 sortedset will believe it is a duplicate and return 0, so return 1 instead
                if (compare == 0)
                {
                    return y.Owner.Uid.CompareTo(x.Owner.Uid);
                }

                return compare;
            }
        }
    }
}
