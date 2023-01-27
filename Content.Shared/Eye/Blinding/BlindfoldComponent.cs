using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed partial class BlindfoldComponent : Component
    {
        [ViewVariables]
        public bool IsActive = false;
    }
}
