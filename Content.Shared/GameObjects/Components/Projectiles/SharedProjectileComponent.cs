#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Projectiles
{
    public abstract class SharedProjectileComponent : Component
    {
        private bool _ignoreShooter = true;
        public override string Name => "Projectile";
        public override uint? NetID => ContentNetIDs.PROJECTILE;

        public EntityUid Shooter { get; protected set; }

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
            public ProjectileComponentState(EntityUid shooter, bool ignoreShooter) : base(ContentNetIDs.PROJECTILE)
            {
                Shooter = shooter;
                IgnoreShooter = ignoreShooter;
            }

            public EntityUid Shooter { get; }
            public bool IgnoreShooter { get; }
        }
    }
}
