using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components
{
    [RegisterComponent]
    public sealed partial class GunSignalControlComponent : Component
    {
        [DataField]
        public ProtoId<SinkPortPrototype> ShootPort = "Trigger";
    }
}
