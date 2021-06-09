#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Items
{
    [RegisterComponent]
    public class ThrownItemComponent : Component
    {
        public override string Name => "ThrownItem";

        public IEntity? Thrower { get; set; }
    }
}
