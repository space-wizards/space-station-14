using SS14.Shared.GameObjects;
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
    public class PowerGeneratorComponent : Component
    {
        public override string Name => "PowerGenerator";

        //Power supply from this entity
        private float _supply = 1000; //arbitrary initial magic number to start
        public float Supply
        {
            get => _supply;
            set { UpdateSupply(value); }
        }

        //If we connect directly to a powernet through a node it is stored here
        public Powernet Parent { get; private set; }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode("Supply", out YamlNode node))
            {
                Supply = node.AsFloat();
            }
        }

        public override void Initialize()
        {
            if (Owner.TryGetComponent(out PowerNodeComponent node))
            {
                node.OnPowernetConnect += PowernetConnect;
                node.OnPowernetDisconnect += PowernetDisconnect;
            }
            else
            {
                var prototype = Owner.Prototype.Name;
                Logger.Log(String.Format("Powergenerator type needs node to function in prototype {0}", prototype));
            }
        }

        private void UpdateSupply(float value)
        {
            _supply = value;
            var node = Owner.GetComponent<PowerNodeComponent>();
            node.Parent.UpdateGenerator(this);
        }

        //Node has become anchored to a powernet
        private void PowernetConnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.AddGenerator(this);
        }

        //Node has become unanchored from a powernet
        private void PowernetDisconnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.RemoveGenerator(this);
        }
    }
}
