using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.DeviceNetwork.Components
{
    /// <summary>
    /// Sends and receives device network messages wirelessly. Devices sending and receiving need to be in range and on the same frequency.
    /// </summary>
    [RegisterComponent]
    public class WirelessNetworkComponent : Component
    {
        public override string Name => "WirelessNetworkConnection";
        
        [DataField("range")]
        public int Range { get; set; }
    }
}
