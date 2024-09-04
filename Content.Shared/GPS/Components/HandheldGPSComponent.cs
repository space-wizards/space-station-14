using Robust.Shared.GameStates;

namespace Content.Shared.GPS.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
    public sealed partial class HandheldGPSComponent : Component
    {
        [DataField, AutoNetworkedField]
        public TimeSpan UpdateRate = TimeSpan.FromSeconds(1.5);

        public string StoredCoords;

        [AutoPausedField]
        public TimeSpan NextCoordUpdate = TimeSpan.Zero;
    }
}
