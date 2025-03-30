using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceLinking.Components;

/// <summary>
///     Provides a verb to change the threshold power level something is looking at.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerThresholdComponent : Component
{
    /// <summary>
    ///     The threshold power level to compare against.
    /// </summary>
    [DataField("thresholdAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public FixedPoint2 ThresholdAmount { get; set; } = FixedPoint2.New(350000);

    /// <summary>
    ///     The minimum value the threshold can be set to.
    /// </summary>
    [DataField("minThresholdAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MinimumThresholdAmount { get; set; } = FixedPoint2.New(0);

    /// <summary>
    ///     The maximum value the threshold can be set to.
    /// </summary>
    [DataField("maxThresholdAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaximumThresholdAmount { get; set; } = FixedPoint2.New(999999999);

    /// <summary>
    /// Whether you're allowed to change the threshold amount.
    /// </summary>
    [DataField("canChangeThresholdAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanChangeThresholdAmount { get; set; } = false;
}
