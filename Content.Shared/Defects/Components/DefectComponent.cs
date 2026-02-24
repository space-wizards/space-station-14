using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Defects.Components;

/// <summary>
/// Abstract base for all defect components on second-hand / worn items.
/// When the entity also has <see cref="RandomDefectsComponent"/>, <c>DefectSystem</c>
/// rolls <see cref="Prob"/> at MapInit and removes defects that fail.
/// </summary>
public abstract partial class DefectComponent : Component
{
    /// <summary>Probability (0–1) that this defect is present at spawn.</summary>
    [DataField]
    public float Prob = 1.0f;
}
