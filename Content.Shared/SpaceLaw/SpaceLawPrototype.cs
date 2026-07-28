using Robust.Shared.Prototypes;

namespace Content.Shared.SpaceLaw;

[Prototype]
public sealed partial class SpaceLawPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public string Desc = string.Empty;

    [DataField(required: true)]
    public string Notes = string.Empty;

    [DataField(required: true)]
    public string Code = string.Empty;

    [DataField(required: true)]
    public string Color = string.Empty;
}
