using Content.Server.GameObjects.Components.Sound;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    /// <summary>
    ///     Handles firing projectiles from a contained <see cref="BallisticBulletComponent" />.
    /// </summary>
    public abstract class BallisticWeaponComponent : BaseProjectileWeaponComponent
    {
        private Chamber[] _chambers;

        /// <summary>
        ///     Number of chambers created during initialization.
        /// </summary>
        private int _chamberCount;

        [ViewVariables]
        private BallisticCaliber _caliber ;
        /// <summary>
        ///     What type of ammo this gun can fire.
        /// </summary>

        private string _soundGunEmpty;
        /// <summary>
        ///     Sound played when trying to shoot if there is no ammo available.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string SoundGunEmpty { get => _soundGunEmpty; set => _soundGunEmpty = value; }

        private float _spreadStdDevGun;
        /// <summary>
        ///     Increases the standard deviation of the ammo being fired.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SpreadStdDevGun { get => _spreadStdDevGun; set => _spreadStdDevGun = value; }

        private float _evenSpreadAngleGun;
        /// <summary>
        ///     Increases the evenspread of the ammo being fired.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float EvenSpreadAngleGun { get => _evenSpreadAngleGun; set => _evenSpreadAngleGun = value; }

        private float _velocityGun;
        /// <summary>
        ///     Increases the velocity of the ammo being fired.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float VelocityGun { get => _velocityGun; set => _velocityGun = value; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundGunEmpty, "sound_empty", "/Audio/Guns/Empty/empty.ogg");
            serializer.DataField(ref _spreadStdDevGun, "spreadstddev", 0);
            serializer.DataField(ref _evenSpreadAngleGun, "evenspread", 0);
            serializer.DataField(ref _velocityGun, "gunvelocity", 0);
            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _chamberCount, "chambers", 1);
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.GetComponent<RangedWeaponComponent>().FireHandler = TryShoot;
            _chambers = new Chamber[_chamberCount];
            for (var i = 0; i < _chambers.Length; i++)
            {
                var container = ContainerManagerComponent.Ensure<ContainerSlot>($"ballistics_chamber_{i}", Owner);
                _chambers[i] = new Chamber(container);
            }
        }

        /// <summary>
        ///     Fires projectiles based on loaded ammo from entity to a coordinate.
        /// </summary>
        protected void TryShoot(IEntity user, GridCoordinates clickLocation)
        {
            var ammo = GetChambered(FirstChamber)?.GetComponent<BallisticBulletComponent>();
            CycleChamberedBullet(FirstChamber);
            if (ammo == null || ammo?.Spent == true || ammo?.Caliber != _caliber)
            {
                PlayEmptySound();
                return;
            }
            ammo.Spent = true;
            var total_stdev = _spreadStdDevGun + ammo.SpreadStdDev_Ammo;
            var final_evenspread = _evenSpreadAngleGun + ammo.EvenSpreadAngle_Ammo;
            var final_velocity = _velocityGun + ammo.Velocity_Ammo;
            FireAtCoord(user, clickLocation, ammo.ProjectileID, total_stdev, ammo.ProjectilesFired, final_evenspread, final_velocity);
        }

        protected IEntity GetChambered(int chamber) => _chambers[chamber].Slot.ContainedEntity;

        /// <summary>
        ///     Loads the next ammo casing into the chamber.
        /// </summary>
        protected virtual void CycleChamberedBullet(int chamber) { }

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

        protected bool LoadIntoChamber(int chamber, IEntity bullet)
        {
            if (!bullet.TryGetComponent(out BallisticBulletComponent component))
            {
                throw new ArgumentException("entity isn't a bullet.", nameof(bullet));
            }
            if (component.Caliber != _caliber)
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

        private void PlayEmptySound() => Owner.GetComponent<SoundComponent>().Play(_soundGunEmpty);

        protected sealed class Chamber
        {
            public Chamber(ContainerSlot slot)
            {
                Slot = slot;
            }

            public ContainerSlot Slot { get; }
        }

        private const int FirstChamber = 0;
    }
}
