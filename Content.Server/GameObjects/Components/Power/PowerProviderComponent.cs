using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Component that wirelessly connects and powers devices, connects to powernet via node and can be combined with internal storage component
    /// </summary>
    public class PowerProviderComponent : PowerDeviceComponent
    {
        public override string Name => "PowerProvider";

        /// <inheritdoc />
        public override DrawTypes Drawtype { get; protected set; } = DrawTypes.Node;

        /// <summary>
        /// Variable that determines the range that the power provider will try to supply power to
        /// </summary>
        public int PowerRange { get; private set; } = 0;

        /// <summary>
        /// List storing all the power devices that we are currently providing power to
        /// </summary>
        public SortedDictionary<PowerDeviceComponent, float> Deviceloadlist = new SortedDictionary<PowerDeviceComponent, float>(new Powernet.DevicePriorityCompare());

        public List<PowerDeviceComponent> DepoweredDevices = new List<PowerDeviceComponent>();

        public override Powernet.Priority Priority { get; protected set; } = Powernet.Priority.Provider;

        public override void LoadParameters(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode("Range", out YamlNode node))
            {
                PowerRange = node.AsInt();
            }
            if (mapping.TryGetNode("Priority", out node))
            {
                Priority = node.AsEnum<Powernet.Priority>();
            }
        }

        /// <inheritdoc />
        public override void SetPowered(bool value)
        {
            //Let them set us true, we must now power all the devices that rely on us for energy
            if (value == true)
            {
                PowerAllDevices();
                return;
            }

            //A powernet has decided we will not be powered this tick, lets try to power ourselves
            if (value == false && Owner.TryGetComponent(out PowerStorageComponent storage))
            {
                //Can the storage cover powering all our devices and us? If so power all
                if (storage.CanDeductCharge(Load))
                {
                    storage.DeductCharge(Load);
                    _powered = true;
                    return;
                }
                //Does the storage even have any power to give us? If so power as much as we can
                else if (storage.RequestAllCharge() != 0)
                {
                    var depowervalue = storage.RequestAllCharge() - Load;
                    _powered = true;
                    //See code in powernet for same functionality
                    foreach (var kvp in Deviceloadlist)
                    {
                        kvp.Key.Powered = false;
                        DepoweredDevices.Add(kvp.Key);
                        depowervalue -= kvp.Value;
                        if (depowervalue < 0)
                            break;
                    }
                    return;
                }
                //Storage doesn't have anything, depower everything
                else if(storage.RequestAllCharge() == 0)
                {
                    DepowerAllDevices();
                    return;
                }
            }

            //For some reason above we could not power ourselves, we depower ourselves and all devices
            DepowerAllDevices();
            return;
        }

        private void PowerAllDevices()
        {
            _powered = true;
            foreach (var device in DepoweredDevices)
            {
                device.Powered = true;
            }
        }

        private void DepowerAllDevices()
        {
            _powered = false;
            foreach (var device in DepoweredDevices)
            {
                device.Powered = false;
            }
        }

        private void PowernetConnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.AddDevice(this);
            Connected = DrawTypes.Node;

            //Find devices within range to take under our control
            var _emanager = IoCManager.Resolve<IServerEntityManager>();
            var position = Owner.GetComponent<TransformComponent>().WorldPosition;
            var entities = _emanager.GetEntitiesInRange(Owner, PowerRange)
                        .Where(x => x.HasComponent<PowerDeviceComponent>());


            foreach (var entity in entities)
            {
                var device = entity.GetComponent<PowerDeviceComponent>();
                
                //Make sure the device can accept power providers to give it power
                if (device.Drawtype == DrawTypes.PowerProvider || device.Drawtype == DrawTypes.Both)
                {
                    device.AddProvider(this);
                }
            }
        }

        private void PowernetDisconnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.RemoveDevice(this);
            Connected = DrawTypes.None;

            //We don't want to make the devices under us think we're still a valid provider if we have no powernet to connect to
            foreach (var device in Deviceloadlist.Keys)
            {
                device.RemoveProvider(this);
            }
        }

        /// <summary>
        /// Register a continuous load from a device connected to the powernet
        /// </summary>
        public void AddDevice(PowerDeviceComponent device)
        {
            Deviceloadlist.Add(device, device.Load);
            Load += device.Load;
        }

        /// <summary>
        /// Update one of the loads from a deviceconnected to the powernet
        /// </summary>
        public void UpdateDevice(PowerDeviceComponent device)
        {
            if (Deviceloadlist.ContainsKey(device))
            {
                Load -= Deviceloadlist[device];
                Deviceloadlist[device] = device.Load;
                Load += device.Load;
            }
        }

        /// <summary>
        /// Remove a continuous load from a device connected to the powernet
        /// </summary>
        public void RemoveDevice(PowerDeviceComponent device)
        {
            if (Deviceloadlist.ContainsKey(device))
            {
                Load -= Deviceloadlist[device];
                Deviceloadlist.Remove(device);
            }
            else
            {
                var name = device.Owner.Prototype.Name;
                Logger.Log(String.Format("We tried to remove a device twice from the same {0} somehow, prototype {1}", Name, name));
            }
        }
    }
}
