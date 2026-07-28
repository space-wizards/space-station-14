using Content.Shared.Destructible.Thresholds;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact trigger that activates when an entity collides or is used to attack the artifact.
/// EG: A user attacks the artifact whilst holding a knife. Or, a anomaly particle collides with the artifact.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATInteractAttackSystem)), AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class XATInteractAttackComponent : Component
{
    /// <summary>
    /// Whitelist of allowed interacting entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Number of interactions required to trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MinMax InteractionCount = new(1, 3);

    /// <summary>
    /// Number of interactions required to trigger, set after initiation
    /// </summary>
    [AutoNetworkedField]
    public int MaxCount = 0;

    /// <summary>
    /// Number of interactions to go.
    /// </summary>
    [AutoNetworkedField]
    public int Count = 0;
}
