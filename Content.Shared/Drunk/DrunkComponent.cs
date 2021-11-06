using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Drunk
{
    [RegisterComponent, NetworkedComponent]
    public class DrunkComponent : Component
    {
        public override string Name => "Drunk";
    }
}
