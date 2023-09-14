using Robust.Shared.GameStates;

namespace Content.Shared.Puppet
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class PuppetDummyComponent : Component
    {
        [DataField("enabled")]
        public bool Enabled = false;
    }
}
