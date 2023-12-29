using Content.Server.Contraband.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Contraband.Components;

[RegisterComponent]
public sealed partial class ContrabandComponent : Component
{
    [DataField("category", required: true), ViewVariables(VVAccess.ReadWrite)]
    public HashSet<ProtoId<ContrabandCategoryPrototype>> Category = new();
}
