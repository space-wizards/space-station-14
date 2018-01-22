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
    public class PowerProviderComponent : Component
    {
        public override string Name => "PowerProvider";

        //How far we will gather devices from to power them
        public int PowerRange { get; private set; } = 0;

        //Component to connect powertransfering components with powerdevices at a distance
        public List<PowerDeviceComponent> Devicelist;

        public override void Initialize()
        {

        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode("Range", out YamlNode node))
            {
                PowerRange = node.AsInt();
            }
        }
    }
}
