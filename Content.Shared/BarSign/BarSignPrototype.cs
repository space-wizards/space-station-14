using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.BarSign;

[Prototype]
public sealed partial class BarSignPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Icon { get; private set; } = default!;

    [DataField]
    public LocId Name { get; private set; } = "barsign-component-name";

    [DataField]
    public LocId Description { get; private set; }

    [DataField]
    public bool Hidden { get; private set; }
}
