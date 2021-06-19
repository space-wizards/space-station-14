#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Surgery.Tool
{
    public class SharedSurgeryToolComponent : Component
    {
        public override string Name => "SurgeryTool";

        [DataField("delay")]
        public float Delay { get; } = default!;
    }
}
