using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Shuttles.Prototypes;

[Prototype]
public sealed partial class GridPackPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<ResPath> GridPaths { get; private set; } =  new();
}
