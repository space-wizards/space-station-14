
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact that is activated after a certain amount of stamina damage is dealt.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(XATStaminaDamageThresholdReachedSystem))]
public sealed partial class XATStaminaDamageThresholdReachedComponent : Component
{
    /// <summary>
    /// Stamina Damage accumulated by artifact so far. Is cleared on node activation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AccumulatedDamage = 0;

    /// <summary>
    /// Amount of Stamina Damage required to trigger the artifact.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float DamageNeeded;

    /// <summary>
    /// What to use in popup after stamina damage was received but <see cref="DamageNeeded"/> is not met yet.
    /// If null - no popup will be shown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? InsufficientDamagePopup = "interact-artifact-more";
}
