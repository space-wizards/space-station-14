using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Projectiles
{
    [NetworkedComponent()]
    public abstract class SharedProjectileComponent : Component
    {
        private bool _ignoreShooter = true;
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
        protected sealed class ProjectileComponentState : ComponentState
        {
            public ProjectileComponentState(EntityUid shooter, bool ignoreShooter)
            {
                Shooter = shooter;
                IgnoreShooter = ignoreShooter;
            }

            public EntityUid Shooter { get; }
            public bool IgnoreShooter { get; }
        }
    }
}
