using Content.Server.StationEvents.Events;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Loads a grid far away from a random station.
/// Requires <see cref="RuleGridsComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(LoadFarGridRule))]
public sealed partial class LoadFarGridRuleComponent : Component
{
    /// <summary>
    /// Path to the grid to spawn.
    /// </summary>
    [DataField(required: true)]
    public ResPath Path = new();

    /// <summary>
    /// Roughly how many AABBs away
    /// </summary>
    [DataField(required: true)]
    public float DistanceModifier;

    /// <summary>
    /// "Stations of Unusual Size Constant", derived from the AABB.Width of Shoukou.
    /// This Constant is used to check the size of a station relative to the reference point
    /// </summary>
    [DataField]
    public float Sousk = 123.44f;
}
