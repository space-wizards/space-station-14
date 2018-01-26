using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Component that wirelessly connects and powers devices, connects to powernet via node and can be combined with internal storage component
    /// </summary>
    public class PowerProviderComponent : PowerDeviceComponent
    {
        public override string Name => "PowerProvider";

        public override DrawTypes Drawtype { get; protected set; } = DrawTypes.Node;

        //How far we will gather devices to be powered
        public int PowerRange { get; private set; } = 0;

        //Component to connect powertransfering components with powerdevices at a distance
        public Dictionary<PowerDeviceComponent, float> Deviceloadlist = new Dictionary<PowerDeviceComponent, float>();

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
