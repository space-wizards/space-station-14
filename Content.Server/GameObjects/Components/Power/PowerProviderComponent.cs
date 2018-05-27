using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
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
        public override DrawTypes DrawType { get; protected set; } = DrawTypes.Node;

        /// <summary>
        /// Variable that determines the range that the power provider will try to supply power to
        /// </summary>
        public int PowerRange { get; private set; } = 0;

        /// <summary>
        /// List storing all the power devices that we are currently providing power to
        /// </summary>
        public SortedSet<PowerDeviceComponent> DeviceLoadList = new SortedSet<PowerDeviceComponent>(new Powernet.DevicePriorityCompare());

        public List<PowerDeviceComponent> DepoweredDevices = new List<PowerDeviceComponent>();

        public override Powernet.Priority Priority { get; protected set; } = Powernet.Priority.Provider;

        public override void OnRemove()
        {
            base.OnRemove();

            foreach (var device in DeviceLoadList)
            {
                device.RemoveProvider(this);
            }
        }

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

        internal override void ProcessInternalPower(float frametime)
        {
            // Right now let's just assume that APCs don't have a power demand themselves and as such they're always marked as powered.
            InternalPowered = true;
            if (!Owner.TryGetComponent<PowerStorageComponent>(out var storage))
            {
                return;
            }

            if (ExternalPowered)
            {
                PowerAllDevices();
                return;
            }


            if (storage.CanDeductCharge(Load * frametime))
            {
                PowerAllDevices();
                storage.DeductCharge(Load * frametime);
                return;
            }

            var remainingEnergy = storage.AvailableCharge(frametime);
            var usedEnergy = 0f;
            foreach (var device in DeviceLoadList)
            {
                var deviceLoad = device.Load * frametime;
                if (deviceLoad > remainingEnergy)
                {
                    device.ExternalPowered = false;
                    DepoweredDevices.Add(device);
                }
                else
                {
                    if (!device.ExternalPowered)
                    {
                        DepoweredDevices.Remove(device);
                        device.ExternalPowered = true;
                    }
                    usedEnergy += deviceLoad;
                    remainingEnergy -= deviceLoad;
                }
            }

            storage.DeductCharge(usedEnergy);
        }
        private void PowerAllDevices()
        {
            foreach (var device in DepoweredDevices)
            {
                device.ExternalPowered = true;
            }
            DepoweredDevices.Clear();
        }

        private void DepowerAllDevices()
        {
            foreach (var device in DeviceLoadList)
            {
                device.ExternalPowered = false;
            }
        }

        protected override void PowernetConnect(object sender, PowernetEventArgs eventarg)
        {
            base.PowernetConnect(sender, eventarg);

            //Find devices within range to take under our control
            var _emanager = IoCManager.Resolve<IServerEntityManager>();
            var position = Owner.GetComponent<TransformComponent>().WorldPosition;
            var entities = _emanager.GetEntitiesInRange(Owner, PowerRange)
                        .Where(x => x.HasComponent<PowerDeviceComponent>());


            foreach (var entity in entities)
            {
                var device = entity.GetComponent<PowerDeviceComponent>();

                //Make sure the device can accept power providers to give it power
                if (device.DrawType == DrawTypes.Provider || device.DrawType == DrawTypes.Both)
                {
                    device.AddProvider(this);
                }
            }
        }


        protected override void PowernetDisconnect(object sender, PowernetEventArgs eventarg)
        {
            base.PowernetDisconnect(sender, eventarg);

            //We don't want to make the devices under us think we're still a valid provider if we have no powernet to connect to
            foreach (var device in DeviceLoadList.ToList())
            {
                device.RemoveProvider(this);
            }
        }

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
                Logger.Info(String.Format("We tried to remove a device twice from the same {0} somehow, prototype {1}", Name, name));
            }
        }
    }
}
