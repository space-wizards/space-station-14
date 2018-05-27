using Content.Shared.GameObjects.EntitySystems;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Master class for group of powertransfercomponents, takes in and distributes power via nodes
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
        private readonly Dictionary<PowerGeneratorComponent, float> GeneratorList = new Dictionary<PowerGeneratorComponent, float>();

        /// <summary>
        ///     Subset of nodelist that draw power, stores information on current continuous powernet load
        /// </summary>
        private readonly SortedSet<PowerDeviceComponent> DeviceLoadList = new SortedSet<PowerDeviceComponent>(new DevicePriorityCompare());

        /// <summary>
        ///     All the devices that have been depowered by this powernet or depowered prior to being absorted into this powernet
        /// </summary>
        private readonly List<PowerDeviceComponent> DepoweredDevices = new List<PowerDeviceComponent>();

        /// <summary>
        ///     A list of the energy storage components that will feed the powernet if necessary, and if there is enough power feed itself
        /// </summary>
        private readonly List<PowerStorageComponent> PowerStorageSupplierList = new List<PowerStorageComponent>();

        /// <summary>
        ///     A list of energy storage components that will never feed the powernet, will try to draw energy to feed themselves if possible
        /// </summary>
        private readonly List<PowerStorageComponent> PowerStorageConsumerList = new List<PowerStorageComponent>();

        /// <summary>
        ///     Static counter of all continuous load placed from devices on this power network.
        ///     In Watts.
        /// </summary>
        public float Load { get; private set; } = 0;

        /// <summary>
        ///     Static counter of all continiuous supply from generators on this power network.
        ///     In Watts.
        /// </summary>
        public float Supply { get; private set; } = 0;

        /// <summary>
        ///     Variable that causes powernet to be regenerated from its wires during the next update cycle.
        /// </summary>
        public bool Dirty { get; set; } = false;

        public void Update(float frameTime)
        {
            float activesupply = Supply * frameTime;
            float activeload = Load * frameTime;

            float storageconsumerdemand = 0;

            foreach (var supply in PowerStorageConsumerList)
            {
                storageconsumerdemand += supply.RequestCharge(frameTime);
            }

            float storagesupply = 0;
            float storagesupplierdemand = 0;

            foreach (var supply in PowerStorageSupplierList)
            {
                storagesupply += supply.AvailableCharge(frameTime);
                storagesupplierdemand += supply.RequestCharge(frameTime);
            }


            //If we have enough power to feed all load and storage demand, then feed everything
            if (activesupply > activeload + storageconsumerdemand + storagesupplierdemand)
            {
                PowerAllDevices();
                ChargeStorageConsumers(frameTime);
                ChargeStorageSuppliers(frameTime);
                return;
            }
            //We don't have enough power for the storage powernet suppliers, ignore powering them
            else if (activesupply > activeload + storageconsumerdemand)
            {
                PowerAllDevices();
                ChargeStorageConsumers(frameTime);
                return;
            }


            float totalRemaining = activesupply + storagesupply;

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

            // What we have left goes into storage consumers.
            foreach (var consumer in PowerStorageConsumerList)
            {
                if (totalRemaining < 0)
                {
                    break;
                }
                var demand = consumer.RequestCharge(frameTime);
                var taken = Math.Min(demand, totalRemaining);
                totalRemaining -= taken;
                consumer.AddCharge(taken);
            }

            var supplierUsed = storagesupply - totalRemaining;

            foreach (var supplier in PowerStorageSupplierList)
            {
                var load = supplier.AvailableCharge(frameTime);
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
            toMerge.GeneratorList.Clear();

            foreach (var device in toMerge.DeviceLoadList)
            {
                DeviceLoadList.Add(device);
            }
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
        /// Remove a continuous load from a device connected to the powernet
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
                var name = device.Owner.Prototype.Name;
                Logger.Info("We tried to remove a device twice from the same powernet somehow, prototype {0}", name);
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
                var name = generator.Owner.Prototype.Name;
                Logger.Info(String.Format("We tried to remove a device twice from the same power somehow, prototype {1}", name));
            }
        }

        /// <summary>
        /// Register a power supply from a generator connected to the powernet
        /// </summary>
        public void AddPowerStorage(PowerStorageComponent storage)
        {
            if (storage.ChargePowernet)
                PowerStorageSupplierList.Add(storage);
            else
                PowerStorageConsumerList.Add(storage);
        }

        //How do I even call this? TODO: fix
        public void UpdateStorageType(PowerStorageComponent storage)
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
        public void RemovePowerStorage(PowerStorageComponent storage)
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

        /// <summary>
        ///     Priority that a device will receive power if powernet cannot supply every device
        /// </summary>
        public enum Priority
        {
            Necessary,
            High,
            Medium,
            Low,
            Provider,
            Unnecessary
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
                if (compare == 0 && !x.Equals(y))
                {
                    return 1;
                }
                return compare;
            }
        }
    }
}
