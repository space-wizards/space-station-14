using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Breathalyzer.Components;

/// <summary>
///     Adds a verb and action that allows the user to check to the entity's drunkenness.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BreathalyzerComponent : Component
{
    /// <summary>
    /// Time between each use of the breathalyzer.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The last read value, used for examine string.
    /// </summary>
    [DataField]
    public TimeSpan? LastReadValue;

    /// <summary>
    /// Drunkenness gets rounded to the nearest multiple of this when measuring.
    /// </summary>
    [DataField]
    public int Specificity = 5;

    // Praying for https://github.com/space-wizards/RobustToolbox/pull/5849 to make this usable
    /// <summary>
    /// Standard deviation for the gaussian random offset when measuring drunkenness.
    /// </summary>
    // [DataField]
    // public int Variance = 5;

    /// <summary>
    /// The drunkenness values and their associated message.<br/>
    /// The first entry with a threshold value smaller than or equal to the measured drunkenness will be displayed.<br/>
    /// The loc will get passed the measured drunkenness (as remaining time of effect 00h00m00s) with the name "<c>approximateDrunkenness</c>".
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<ulong, LocId> Thresholds = [];
}
