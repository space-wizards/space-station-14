using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DropPod
{
    [RegisterComponent]
    public sealed partial class LandingPointComponent : Component
    {

        [DataField("name")]
        public string NameLandingPoint = "unknow"; // The name of the point that the player will see when activating the DropPod console

        [DataField("uin")]
        public int UIN = 1; // The unique identification number of the point (each must have its own unique one!)
    }
}
