using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameStates;

namespace Content.Shared.Destructible;

/// <summary>
///     When attached to an <see cref="Robust.Shared.GameObjects.EntityUid"/>, allows it to take damage
///     and triggers thresholds when reached.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DestructibleComponent : Component
{
    [DataField]
    public List<DamageThreshold> Thresholds = new();
}
