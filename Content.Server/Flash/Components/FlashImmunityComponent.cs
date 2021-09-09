using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Flash.Components
{
    [RegisterComponent, Friend(typeof(FlashSystem))]
    public class FlashImmunityComponent : Component
    {
        public override string Name => "FlashImmunity";

        [DataField("enabled")]
        public bool Enabled { get; set; } = true;
    }
}
