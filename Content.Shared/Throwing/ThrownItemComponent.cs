#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.Throwing
{
    [RegisterComponent]
    public class ThrownItemComponent : Component
    {
        public override string Name => "ThrownItem";

        public IEntity? Thrower { get; set; }
    }
}
