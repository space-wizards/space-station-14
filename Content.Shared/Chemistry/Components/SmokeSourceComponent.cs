using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Stores a smoke solution on a nullspace entity that distributes the reagents to related smoke entities.
/// <seealso cref="SmokeComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class SmokeSourceComponent : Component
{
    /// <summary>
    /// The solution container containing the shared solution.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionManagerComponent>? SolutionContainer = null;

    /// <summary>
    /// The color of the smoke. Determined by the reagents when initially spawned.
    /// </summary>
    [DataField]
    public Color SmokeColor;

    /// <summary>
    /// The number of tiles that this smoke has spread to.
    /// Used to calculate the transfer rate.
    /// </summary>
    [DataField]
    public int SpreadCount = 0;

    /// <summary>
    /// Max volume of solution that the smoke holds.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxVolume = 600f;

    /// <summary>
    /// The starting volume of the solution in the smoke.
    /// Used to calculate the transfer rate.
    /// </summary>
    [DataField]
    public FixedPoint2 OriginalVolume = 0f;

    /// <summary>
    /// When set to true, the transfer rate will be re-calculated after the tick's spreading has completed.
    /// </summary>
    [ViewVariables]
    public bool DirtyTransferRateCalc;

    /// <summary>
    /// The max rate at which chemicals are transferred from the smoke to the person inhaling it.
    /// </summary>
    [DataField]
    public FixedPoint2 TransferRate;

    /// <summary>
    /// The total lifespan of the smoke.
    /// </summary>
    [DataField]
    public float Duration = 10;
}
