using Content.Shared.Storage;
using Content.Shared.Tools;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Components;

/// <summary>
/// Used for something that can be refined by welder.
/// For example, glass shard can be refined to glass sheet.
/// </summary>
[RegisterComponent, Access(typeof(RefiningSystem))]
public sealed partial class WelderRefinableComponent : Component
{
    /// <summary>
    /// The items created when the item is refined.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> RefineResult = new();

    /// <summary>
    /// The amount of time it takes to refine a given item.
    /// </summary>
    [DataField]
    public float RefineTime = 2f;

    /// <summary>
    /// The amount of fuel it takes to refine a given item.
    /// </summary>
    [DataField]
    public float RefineFuel;

    /// <summary>
    /// The tool type needed in order to refine this item.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> QualityNeeded = "Welding";
}
