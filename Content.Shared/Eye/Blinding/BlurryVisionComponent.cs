using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class BlurryVisionComponent : Component
    {
        [DataField("mangitude")]
        public float Magnitude = 1f;

        public bool Active => Magnitude < 10f;
    }
}
