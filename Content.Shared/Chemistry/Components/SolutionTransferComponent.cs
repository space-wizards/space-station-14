using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
///     Gives click behavior for transferring to/from other reagent containers.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SolutionTransferComponent : Component
{
    /// <summary>
    ///     The amount of solution to be transferred from this solution when clicking on other solutions with it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    /// <summary>
    ///     The minimum amount of solution that can be transferred at once from this solution.
    /// </summary>
    [DataField("minTransferAmount"), AutoNetworkedField]
    public FixedPoint2 MinimumTransferAmount = FixedPoint2.New(5);

    /// <summary>
    ///     The maximum amount of solution that can be transferred at once from this solution.
    /// </summary>
    [DataField("maxTransferAmount"), AutoNetworkedField]
    public FixedPoint2 MaximumTransferAmount = FixedPoint2.New(100);

    /// <summary>
    ///     Can this entity take reagent from reagent tanks?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanReceive = true;

    /// <summary>
    ///     Can this entity give reagent to other reagent containers?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanSend = true;

    /// <summary>
    /// Whether you're allowed to change the transfer amount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanChangeTransferAmount = false;
}
