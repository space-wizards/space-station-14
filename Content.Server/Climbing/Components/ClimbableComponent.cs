using Content.Shared.CCVar;
using Content.Shared.Climbing;
using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.Climbing.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedClimbableComponent))]
public sealed class ClimbableComponent : SharedClimbableComponent
{
    /// <summary>
    ///     The time it takes to climb onto the entity.
    /// </summary>
    [ViewVariables]
    [DataField("delay")]
    public float ClimbDelay = 0.8f;

    /// <summary>
    /// If set, people can bonk on this if <see cref="CCVars.GameTableBonk"/> is set or if they are clumsy.
    /// </summary>
    [ViewVariables] [DataField("bonk")] public bool Bonk = false;

    /// <summary>
    /// Chance of bonk triggering if the user is clumsy.
    /// </summary>
    [ViewVariables] [DataField("bonkClumsyChance")]
    public float BonkClumsyChance = 0.75f;

    /// <summary>
    /// Sound to play when bonking.
    /// </summary>
    /// <seealso cref="Bonk"/>
    [ViewVariables] [DataField("bonkSound")]
    public SoundSpecifier? BonkSound;

    /// <summary>
    /// How long to stun players on bonk, in seconds.
    /// </summary>
    /// <seealso cref="Bonk"/>
    [ViewVariables] [DataField("bonkTime")]
    public float BonkTime = 2;

    /// <summary>
    /// How much damage to apply on bonk.
    /// </summary>
    /// <seealso cref="Bonk"/>
    [ViewVariables] [DataField("bonkDamage")]
    public DamageSpecifier? BonkDamage;
}
