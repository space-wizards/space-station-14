using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameStates;

namespace Content.Shared.Destructible;

/// <summary>
///     When attached to an <see cref="EntityUid"/>, allows it to take damage
///     and triggers thresholds when reached.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DestructibleComponent : Component
{
    /// <summary>
    /// A list of damage thresholds for the entity;
    /// includes their triggers and resultant behaviors
    /// </summary>
    [DataField(serverOnly: true)]
    public List<DamageThreshold> Thresholds = [];

    /// <summary>
    /// Specifies whether the entity has passed a damage threshold that causes it to break
    /// </summary>
    [DataField]
    public bool IsBroken;
}
