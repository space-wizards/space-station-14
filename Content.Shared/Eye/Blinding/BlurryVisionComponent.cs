using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding
{
    [RegisterComponent, NetworkedComponent]
    public sealed class BlurryVisionComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public float Magnitude = 1f;
    }
}
