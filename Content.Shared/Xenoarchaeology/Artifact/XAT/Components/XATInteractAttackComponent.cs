using Content.Shared.Destructible.Thresholds;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact trigger that activates when an entity collides or is used to attack the artifact.
/// EG: A user attacks the artifact whilst holding a knife. Or, a anomaly particle collides with the artifact.
/// </summary>
/// <remarks>
/// Please consider the fact that all triggers should not be overlapping,
/// or softlocking artifact will become possible (when several triggers activate automatically, or one blocks other).
/// To avoid this - remember about <see cref="XATDamageThresholdReachedComponent"/>, <see cref="XATStaminaDamageThresholdReachedComponent"/>
/// can overlap with this component in a bad way.
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(XATInteractAttackSystem)), AutoGenerateComponentState]
public sealed partial class XATInteractAttackComponent : Component
{
    /// <summary>
    /// Whitelist of allowed interacting entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Range to roll for selecting number of interactions, required to activate the trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MinMax InteractionCount = new(1, 1);

    /// <summary>
    /// Number of interactions required to trigger, set after initiation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? MaxCount;

    /// <summary>
    /// Number of interactions currently left to activate the trigger.
    /// Resets after each activation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? Count;

    /// <summary>
    /// What to use in popup after an interaction was received but <see cref="MaxCount"/> is not met yet.
    /// If null - no popup will be shown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? InsufficientInteractionPopup = "interact-artifact-more";
}
