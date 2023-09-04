using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Mind.Components;

[RegisterComponent]
public sealed partial class TransferMindOnGibComponent : Component
{
    [DataField("targetTag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
    public string TargetTag = "MindTransferTarget";

    /// <summary>
    ///     Components from the entity to pass along with the mind
    /// </summary>
    [DataField("transferredComponents")]
    public ComponentRegistry TransferredComponents = default!;
}
