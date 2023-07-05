using Robust.Shared.GameStates;

namespace Content.Shared.Puppet
{
    [RegisterComponent, NetworkedComponent]
    public sealed class PuppetDummyComponent : Component
    {
        [DataField("enabled")]
        public bool Enabled = false;
    }
}
