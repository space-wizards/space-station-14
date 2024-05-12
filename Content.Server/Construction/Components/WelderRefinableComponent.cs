using Content.Shared.Tools;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Components;

/// <summary>
/// Used for something that can be refined by welder.
/// For example, glass shard can be refined to glass sheet.
/// </summary>
[RegisterComponent]
public sealed partial class WelderRefinableComponent : Component
{
    [DataField]
    public HashSet<EntProtoId>? RefineResult = new();

    [DataField]
    public float RefineTime = 2f;

    [DataField]
    public float RefineFuel;

    [DataField]
    public ProtoId<ToolQualityPrototype> QualityNeeded = "Welding";
}
