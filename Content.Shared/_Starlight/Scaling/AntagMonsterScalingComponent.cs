using Content.Shared.FixedPoint;
using Content.Shared.Mobs;

namespace Content.Shared._Starlight.Scaling.Components;

[RegisterComponent]
public sealed partial class AntagMonsterScalingComponent : Component
{
    /// <summary>
    /// Indicates the highest percentage health increase (or decrease) allowed for this creature.
    /// </summary>
    [DataField]
    public double MaximumHealthScaling = 0.50;

    [DataField]
    public bool IsScaled = false;

    [DataField]
    public SortedDictionary<FixedPoint2, MobState>? OriginalThresholds;
}