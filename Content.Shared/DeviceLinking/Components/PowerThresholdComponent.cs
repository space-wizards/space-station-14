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
    public int ThresholdAmount { get; set; } = 350000;

    /// <summary>
    ///     Default threshold amounts for the set-threshold verb.
    /// </summary>
    [DataField("defaultThresholdAmounts")]
    public List<int>? DefaultThresholdAmounts;

    /// <summary>
    ///     The minimum value the threshold can be set to.
    /// </summary>
    [DataField("minThresholdAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MinimumThresholdAmount { get; set; } = 0;

    /// <summary>
    ///     The maximum value the threshold can be set to.
    /// </summary>
    [DataField("maxThresholdAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaximumThresholdAmount { get; set; } = 999999999;

    /// <summary>
    ///     Whether you're allowed to change the threshold amount.
    /// </summary>
    [DataField("canChangeThresholdAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanChangeThresholdAmount { get; set; } = false;
}
