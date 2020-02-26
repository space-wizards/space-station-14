using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Sound;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
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
        private Angle _sprayAngle;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _soundGunEmpty, "sound_empty", null);
            serializer.DataField(ref _sprayAngle, "sprayangle", 0);
        }

        protected override void FireProjectile(IEntity user, GridCoordinates clickLocation)
        {
            var projectiles = GetFiredProjectile();
            var pellets = projectiles.Count;
            if (pellets > 1 & _sprayAngle > 0)
            {
                var angle = GetAngleFromClickLocation(user, clickLocation);
                var anglelist = Linspace(angle - _sprayAngle, angle + _sprayAngle, pellets);
                for (var i = 0; i < pellets ; i++)
                {
                    FireAtAngle(user, anglelist[i], projectiles[i]);
                }
                return;
            }
            foreach (var projectile in projectiles)
            {
                FireAtClickLocation(user, clickLocation, projectile);
            }
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

        protected sealed override List<IEntity> GetFiredProjectile()
        {
            var projectiles = new List<IEntity>();
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
                    return projectiles;
                }

                CycleChamberedBullet(0);
                // Load a new bullet into the chamber from magazine.
                var bulletquantity = bullet.BulletQuantity;

                for (var i = 0; i < bulletquantity; i++)
                {
                    var projec = Owner.EntityManager.SpawnEntity(bullet.ProjectileType, Owner.Transform.GridPosition);
                    projectiles.Add(projec);
                }
                bullet.Spent = true;
                return projectiles;
            }
            PlayEmpty();
            return projectiles;
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
