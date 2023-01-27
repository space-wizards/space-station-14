using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed partial class BlurryVisionComponent : Component
    {
        [DataField("mangitude")]
        public float Magnitude = 1f;

        public bool Active => Magnitude < 10f;
    }
}
