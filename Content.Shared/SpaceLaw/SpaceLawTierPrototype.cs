using Robust.Shared.Prototypes;

namespace Content.Shared.SpaceLaw;

[Prototype]
public sealed partial class SpaceLawTierPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public string Code = string.Empty;

    [DataField(required: true)]
    public string Sentence = string.Empty;

    [DataField(required: true)]
    public List<ProtoId<SpaceLawPrototype>> Laws = new();
}