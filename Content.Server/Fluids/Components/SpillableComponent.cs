using System.Threading;

namespace Content.Server.Fluids.Components;

[RegisterComponent]
public sealed class SpillableComponent : Component
{
    [DataField("solution")]
    public string SolutionName = "puddle";

    /// <summary>
    ///     Should this item be spilled when worn as clothing?
    ///     Doesn't count for pockets or hands.
    /// </summary>
    [DataField("spillWorn")]
    public bool SpillWorn = true;

    [DataField("spillDelay")]
    public float? SpillDelay;

    public CancellationTokenSource? CancelToken;
}
