using Content.Shared.Singularity.Components;
using Robust.Client.Animations;

namespace Content.Client.Singularity.Visualizers;

/// <summary>
/// The component used to reflect the state of a radiation collector in its appearance.
/// </summary>
[RegisterComponent]
[Access(typeof(RadiationCollectorSystem))]
public sealed partial class RadiationCollectorComponent : Component
{
    /// <summary>
    /// The key used to index the (de)activation animations played when turning a radiation collector on/off.
    /// </summary>
    [ViewVariables]
    public const string AnimationKey = "radiationcollector_animation";

    /// <summary>
    /// The current visual state of the radiation collector.
    /// </summary>
    [ViewVariables]
    public RadiationCollectorVisualState CurrentState = RadiationCollectorVisualState.Deactive;

    /// <summary>
    /// The RSI state used for the main sprite layer (<see cref="RadiationCollectorVisualLayers.Main"/>) when the radiation collector is active.
    /// </summary>
    [DataField("activeState")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string ActiveState = "ca_on";

    /// <summary>
    /// The RSI state used for the main sprite layer (<see cref="RadiationCollectorVisualLayers.Main"/>) when the radiation collector is inactive.
    /// </summary>
    [DataField("inactiveState")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string InactiveState = "ca_off";

    /// <summary>
    /// Used to build the <value cref="ActivateAnimation">activation animation</value> when the component is initialized.
    /// </summary>
    [DataField("activatingState")]
    public string ActivatingState = "ca_active";

    /// <summary>
    /// Used to build the <see cref="DeactiveAnimation">deactivation animation</see> when the component is initialized.
    /// </summary>
    [DataField("deactivatingState")]
    public string DeactivatingState = "ca_deactive";

    /// <summary>
    /// The animation used when turning on the radiation collector.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Animation ActivateAnimation = default!;

    /// <summary>
    /// The animation used when turning off the radiation collector.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Animation DeactiveAnimation = default!;
}
