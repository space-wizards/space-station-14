using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Defects.Components;

// Abstract base for all defect components on second-hand / worn items.
// When the entity also has RandomDefectsComponent, DefectSystem
// rolls Prob at MapInit and removes defects that fail.
public abstract partial class DefectComponent : Component
{
    // Probability (0–1) that this defect is present at spawn.
    [DataField]
    public float Prob = 1.0f;
}
