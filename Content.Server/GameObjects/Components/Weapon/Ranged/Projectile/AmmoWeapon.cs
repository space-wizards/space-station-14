using Content.Server.GameObjects.Components.Sound;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables; //todo: add VV view/edit to ammogun properties

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    /// <summary>
    ///     Weapons that fire projectiles from  an <see cref="AmmoComponent" />.
    /// </summary>
    public abstract class AmmoWeaponComponent : SharedProjectileWeaponComponent
    {
        protected Chamber[] Chambers;
        protected abstract int ChamberCount { get; }
        /// <summary>
        ///     Sound played when trying to shoot if there is no ammo available.
        /// </summary>
        private string _soundGunEmpty;
        private float _spreadStdDev_Gun;
        private float _evenSpreadAngle_Gun;
        private float _velocity_Gun;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundGunEmpty, "sound_empty", "/Audio/Guns/Empty/empty.ogg");
            serializer.DataField(ref _spreadStdDev_Gun, "spreadstddev", 3);
            serializer.DataField(ref _evenSpreadAngle_Gun, "evenspread", 0);
            serializer.DataField(ref _velocity_Gun, "gunvelocity", 0);
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
            var ammo = GetChamberedAmmo();
            if(ammo == null | ammo?.Spent == true)
            {
                PlayEmptySound();
                return;
            }
            ammo.Spent = true;
            var total_stdev = _spreadStdDev_Gun + ammo.SpreadStdDev_Ammo;
            var final_spreadangle = _evenSpreadAngle_Gun + ammo.EvenSpreadAngle_Ammo;
            var final_velocity = _velocity_Gun + ammo.Velocity_Ammo;
            FireAtCoord(user, clickLocation, ammo.ProjectileID, total_stdev, ammo.ProjectilesFired, final_spreadangle, final_velocity);
        }

        protected IEntity GetChambered(int chamber) => Chambers[chamber].Slot.ContainedEntity;

        /// <summary>
        ///     Returns AmmoComponent of chambered ammo
        /// </summary>
        private AmmoComponent GetChamberedAmmo()
        {
            var bullet = GetChambered(FirstChamber)?.GetComponent<AmmoComponent>();
            if (bullet != null)
            {
                CycleChamberedBullet(FirstChamber);
            }
            return bullet;
        }

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
