
using Robust.Shared.GameStates;

namespace Content.Shared.GPS.Components
{
    [RegisterComponent, NetworkedComponent, ComponentProtoName("HandheldGPS"), AutoGenerateComponentState]
    public sealed partial class HandheldGPSComponent : Component
    {
        [DataField("updateRate")]
        [AutoNetworkedField]
        public float UpdateRate = 1.5f;

        [DataField("storedCoords")]
        [AutoNetworkedField]
        public string StoredCoords = "Unknown";

        [DataField("nextUpdate")]
        [AutoNetworkedField]
        public TimeSpan NextCoordUpdate = TimeSpan.Zero;
    }
}
