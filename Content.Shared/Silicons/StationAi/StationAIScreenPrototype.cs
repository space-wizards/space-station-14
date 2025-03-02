using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.StationAi;

[Prototype("aiScreen")]
public sealed partial class StationAIScreenPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    [DataField]
    public bool Roundstart = true;

    [DataField]
    public int Priority = 1;
}
