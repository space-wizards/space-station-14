using Content.Server.Body.Surgery.Tool.Behaviors;
using Content.Shared.Body.Surgery.Tool;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Body.Surgery.Tool
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSurgeryToolComponent))]
    public class SurgeryToolComponent : SharedSurgeryToolComponent
    {
        [DataField("behavior")]
        public ISurgeryBehavior? Behavior { get; } = default!;
    }
}
