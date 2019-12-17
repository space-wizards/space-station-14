using System;
using Content.Server.GameObjects.Components.Sound;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    public abstract class BallisticWeaponComponent : ProjectileWeaponComponent
    {
        private BallisticCaliber _caliber;
        private Chamber[] _chambers;

        public BallisticCaliber Caliber => _caliber;
        protected abstract int ChamberCount { get; }

        private string _soundGunEmpty;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _soundGunEmpty, "sound_empty", null);
        }

        public override void Initialize()
        {
            base.Initialize();

            _chambers = new Chamber[ChamberCount];
            for (var i = 0; i < _chambers.Length; i++)
            {
                var container = ContainerManagerComponent.Ensure<ContainerSlot>($"ballistics_chamber_{i}", Owner);
                _chambers[i] = new Chamber(container);
            }
        }

        public IEntity GetChambered(int chamber) => _chambers[chamber].Slot.ContainedEntity;

        public bool LoadIntoChamber(int chamber, IEntity bullet)
        {
            if (!bullet.TryGetComponent(out BallisticBulletComponent component))
            {
                throw new ArgumentException("entity isn't a bullet.", nameof(bullet));
            }

            if (component.Caliber != Caliber)
            {
                throw new ArgumentException("entity is of the wrong caliber.", nameof(bullet));
            }

            if (GetChambered(chamber) != null)
            {
                return false;
            }

            _chambers[chamber].Slot.Insert(bullet);
            return true;
        }

        protected sealed override IEntity GetFiredProjectile()
        {
            void PlayEmpty()
            {
                if (_soundGunEmpty != null)
                {
                    Owner.GetComponent<SoundComponent>().Play(_soundGunEmpty);
                }
            }
            var chambered = GetChambered(0);
            if (chambered != null)
            {
                var bullet = chambered.GetComponent<BallisticBulletComponent>();
                if (bullet.Spent)
                {
                    PlayEmpty();
                    return null;
                }

                var projectile = Owner.EntityManager.SpawnEntity(bullet.ProjectileType, Owner.Transform.GridPosition);
                bullet.Spent = true;

                CycleChamberedBullet(0);

                // Load a new bullet into the chamber from magazine.
                return projectile;
            }

            PlayEmpty();
            return null;
        }

        protected virtual void CycleChamberedBullet(int chamber)
        {

        }

        public IEntity RemoveFromChamber(int chamber)
        {
            var c = _chambers[chamber];
            var loaded = c.Slot.ContainedEntity;
            if (loaded != null)
            {
                c.Slot.Remove(loaded);
            }
            return loaded;
        }

        private sealed class Chamber
        {
            public Chamber(ContainerSlot slot)
            {
                Slot = slot;
            }

            public ContainerSlot Slot { get; }
        }

    }
}
