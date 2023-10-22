using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
///     Gives click behavior for transferring to/from other reagent containers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SolutionTransferComponent : SharedSolutionTransferComponent
{
    /// <summary>
    ///     The amount of solution to be transferred from this solution when clicking on other solutions with it.
    /// </summary>
    [DataField("transferAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TransferAmount { get; set; } = FixedPoint2.New(5);

    /// <summary>
    ///     The minimum amount of solution that can be transferred at once from this solution.
    /// </summary>
    [DataField("minTransferAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MinimumTransferAmount { get; set; } = FixedPoint2.New(5);

    /// <summary>
    ///     The maximum amount of solution that can be transferred at once from this solution.
    /// </summary>
    [DataField("maxTransferAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaximumTransferAmount { get; set; } = FixedPoint2.New(50);

    /// <summary>
    /// Whether you're allowed to change the transfer amount.
    /// </summary>
    [DataField("canChangeTransferAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanChangeTransferAmount { get; set; } = false;

    /// <summary>
    /// pure solutions (inject or draw something) 
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public SharedTransferToggleMode? ToggleMode { get; set; }

    public Solution? DrainableSolution { get; set; } = null;
    public Solution? RefillableSolution { get; set; } = null;
}
