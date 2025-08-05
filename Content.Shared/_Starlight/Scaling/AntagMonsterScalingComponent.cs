using Content.Shared.FixedPoint;
using Content.Shared.Mobs;

namespace Content.Shared._Starlight.Scaling.Components;

[RegisterComponent]
public sealed partial class AntagMonsterScalingComponent : Component
{
    /// <summary>
    /// Determines how much the health scaling affects this monster.
    /// This value is LINEAR, meaning the final health calculation is multiplied
    /// by this value.
    /// 0.0 means its health is unaffected.
    /// </summary>
    [DataField(required: true)]
    public double HealthScalingAdjustment = 0.0;

    [DataField]
    public bool IsScaled = false;

    [DataField]
    public SortedDictionary<FixedPoint2, MobState>? OriginalThresholds;
}