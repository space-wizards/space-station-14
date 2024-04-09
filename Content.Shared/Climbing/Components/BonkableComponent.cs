using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Climbing.Components;

/// <summary>
///     Makes entity do damage and stun entities with ClumsyComponent
///     upon DragDrop or Climb interactions.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(Systems.BonkSystem))]
public sealed partial class BonkableComponent : Component
{
    /// <summary>
    /// Chance of bonk triggering if the user is clumsy.
    /// </summary>
    [DataField("bonkClumsyChance")]
    public float BonkClumsyChance = 0.5f;

    /// <summary>
    /// Sound to play when bonking.
    /// </summary>
    /// <seealso cref="Bonk"/>
    [DataField("bonkSound")]
    public SoundSpecifier? BonkSound;

    /// <summary>
    /// How long to stun players on bonk, in seconds.
    /// </summary>
    /// <seealso cref="Bonk"/>
    [DataField("bonkTime")]
    public float BonkTime = 2;

    /// <summary>
    /// How much damage to apply on bonk.
    /// </summary>
    /// <seealso cref="Bonk"/>
    [DataField("bonkDamage")]
    public DamageSpecifier? BonkDamage;

    /// <summary>
    /// How long it takes to bonk.
    /// </summary>
    [DataField("bonkDelay")]
    public float BonkDelay = 1.5f;
}
