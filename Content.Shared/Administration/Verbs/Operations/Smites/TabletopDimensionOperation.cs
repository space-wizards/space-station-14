using Content.Shared.Tabletop.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations.Smites;

public sealed partial class TabletopDimensionOperation : AdminOperationBase<TabletopDimensionOperation>
{
    [DataField(required: true)]
    public EntProtoId<TabletopGameComponent> Prototype { get; private set; }
}
