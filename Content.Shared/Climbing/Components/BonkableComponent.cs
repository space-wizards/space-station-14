using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Climbing.Components;

/// <summary>
///     Makes entity do damage and stun entities with ClumsyComponent
///     upon DragDrop or Climb interactions.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BonkableComponent : Component
{
    /// <summary>
    ///     How long to stun players on bonk, in seconds.
    /// </summary>
    [DataField]
    public TimeSpan BonkTime = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     How much damage to apply on bonk.
    /// </summary>
    [DataField]
    public DamageSpecifier? BonkDamage;
}
