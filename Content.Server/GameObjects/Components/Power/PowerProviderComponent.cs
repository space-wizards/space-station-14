using System.Collections.Generic;
using System.Linq;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Component that wirelessly connects and powers devices, connects to powernet via node and can be combined with internal storage component
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(PowerDeviceComponent))]
    public class PowerProviderComponent : PowerDeviceComponent
    {
        public override string Name => "PowerProvider";

        /// <inheritdoc />
        public override DrawTypes DrawType { get; protected set; } = DrawTypes.Node;

        protected override bool SaveLoad => false;

        /// <summary>
        /// Variable that determines the range that the power provider will try to supply power to
        /// </summary>
        [ViewVariables]
        public int PowerRange
        {
            get => _range;
            private set => _range = value;
        }

        private int _range = 0;

        /// <summary>
        /// List storing all the power devices that we are currently providing power to
        /// </summary>
        public SortedSet<PowerDeviceComponent> DeviceLoadList =
            new SortedSet<PowerDeviceComponent>(new Powernet.DevicePriorityCompare());

        /// <summary>
        ///     List of devices in range that we "advertised" to.
        /// </summary>
        public HashSet<PowerDeviceComponent> AdvertisedDevices = new HashSet<PowerDeviceComponent>();

        public List<PowerDeviceComponent> DepoweredDevices = new List<PowerDeviceComponent>();

        public override Powernet.Priority Priority { get; protected set; } = Powernet.Priority.Provider;

        private bool _mainBreaker = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool MainBreaker
        {
            get => _mainBreaker;
            set
            {
                if (_mainBreaker == value)
                {
                    return;
                }

                _mainBreaker = value;
                if (!value)
                {
                    DepowerAllDevices();
                    Load = 0;
                }
                else
                {
                    Load = TheoreticalLoad;
                }
            }
        }

        private float _theoreticalLoad = 0f;

        [ViewVariables]
        public float TheoreticalLoad
        {
            get => _theoreticalLoad;
            set
            {
                _theoreticalLoad = value;
                if (MainBreaker)
                {
                    Load = value;
                }
            }
        }

        public PowerProviderComponent()
        {
            Load = 0;
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();

            foreach (var device in AdvertisedDevices.ToList())
            {
                device.RemoveProvider(this);
            }

            AdvertisedDevices.Clear();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _range, "range", 0);
        }

        internal override void ProcessInternalPower(float frametime)
        {
            // Right now let's just assume that APCs don't have a power demand themselves and as such they're always marked as powered.
            InternalPowered = true;
            if (!Owner.TryGetComponent<PowerStorageComponent>(out var storage))
            {
                return;
            }

            if (!MainBreaker)
            {
                return;
            }

            if (ExternalPowered)
            {
                PowerAllDevices();
                return;
            }

            if (storage.CanDeductCharge(TheoreticalLoad * frametime))
            {
                PowerAllDevices();
                storage.DeductCharge(TheoreticalLoad * frametime);
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
                DepoweredDevices.Add(device);
            }
        }

        protected override void PowernetConnect(object sender, PowernetEventArgs eventarg)
        {
            base.PowernetConnect(sender, eventarg);

            //Find devices within range to take under our control
            var _emanager = IoCManager.Resolve<IServerEntityManager>();
            var position = Owner.GetComponent<ITransformComponent>().WorldPosition;
            var entities = _emanager.GetEntitiesInRange(Owner, PowerRange)
                .Where(x => x.HasComponent<PowerDeviceComponent>());


            foreach (var entity in entities)
            {
                var device = entity.GetComponent<PowerDeviceComponent>();

                //Make sure the device can accept power providers to give it power
                if (device.DrawType == DrawTypes.Provider || device.DrawType == DrawTypes.Both)
                {
                    if (!AdvertisedDevices.Contains(device))
                    {
                        device.AddProvider(this);
                        AdvertisedDevices.Add(device);
                    }
                }
            }
        }


        protected override void PowernetDisconnect(object sender, PowernetEventArgs eventarg)
        {
            base.PowernetDisconnect(sender, eventarg);

            //We don't want to make the devices under us think we're still a valid provider if we have no powernet to connect to
            foreach (var device in AdvertisedDevices.ToList())
            {
                device.RemoveProvider(this);
            }

            AdvertisedDevices.Clear();
        }

        /// <summary>
        ///     Register a continuous load from a device connected to the powernet
        /// </summary>
        public void AddDevice(PowerDeviceComponent device)
        {
            DeviceLoadList.Add(device);
            TheoreticalLoad += device.Load;
            if (!device.Powered)
                DepoweredDevices.Add(device);
        }

        /// <summary>
        ///     Update one of the loads from a deviceconnected to the powernet
        /// </summary>
        public void UpdateDevice(PowerDeviceComponent device, float oldLoad)
        {
            if (DeviceLoadList.Contains(device))
            {
                TheoreticalLoad -= oldLoad;
                TheoreticalLoad += device.Load;
            }
        }

        /// <summary>
        ///     Remove a continuous load from a device connected to the powernet
        /// </summary>
        public void RemoveDevice(PowerDeviceComponent device)
        {
            if (DeviceLoadList.Contains(device))
            {
                TheoreticalLoad -= device.Load;
                DeviceLoadList.Remove(device);
                if (DepoweredDevices.Contains(device))
                    DepoweredDevices.Remove(device);
            }
            else
            {
                Logger.WarningS("power", "We tried to remove device {0} twice from the same {1}, somehow.",
                    device.Owner, Owner);
            }
        }

        /// <summary>
        /// Whether the device can be serviced by this provider.
        /// </summary>
        public bool CanServiceDevice(PowerDeviceComponent device)
        {
            // Stops an APC from trying to connect to itself
            if (this == device)
                return false;

            return (device.Owner.Transform.WorldPosition - Owner.Transform.WorldPosition).LengthSquared <= _range;
        }
    }
}
