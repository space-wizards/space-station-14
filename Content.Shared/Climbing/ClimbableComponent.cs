using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Climbing
{
    [RegisterComponent, NetworkedComponent]
    public sealed class ClimbableComponent : Component
    {
        /// <summary>
        ///     The range from which this entity can be climbed.
        /// </summary>
        [DataField("range")] public float Range = SharedInteractionSystem.InteractionRange / 1.4f;

        /// <summary>
        ///     The time it takes to climb onto the entity.
        /// </summary>
        [DataField("delay")]
        public float ClimbDelay = 0.8f;

        /// <summary>
        /// If set, people can bonk on this if <see cref="CCVars.GameTableBonk"/> is set or if they are clumsy.
        /// </summary>
        [DataField("bonk")] public bool Bonk = false;

        /// <summary>
        /// Chance of bonk triggering if the user is clumsy.
        /// </summary>
        [DataField("bonkClumsyChance")]
        public float BonkClumsyChance = 0.75f;

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
    }
}
