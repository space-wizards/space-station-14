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
            EntitySystemManager.GetEntitySystem<PowerSystem>().Powernets.Add(this);
        }

        /// <summary>
        /// The entities that make up the powernet's physical location and allow powernet connection
        /// </summary>
        public List<PowerTransferComponent> Wirelist { get; set; } = new List<PowerTransferComponent>();

        /// <summary>
        /// Entities that connect directly to the powernet through PTC above to add power or add power load
        /// </summary>
        public List<PowerNodeComponent> Nodelist { get; set; } = new List<PowerNodeComponent>();

        /// <summary>
        /// Subset of nodelist that adds a continuous power supply to the network
        /// </summary>
        public Dictionary<PowerGeneratorComponent, float> Generatorlist { get; set; } = new Dictionary<PowerGeneratorComponent, float>();

        /// <summary>
        /// Subset of nodelist that draw power, stores information on current continuous powernet load
        /// </summary>
        public SortedSet<PowerDeviceComponent> Deviceloadlist { get; set; } = new SortedSet<PowerDeviceComponent>(new DevicePriorityCompare());

        /// <summary>
        /// Comparer that keeps the device dictionary sorted by powernet priority
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

        /// <summary>
        /// Priority that a device will receive power if powernet cannot supply every device
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
        /// All the devices that have been depowered by this powernet or depowered prior to being absorted into this powernet
        /// </summary>
        public List<PowerDeviceComponent> DepoweredDevices { get; set; } = new List<PowerDeviceComponent>();

        /// <summary>
        /// A list of the energy storage components that will feed the powernet if necessary, and if there is enough power feed itself
        /// </summary>
        public List<PowerStorageComponent> PowerStorageSupplierlist { get; set; } = new List<PowerStorageComponent>();

        /// <summary>
        /// A list of energy storage components that will never feed the powernet, will try to draw energy to feed themselves if possible
        /// </summary>
        public List<PowerStorageComponent> PowerStorageConsumerlist { get; set; } = new List<PowerStorageComponent>();

        /// <summary>
        /// Static counter of all continuous load placed from devices on this power network
        /// </summary>
        public float Load { get; private set; } = 0;

        /// <summary>
        /// Static counter of all continiuous supply from generators on this power network
        /// </summary>
        public float Supply { get; private set; } = 0;

        /// <summary>
        /// Variable that causes powernet to be regenerated from its wires during the next update cycle
        /// </summary>
        public bool Dirty { get; set; } = false;

        public void Update(float frametime)
        {
            float activesupply = Supply;
            float activeload = Load;

            float storagedemand = 0;

            foreach (var supply in PowerStorageConsumerlist)
            {
                storagedemand += supply.RequestCharge();
            }

            float passivesupply = 0;
            float passivedemand = 0;

            foreach (var supply in PowerStorageSupplierlist)
            {
                passivesupply += supply.AvailableCharge();
                passivedemand += supply.RequestCharge();
            }


            //If we have enough power to feed all load and storage demand, then feed everything
            if (activesupply > activeload + storagedemand + passivedemand)
            {
                PowerAllDevices();
                ChargeActiveStorage();
                ChargePassiveStorage();
            }
            //We don't have enough power for the storage powernet suppliers, ignore powering them
            else if (activesupply > activeload + storagedemand)
            {
                PowerAllDevices();
                ChargeActiveStorage();
            }
            //We require the storage powernet suppliers to power the remaining storage components and device load
            else if (activesupply + passivesupply > activeload + storagedemand)
            {
                PowerAllDevices();
                ChargeActiveStorage();
                RetrievePassiveStorage();
            }
            //We cant afford to fund the storage components, so lets try to power the basic load using our supply and storage supply
            else if (activesupply + passivesupply > activeload)
            {
                PowerAllDevices();
                RetrievePassiveStorage();
            }
            //We cant even cover the basic device load, start disabling devices in order of priority until the remaining load is lowered enough to be met
            else if (activesupply + passivesupply < activeload)
            {
                PowerAllDevices(); //This merely makes our inevitable betrayal all the sweeter
                RetrievePassiveStorage();

                var depowervalue = activeload - (activesupply + passivesupply);

                //Providers use same method to recreate functionality
                foreach (var device in Deviceloadlist)
                {
                    device.Powered = false;
                    DepoweredDevices.Add(device);
                    depowervalue -= device.Load;
                    if (depowervalue < 0)
                        break;
                }
            }
        }

        private void PowerAllDevices()
        {
            foreach (var device in DepoweredDevices)
            {
                device.Powered = true;
            }
            DepoweredDevices.Clear();
        }

        private void ChargeActiveStorage()
        {
            foreach (var storage in PowerStorageConsumerlist)
            {
                storage.ChargePowerTick();
            }
        }

        private void ChargePassiveStorage()
        {
            foreach (var storage in PowerStorageSupplierlist)
            {
                storage.ChargePowerTick();
            }
        }

        private void RetrievePassiveStorage()
        {
            foreach (var storage in PowerStorageSupplierlist)
            {
                storage.ChargePowerTick();
            }
        }

        /// <summary>
        /// Kills a powernet after it is marked dirty and its component have already been regenerated by the powernet system
        /// </summary>
        public void DirtyKill()
        {
            Wirelist.Clear();
            while (Nodelist.Count != 0)
            {
                Nodelist[0].DisconnectFromPowernet();
            }
            Generatorlist.Clear();
            Deviceloadlist.Clear();
            DepoweredDevices.Clear();
            PowerStorageSupplierlist.Clear();
            PowerStorageConsumerlist.Clear();

            RemoveFromSystem();
        }

        /// <summary>
        /// Combines two powernets when they connect via powertransfer components
        /// </summary>
        public void MergePowernets(Powernet toMerge)
        {
            //TODO: load balance reconciliation between powernets on merge tick here

            foreach (var wire in toMerge.Wirelist)
            {
                wire.Parent = this;
            }
            Wirelist.AddRange(toMerge.Wirelist);
            toMerge.Wirelist.Clear();

            foreach (var node in toMerge.Nodelist)
            {
                node.Parent = this;
            }
            Nodelist.AddRange(toMerge.Nodelist);
            toMerge.Nodelist.Clear();

            foreach (var generator in toMerge.Generatorlist)
            {
                Generatorlist.Add(generator.Key, generator.Value);
            }
            toMerge.Generatorlist.Clear();

            foreach (var device in toMerge.Deviceloadlist)
            {
                Deviceloadlist.Add(device);
            }
            toMerge.Deviceloadlist.Clear();

            DepoweredDevices.AddRange(toMerge.DepoweredDevices);
            toMerge.DepoweredDevices.Clear();

            PowerStorageSupplierlist.AddRange(toMerge.PowerStorageSupplierlist);
            toMerge.PowerStorageSupplierlist.Clear();

            PowerStorageConsumerlist.AddRange(toMerge.PowerStorageConsumerlist);
            toMerge.PowerStorageConsumerlist.Clear();

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
            Deviceloadlist.Add(device);
            Load += device.Load;
            if (!device.Powered)
                DepoweredDevices.Add(device);
        }

        /// <summary>
        /// Update one of the loads from a deviceconnected to the powernet
        /// </summary>
        public void UpdateDevice(PowerDeviceComponent device, float oldLoad)
        {
            if (Deviceloadlist.Contains(device))
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
            if (Deviceloadlist.Contains(device))
            {
                Load -= device.Load;
                Deviceloadlist.Remove(device);
                if (DepoweredDevices.Contains(device))
                    DepoweredDevices.Remove(device);
            }
            else
            {
                var name = device.Owner.Prototype.Name;
                Logger.Info(String.Format("We tried to remove a device twice from the same powernet somehow, prototype {0}", name));
            }
        }

        /// <summary>
        /// Register a power supply from a generator connected to the powernet
        /// </summary>
        public void AddGenerator(PowerGeneratorComponent generator)
        {
            Generatorlist.Add(generator, generator.Supply);
            Supply += generator.Supply;
        }

        /// <summary>
        /// Update the value supplied from a generator connected to the powernet
        /// </summary>
        public void UpdateGenerator(PowerGeneratorComponent generator)
        {
            if (Generatorlist.ContainsKey(generator))
            {
                Supply -= Generatorlist[generator];
                Generatorlist[generator] = generator.Supply;
                Supply += generator.Supply;
            }
        }

        /// <summary>
        /// Remove a power supply from a generator connected to the powernet
        /// </summary>
        public void RemoveGenerator(PowerGeneratorComponent generator)
        {
            if (Generatorlist.ContainsKey(generator))
            {
                Supply -= Generatorlist[generator];
                Generatorlist.Remove(generator);
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
                PowerStorageSupplierlist.Add(storage);
            else
                PowerStorageConsumerlist.Add(storage);
        }

        //How do I even call this? TODO: fix
        public void UpdateStorageType(PowerStorageComponent storage)
        {
            //If our chargepowernet settings change we need to tell the powernet of this new setting and remove traces of our old setting
            if (PowerStorageSupplierlist.Contains(storage))
                PowerStorageSupplierlist.Remove(storage);
            if (PowerStorageConsumerlist.Contains(storage))
                PowerStorageConsumerlist.Remove(storage);

            //Apply new setting
            if (storage.ChargePowernet)
                PowerStorageSupplierlist.Add(storage);
            else
                PowerStorageConsumerlist.Add(storage);
        }

        /// <summary>
        /// Remove a power supply from a generator connected to the powernet
        /// </summary>
        public void RemovePowerStorage(PowerStorageComponent storage)
        {
            if (PowerStorageSupplierlist.Contains(storage))
            {
                PowerStorageSupplierlist.Remove(storage);
            }
            if (PowerStorageConsumerlist.Contains(storage))
            {
                PowerStorageSupplierlist.Remove(storage);
            }
        }
        #endregion Registration
    }
}
