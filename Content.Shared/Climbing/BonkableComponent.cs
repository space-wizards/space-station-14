using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Climbing
{
    [RegisterComponent, NetworkedComponent]
    public sealed class BonkableComponent : Component
    {
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
