using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Devices
{
    [RegisterComponent]
    public class SharedSignalerComponent : Component
    {
        public override string Name => "Signaler";

        [DataField("frequency")]
        public int Frequency = 100;
    }
}
