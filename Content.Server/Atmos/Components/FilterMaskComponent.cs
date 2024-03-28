using Content.Server.Body.Systems;
using Content.Shared.FixedPoint;


namespace Content.Server.Atmos.Components;

/// <summary>
/// Used in head and mask with a smoke gas filter.
/// </summary>
[RegisterComponent, Access(typeof(SmokeFilterSystem))]
public sealed partial class FilterMaskComponent : Component
{
    [DataField]
    public bool IsActive = false;
    /// <summary>
    /// The durability left
    /// </summary>
    [DataField]
    public FixedPoint2 State = 100;

    [DataField]
    public FixedPoint2 Max_State = 100;

    [DataField]
    public FixedPoint2 Good_State = 50;

    [DataField]
    public FixedPoint2 Bad_State = 10;

    /// <summary>
    ///Percentage of smoke quantity who pass the filter like 0.1 -> 10% removed
    /// </summary>
    [DataField]
    public FixedPoint2? Particle_ignore = 0.0;
    /// <summary>
    /// The durability removed at each use
    /// </summary>
    [DataField]
    public FixedPoint2 Use_rate = 0.1;
}

