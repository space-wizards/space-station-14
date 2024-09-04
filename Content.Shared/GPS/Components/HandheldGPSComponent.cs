
using Robust.Shared.GameStates;

namespace Content.Shared.GPS.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class HandheldGPSComponent : Component
    {
        [DataField("updateRate")]
        [AutoNetworkedField]
        public float UpdateRate = 1.5f;

        public string StoredCoords = "Unknown";
        public TimeSpan NextCoordUpdate = TimeSpan.Zero;
    }
}
