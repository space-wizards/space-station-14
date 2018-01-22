using SS14.Shared.GameObjects;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects.Components.Power
{
    //Component that requires power to function
    public class PowerDeviceComponent : Component
    {
        public override string Name => "PowerDevice";

        /// <summary>
        /// The method of draw we will try to use to place our load set via component parameter, defaults to not needing power
        /// </summary>
        public DrawTypes Drawtype { get; private set; } = DrawTypes.None;

        /// <summary>
        /// The power draw method we are currently connected to and using
        /// </summary>
        public DrawTypes Connected { get; private set; } = DrawTypes.None;

        /// <summary>
        /// Status indicator variable for powered
        /// </summary>
        public bool Powered { get; private set; } = false;

        /// <summary>
        /// Power load from this entity
        /// </summary>
        private float _load = 100; //arbitrary magic number to start
        public float Load
        {
            get => _load;
            set { UpdateLoad(value); }
        }

        /// <summary>
        /// A power provider that will handle our load, if we are linked to any
        /// </summary>
        public PowerProviderComponent Provider { get; private set; }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode("Drawtype", out YamlNode node))
            {
                Drawtype = node.AsEnum<DrawTypes>();
            }
            if (mapping.TryGetNode("Load", out node))
            {
                Load = node.AsFloat();
            }
        }

        public override void Initialize()
        {
            if(Drawtype == DrawTypes.Both || Drawtype == DrawTypes.Node)
            {
                if(Owner.TryGetComponent(out PowerNodeComponent node))
                {
                    node.OnPowernetConnect += PowernetConnect;
                    node.OnPowernetDisconnect += PowernetDisconnect;
                }
            }
            if(Drawtype == DrawTypes.Both || Drawtype == DrawTypes.PowerProvider)
            {
                //stuff to connect to provider
            }
        }

        private void UpdateLoad(float value)
        {
            if(Connected == DrawTypes.Node)
            {
                var node = Owner.GetComponent<PowerNodeComponent>();
                node.Parent.UpdateDevice(this);
            }
            else if(Connected == DrawTypes.PowerProvider)
            {
                //Provider code
            }
        }
        
        //Node has become anchored to a powernet
        private void PowernetConnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.AddDevice(this);
            Connected = DrawTypes.Node;
            //Remove from provider so that direct powernet connections take priority if using Both
        }

        //Node has become unanchored from a powernet
        private void PowernetDisconnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.RemoveDevice(this);
            //Add to provider if one is available 
            //If not available code below
            Connected = DrawTypes.None;
        }
    }

    public enum DrawTypes
    {
        None = 0,
        Node = 1,
        PowerProvider = 2,
        Both = 3
    }
}
