using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.DeviceNetwork.Components
{
    /// <summary>
    /// Sends and receives device network messages wirelessly. Devices sending and receiving need to be in range and on the same frequency.
    /// </summary>
    [RegisterComponent]
    [ComponentProtoName("WirelessNetworkConnection")]
    public class WirelessNetworkComponent : Component
    {
        [DataField("range")]
        public int Range { get; set; }
    }
}
