using Robust.Shared.GameStates;

namespace Content.Shared.Dummy
{
    [RegisterComponent, NetworkedComponent]
    public sealed class DummyComponent : Component
    {
        [DataField("enabled")]
        public bool Enabled = false;
    }
}
