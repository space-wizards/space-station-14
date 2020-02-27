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
    ///     Handles firing projectiles from a contained <see cref="AmmoComponent" />.
    /// </summary>
    public abstract class AmmoWeaponComponent : SharedProjectileWeaponComponent
    {
        protected Chamber[] Chambers;
        protected abstract int ChamberCount { get; }

        protected BallisticCaliber Caliber ;
        /// <summary>
        ///     What type of ammo this gun can fire.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public BallisticCaliber GunCaliber { get => Caliber; set => Caliber = value; }

        private string _soundGunEmpty;
        /// <summary>
        ///     Sound played when trying to shoot if there is no ammo available.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string SoundGunEmpty { get => _soundGunEmpty; set => _soundGunEmpty = value; }

        private float _spreadStdDev_Gun;
        /// <summary>
        ///     Increases the standard deviation of the ammo being fired.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SpreadStdDev_Gun { get => _spreadStdDev_Gun; set => _spreadStdDev_Gun = value; }

        private float _evenSpreadAngle_Gun;
        /// <summary>
        ///     Increases the evenspread of the ammo being fired.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float EvenSpreadAngle_Gun { get => _evenSpreadAngle_Gun; set => _evenSpreadAngle_Gun = value; }

        private float _velocity_Gun;
        /// <summary>
        ///     Increases the velocity of the ammo being fired.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Velocity_Gun { get => _velocity_Gun; set => _velocity_Gun = value; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundGunEmpty, "sound_empty", "/Audio/Guns/Empty/empty.ogg");
            serializer.DataField(ref _spreadStdDev_Gun, "spreadstddev", 3);
            serializer.DataField(ref _evenSpreadAngle_Gun, "evenspread", 20);
            serializer.DataField(ref _velocity_Gun, "gunvelocity", 0);
            serializer.DataField(ref Caliber, "caliber", BallisticCaliber.Unspecified);
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.GetComponent<RangedWeaponComponent>().FireHandler = TryShoot;
            Chambers = new Chamber[ChamberCount];
            for (var i = 0; i < Chambers.Length; i++)
            {
                var container = ContainerManagerComponent.Ensure<ContainerSlot>($"ballistics_chamber_{i}", Owner);
                Chambers[i] = new Chamber(container);
            }
        }

        /// <summary>
        ///     Fires projectiles based on loaded ammo from entity to a coordinate.
        /// </summary>
        protected void TryShoot(IEntity user, GridCoordinates clickLocation)
        {
            var ammo = GetChambered(FirstChamber)?.GetComponent<AmmoComponent>();
            CycleChamberedBullet(FirstChamber);
            if (ammo == null | ammo?.Spent == true | ammo?.Caliber != Caliber)
            {
                PlayEmptySound();
                return;
            }
            ammo.Spent = true;
            var total_stdev = _spreadStdDev_Gun + ammo.SpreadStdDev_Ammo;
            var final_evenspread = _evenSpreadAngle_Gun + ammo.EvenSpreadAngle_Ammo;
            var final_velocity = _velocity_Gun + ammo.Velocity_Ammo;
            FireAtCoord(user, clickLocation, ammo.ProjectileID, total_stdev, ammo.ProjectilesFired, final_evenspread, final_velocity);
        }

        protected IEntity GetChambered(int chamber) => Chambers[chamber].Slot.ContainedEntity;

        /// <summary>
        ///     Loads the next ammo casing into the chamber.
        /// </summary>
        protected virtual void CycleChamberedBullet(int chamber) { }

        public IEntity RemoveFromChamber(int chamber)
        {
            var c = Chambers[chamber];
            var loaded = c.Slot.ContainedEntity;
            if (loaded != null)
            {
                c.Slot.Remove(loaded);
            }
            return loaded;
        }

        protected bool LoadIntoChamber(int chamber, IEntity bullet)
        {
            if (!bullet.TryGetComponent(out AmmoComponent component))
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
            Chambers[chamber].Slot.Insert(bullet);
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
