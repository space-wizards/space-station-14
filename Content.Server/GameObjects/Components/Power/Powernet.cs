using SS14.Shared.GameObjects;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Power
{
    //Master class for group of powertransfercomponents, takes in and distributes power via nodes
    public class Powernet : Component
    {
        public override string Name => "Dont fucking use this, this isn't a real component it just needs to update";

        /// <summary>
        /// The entities that make up the powernet's physical location and allow powernet connection
        /// </summary>
        public List<PowerTransferComponent> Wirelist { get; set; } = new List<PowerTransferComponent>();

        /// <summary>
        /// Entities that connect directly to the powernet through PTC above to add power or add power load
        /// </summary>
        public List<PowerNodeComponent> Nodelist { get; set; } = new List<PowerNodeComponent>();

        /// <summary>
        /// Subset of nodelist that draw power, stores information on current continuous powernet load
        /// </summary>
        public Dictionary<PowerDeviceComponent, float> Deviceloadlist { get; set; } = new Dictionary<PowerDeviceComponent, float>();

        /// <summary>
        /// Subset of nodelist that draws power from a number of devices combined, adding load from devices at a range from one single node
        /// </summary>
        public Dictionary<PowerProviderComponent, float> Providerloadlist { get; set; } = new Dictionary<PowerProviderComponent, float>();


        /// <summary>
        /// Subset of nodelist that adds a continuous power supply to the network
        /// </summary>
        public Dictionary<PowerGeneratorComponent, float> Generatorlist { get; set; } = new Dictionary<PowerGeneratorComponent, float>();

        public float Load { get; private set; } = 0;
        public float Supply { get; private set; } = 0;

        public void Update()
        {

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

            foreach (var node in toMerge.Nodelist)
            {
                node.Parent = this;
            }
            Nodelist.AddRange(toMerge.Nodelist);

            Deviceloadlist.Concat(toMerge.Deviceloadlist).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            Providerloadlist.Concat(toMerge.Providerloadlist).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            Generatorlist.Concat(toMerge.Generatorlist).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            Load += toMerge.Load;
            Supply += toMerge.Supply;

            toMerge.Deviceloadlist.Clear();
            toMerge.Providerloadlist.Clear();
            toMerge.Generatorlist.Clear();
            toMerge.Wirelist.Clear();
            toMerge.Nodelist.Clear();
            //just shutdown the other powernet instead?? or manually destroy here
        }


        #region Registration
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
            if(Deviceloadlist.ContainsKey(device))
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
            if(Deviceloadlist.ContainsKey(device))
            {
                Load -= Deviceloadlist[device];
                Deviceloadlist.Remove(device);
            }
            else
            {
                var name = device.Owner.Prototype.Name;
                Logger.Log(String.Format("We tried to remove a device twice from the same powernet somehow, prototype {0}", name));
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
                Logger.Log(String.Format("We tried to remove a generator twice from the same powernet somehow, prototype {0}", name));
            }
        }
        #endregion Registration
    }
}
