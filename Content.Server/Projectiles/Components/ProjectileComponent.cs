using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Projectiles.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedProjectileComponent))]
    public class ProjectileComponent : SharedProjectileComponent
    {
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        [DataField("damages")] private Dictionary<DamageType, int> _damages = new();

        [ViewVariables]
        public Dictionary<DamageType, int> Damages
        {
            get => _damages;
            set => _damages = value;
        }
=======
=======
>>>>>>> refactor-damageablecomponent
        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // This also requires changing the dictionary type and modifying ProjectileSystem.cs, which uses it.
        // While thats being done, also replace "damages" -> "damageTypes" For consistency.
        [DataField("damages")]
        [ViewVariables(VVAccess.ReadWrite)]
        public Dictionary<string, int> Damages { get; set; } = new();
<<<<<<< HEAD
>>>>>>> Refactor damageablecomponent update (#4406)
=======
>>>>>>> refactor-damageablecomponent

        [DataField("deleteOnCollide")]
        public bool DeleteOnCollide { get; } = true;

        // Get that juicy FPS hit sound
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        [DataField("soundHit", required: true)] public SoundSpecifier? SoundHit = default!;
=======
        [DataField("soundHit", required: true)] public SoundSpecifier SoundHit = default!;
>>>>>>> Bring refactor-damageablecomponent branch up-to-date with master (#4510)
=======
        [DataField("soundHit", required: true)] public SoundSpecifier SoundHit = default!;
>>>>>>> refactor-damageablecomponent
        [DataField("soundHitSpecies")] public SoundSpecifier? SoundHitSpecies = null;

        public bool DamagedEntity;

        public float TimeLeft { get; set; } = 10;

        /// <summary>
        /// Function that makes the collision of this object ignore a specific entity so we don't collide with ourselves
        /// </summary>
        /// <param name="shooter"></param>
        public void IgnoreEntity(IEntity shooter)
        {
            Shooter = shooter.Uid;
            Dirty();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ProjectileComponentState(Shooter, IgnoreShooter);
        }
    }
}
