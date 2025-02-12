using Content.Shared.Tag;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Mind.Components;

[RegisterComponent]
public sealed partial class TransferMindOnGibComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>)))]
    public string TargetTag = "MindTransferTarget";
}
