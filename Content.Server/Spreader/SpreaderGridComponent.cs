using Content.Shared.Spreader;
using Robust.Shared.Prototypes;

namespace Content.Server.Spreader;

[RegisterComponent]
public sealed partial class SpreaderGridComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<EdgeSpreaderPrototype>, float> ProtoUpdateAccumulators =  new Dictionary<ProtoId<EdgeSpreaderPrototype>, float>();
}
