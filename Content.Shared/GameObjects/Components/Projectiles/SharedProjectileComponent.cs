#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Projectiles
{
    public abstract class SharedProjectileComponent : Component, ICollideSpecial
    {
        private bool _ignoreShooter = true;
        public override string Name => "Projectile";
        public override uint? NetID => ContentNetIDs.PROJECTILE;

        protected abstract EntityUid Shooter { get; }

        public bool IgnoreShooter
        {
            get => _ignoreShooter;
            set
            {
                if (_ignoreShooter == value) return;

                _ignoreShooter = value;
                Dirty();
            }
        }

        [NetSerializable, Serializable]
        protected class ProjectileComponentState : ComponentState
        {
            public ProjectileComponentState(uint netId, EntityUid shooter, bool ignoreShooter) : base(netId)
            {
                Shooter = shooter;
                IgnoreShooter = ignoreShooter;
            }

            public EntityUid Shooter { get; }
            public bool IgnoreShooter { get; }
        }

        public bool PreventCollide(IPhysBody collidedwith)
        {
            return IgnoreShooter && collidedwith.Owner.Uid == Shooter;
        }
    }
}
