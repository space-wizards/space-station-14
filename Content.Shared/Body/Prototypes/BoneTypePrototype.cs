using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes;

[Prototype("boneType")]
public sealed class BoneTypePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;
}
